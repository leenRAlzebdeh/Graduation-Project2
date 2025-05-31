using JUSTLockers.Service;
using JUSTLockers.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JUSTLockers.Tests.IntegartionTest
{
    public class LockerReservationIntegrationTest: IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly StudentService _studentService;
        private MySqlConnection _connection;
        private MySqlTransaction _transaction;
        private readonly AdminService _adminService;
        private readonly string connectionString = "Server=localhost;Database=testing;User=root;Password=1234;";
        private readonly IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());


        public LockerReservationIntegrationTest()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                {"ConnectionStrings:DefaultConnection", connectionString}
                })
                .Build();

            _configuration = config;
            _studentService = new StudentService(_configuration, memoryCache, _adminService);

            // Start a transaction for test isolation
            _connection = new MySqlConnection(connectionString);
            _connection.Open();
            _transaction = _connection.BeginTransaction();
        }

        #region Helper Methods
        public async Task<T?> GetRandomEntityAsync<T>(string tableName, Func<IDataReader, T> mapFunc, string? whereClause = null, object? parameters = null)
        {
            var cmd = _connection.CreateCommand();
            cmd.Transaction = _transaction;
            cmd.CommandText = $"SELECT * FROM {tableName}" + (whereClause != null ? $" WHERE {whereClause}" : "") + " ORDER BY RAND() LIMIT 1";
            if (parameters != null)
            {
                foreach (var prop in parameters.GetType().GetProperties())
                {
                    cmd.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                }
            }
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return mapFunc(reader);
            }
            return default;
        }

        public async Task<int> GetNextIdAsync(string tableName, string idColumn = "Id")
        {
            var cmd = _connection.CreateCommand();
            cmd.Transaction = _transaction;
            cmd.CommandText = $"SELECT IFNULL(MAX({idColumn}), 0) + 1 FROM {tableName}";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private async Task CleanupOrphanedReservations()
        {
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = _transaction;
            cmd.CommandText = @"
                DELETE FROM Reservations 
                WHERE LockerId NOT IN (
                    SELECT COALESCE(locker_id, '') FROM Students WHERE locker_id IS NOT NULL
                )";
            await cmd.ExecuteNonQueryAsync();
        }
        #endregion

        #region Integration Tests
        [Fact]
        public async Task LockerReservationWorkflow_SuccessfulReservation_UpdatesAllEntities()
        {
            // Arrange: Get a student without a reservation and not blocked
            var student = await GetRandomEntityAsync(
                "Students s LEFT JOIN Reservations r ON s.id = r.StudentId LEFT JOIN BlockList b ON s.id = b.student_id",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("department")),
                    Location = r.GetString(r.GetOrdinal("Location"))
                },
                "r.Id IS NULL AND b.student_id IS NULL"
            );
            if (student == null)
            {
                Console.WriteLine("No unblocked student without reservation found; skipping test.");
                return;
            }

            // Verify student is not blocked
            var isBlocked = await _studentService.IsStudentBlocked(student.Id);
            Assert.False(isBlocked);

            // Get department info
            var deptInfo = await _studentService.GetDepartmentInfo(student.Id);
            Assert.NotNull(deptInfo);
            Assert.Equal(student.Department, deptInfo.DepartmentName);
            Assert.Equal(student.Location, deptInfo.Location);

            // Get available wings and levels
            var wings = await _studentService.GetAvailableWingsAndLevels(student.Department, student.Location);
            if (!wings.Any())
            {
                Console.WriteLine($"No available wings for department {student.Department}, location {student.Location}; skipping test.");
                return;
            }

            // Select a random wing and level
            var random = new Random();
            var wingLevel = wings[random.Next(wings.Count)];
            var wing = wingLevel.Wing;
            var level = wingLevel.Level;

            // Get cabinet to check capacity
            var cabinet = await GetRandomEntityAsync(
                "Cabinets",
                r => new
                {
                    CabinetId = r.GetString(r.GetOrdinal("cabinet_id")),
                    Capacity = r.GetInt32(r.GetOrdinal("capacity")),
                    ReservedLockers = r.GetInt32(r.GetOrdinal("reservedLockers"))
                },
                "department_name = @Department AND location = @Location AND wing = @Wing AND level = @Level",
                new { Department = student.Department, Location = student.Location, Wing = wing, Level = level }
            );
            if (cabinet == null || cabinet.ReservedLockers >= cabinet.Capacity)
            {
                Console.WriteLine("No cabinet with available capacity found; skipping test.");
                return;
            }

            // Act: Reserve a locker
            await CleanupOrphanedReservations(); // Ensure no orphaned reservations
            var lockerId = await _studentService.ReserveLockerInWingAndLevel(student.Id, student.Department, student.Location, wing, level);
            Assert.NotNull(lockerId);

            // Assert: Verify reservation, locker, cabinet, and student updates
            // Check reservation
            var reservation = await GetRandomEntityAsync(
                "Reservations",
                r => new
                {
                    Id = r.GetString(r.GetOrdinal("Id")),
                    StudentId = r.GetInt32(r.GetOrdinal("StudentId")),
                    LockerId = r.GetString(r.GetOrdinal("LockerId")),
                    Status = r.GetString(r.GetOrdinal("Status"))
                },
                "StudentId = @StudentId AND LockerId = @LockerId",
                new { StudentId = student.Id, LockerId = lockerId }
            );
            Assert.NotNull(reservation);
            Assert.Equal("RESERVED", reservation.Status);

            // Check locker
            var locker = await GetRandomEntityAsync(
                "Lockers",
                r => new
                {
                    Id = r.GetString(r.GetOrdinal("Id")),
                    Status = r.GetString(r.GetOrdinal("Status")),
                    CabinetId = r.GetString(r.GetOrdinal("CabinetId"))
                },
                "Id = @LockerId",
                new { LockerId = lockerId }
            );
            Assert.NotNull(locker);
            Assert.Equal("RESERVED", locker.Status);
            Assert.Equal(cabinet.CabinetId, locker.CabinetId);

            // Check cabinet
            var updatedCabinet = await GetRandomEntityAsync(
                "Cabinets",
                r => new { ReservedLockers = r.GetInt32(r.GetOrdinal("reservedLockers")) },
                "cabinet_id = @CabinetId",
                new { CabinetId = cabinet.CabinetId }
            );
            Assert.NotNull(updatedCabinet);
            Assert.Equal(cabinet.ReservedLockers + 1, updatedCabinet.ReservedLockers);

            // Check student
            var updatedStudent = await GetRandomEntityAsync(
                "Students",
                r => new { LockerId = r.IsDBNull(r.GetOrdinal("locker_id")) ? null : r.GetString(r.GetOrdinal("locker_id")) },
                "id = @StudentId",
                new { StudentId = student.Id }
            );
            Assert.NotNull(updatedStudent);
            Assert.Equal(lockerId, updatedStudent.LockerId);
        }

        [Fact]
        public async Task LockerReservationWorkflow_BlockedStudent_FailsReservation()
        {
            // Arrange: Get a blocked student
            var student = await GetRandomEntityAsync(
                "Students s JOIN BlockList b ON s.id = b.student_id",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("department")),
                    Location = r.GetString(r.GetOrdinal("Location"))
                },
                null
            );
            if (student == null)
            {
                Console.WriteLine("No blocked student found; skipping test.");
                return;
            }

            // Verify student is blocked
            var isBlocked = await _studentService.IsStudentBlocked(student.Id);
            Assert.True(isBlocked);

            // Get available wings and levels
            var wings = await _studentService.GetAvailableWingsAndLevels(student.Department, student.Location);
            if (!wings.Any())
            {
                Console.WriteLine($"No available wings for department {student.Department}, location {student.Location}; skipping test.");
                return;
            }

            // Select a random wing and level
            var random = new Random();
            var wingLevel = wings[random.Next(wings.Count)];
            var wing = wingLevel.Wing;
            var level = wingLevel.Level;

            // Act: Attempt to reserve a locker
            await CleanupOrphanedReservations();
            var lockerId = await _studentService.ReserveLockerInWingAndLevel(student.Id, student.Department, student.Location, wing, level);

            // Assert: Reservation fails
            Assert.Null(lockerId);

            // Verify no reservation was created
            var reservation = await GetRandomEntityAsync(
                "Reservations",
                r => new { Id = r.GetString(r.GetOrdinal("Id")) },
                "StudentId = @StudentId AND Status = 'RESERVED'",
                new { StudentId = student.Id }
            );
            Assert.Null(reservation);
        }

        [Fact]
        public async Task LockerReservationWorkflow_NoAvailableLockers_FailsReservation()
        {
            // Arrange: Get a student without a reservation
            var student = await GetRandomEntityAsync(
                "Students s LEFT JOIN Reservations r ON s.id = r.StudentId LEFT JOIN BlockList b ON s.id = b.student_id",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("department")),
                    Location = r.GetString(r.GetOrdinal("Location"))
                },
                "r.Id IS NULL AND b.student_id IS NULL"
            );
            if (student == null)
            {
                Console.WriteLine("No unblocked student without reservation found; skipping test.");
                return;
            }

            // Get a cabinet with no available lockers (reservedLockers >= capacity)
            var cabinet = await GetRandomEntityAsync(
                "Cabinets",
                r => new
                {
                    Wing = r.GetString(r.GetOrdinal("wing")),
                    Level = r.GetInt32(r.GetOrdinal("level")),
                    Department = r.GetString(r.GetOrdinal("department_name")),
                    Location = r.GetString(r.GetOrdinal("location")),
                    Capacity = r.GetInt32(r.GetOrdinal("capacity")),
                    ReservedLockers = r.GetInt32(r.GetOrdinal("reservedLockers"))
                },
                "reservedLockers >= capacity AND department_name = @Department AND location = @Location",
                new { Department = student.Department, Location = student.Location }
            );
            if (cabinet == null)
            {
                Console.WriteLine("No fully reserved cabinet found; skipping test.");
                return;
            }

            // Act: Attempt to reserve a locker
            await CleanupOrphanedReservations();
            var lockerId = await _studentService.ReserveLockerInWingAndLevel(student.Id, cabinet.Department, cabinet.Location, cabinet.Wing, cabinet.Level);

            // Assert: Reservation fails
            Assert.Null(lockerId);

            // Verify no reservation was created
            var reservation = await GetRandomEntityAsync(
                "Reservations",
                r => new { Id = r.GetString(r.GetOrdinal("Id")) },
                "StudentId = @StudentId",
                new { StudentId = student.Id }
            );
            Assert.Null(reservation);
        }

        [Fact]
        public async Task LockerReservationWorkflow_ExistingReservation_FailsReservation()
        {
            // Arrange: Get a student with an active reservation
            var student = await GetRandomEntityAsync(
                "Students s JOIN Reservations r ON s.id = r.StudentId LEFT JOIN BlockList b ON s.id = b.student_id",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("department")),
                    Location = r.GetString(r.GetOrdinal("Location"))
                },
                "r.Status = 'RESERVED' AND b.student_id IS NULL"
            );
            if (student == null)
            {
                Console.WriteLine("No unblocked student with active reservation found; skipping test.");
                return;
            }

            // Verify student has a locker
            var hasLocker = await Task.FromResult(_studentService.HasLocker(student.Id));
            Assert.True(hasLocker);

            // Get available wings and levels
            var wings = await _studentService.GetAvailableWingsAndLevels(student.Department, student.Location);
            if (!wings.Any())
            {
                Console.WriteLine($"No available wings for department {student.Department}, location {student.Location}; skipping test.");
                return;
            }

            // Select a random wing and level
            var random = new Random();
            var wingLevel = wings[random.Next(wings.Count)];
            var wing = wingLevel.Wing;
            var level = wingLevel.Level;

            // Act: Attempt to reserve another locker
            await CleanupOrphanedReservations();
            var lockerId = await _studentService.ReserveLockerInWingAndLevel(student.Id, student.Department, student.Location, wing, level);

            // Assert: Reservation fails
            Assert.Null(lockerId);

            // Verify only one reservation exists
            var reservations = new List<string>();
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Transaction = _transaction;
                cmd.CommandText = "SELECT Id FROM Reservations WHERE StudentId = @StudentId AND Status = 'RESERVED'";
                cmd.Parameters.AddWithValue("@StudentId", student.Id);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    reservations.Add(reader.GetString(reader.GetOrdinal("Id")));
                }
            }
            Assert.Single(reservations);
        }
        #endregion

        public void Dispose()
        {
            // Roll back the transaction to undo changes
            _transaction?.Rollback();
            _transaction?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}

