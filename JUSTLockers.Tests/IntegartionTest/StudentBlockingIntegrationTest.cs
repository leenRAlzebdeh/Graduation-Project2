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
    public class StudentBlockingIntegrationTest: IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly StudentService _studentService;
        private readonly AdminService _adminService;
        private readonly SupervisorService _supervisorService;
        private MySqlConnection _connection;
        private MySqlTransaction _transaction;
        private readonly string connectionString = "Server=localhost;Database=testing;User=root;Password=1234;";
        private readonly IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        public StudentBlockingIntegrationTest()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                {"ConnectionStrings:DefaultConnection", connectionString}
                })
                .Build();

            _configuration = config;
            _studentService = new StudentService(_configuration, memoryCache, _adminService);
            _adminService = new AdminService(_configuration, memoryCache);
            _supervisorService = new SupervisorService(_configuration, _adminService, memoryCache);

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

        private async Task CleanupOrphanedReservations()
        {
            var cmd = _connection.CreateCommand();
            cmd.Transaction = _transaction;
            cmd.CommandText = @"
                DELETE FROM Reservations 
                WHERE LockerId NOT IN (
                    SELECT COALESCE(locker_id, '') FROM Students WHERE locker_id IS NOT NULL
                )";
            await cmd.ExecuteNonQueryAsync();
        }
        #endregion

        [Fact]
        public async Task StudentBlockingWorkflow_BlockAndUnblock_UpdatesBlockList()
        {
            // Arrange: Get a supervisor and unblocked student with reservation
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("supervised_department")),
                    Location = r.GetString(r.GetOrdinal("location"))
                },
                "supervised_department IS NOT NULL AND location IS NOT NULL"
            );
            if (supervisor == null)
            {
                Console.WriteLine("No supervisor found; skipping test.");
                return;
            }

            var student = await GetRandomEntityAsync(
                "Students s JOIN Reservations r ON s.id = r.StudentId LEFT JOIN BlockList b ON s.id = b.student_id",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "s.department = @Department AND s.Location = @Location AND r.Status = 'RESERVED' AND b.student_id IS NULL",
                new { Department = supervisor.Department, Location = supervisor.Location }
            );
            if (student == null)
            {
                Console.WriteLine("No unblocked student with reservation found; skipping test.");
                return;
            }

            // Act 1: Block student
            await CleanupOrphanedReservations();
            var blockMessage = _supervisorService.BlockStudent(student.Id, supervisor.Id);
            Assert.Equal("Student successfully blocked.", blockMessage);

            // Assert: Verify BlockList and reservation
            var blockEntry = await GetRandomEntityAsync(
                "BlockList",
                r => new { StudentId = r.GetInt32(r.GetOrdinal("student_id")) },
                "student_id = @StudentId AND blocked_by = @BlockedBy",
                new { StudentId = student.Id, BlockedBy = supervisor.Id }
            );
            Assert.NotNull(blockEntry);

            var reservation = await GetRandomEntityAsync(
                "Reservations",
                r => new { Status = r.GetString(r.GetOrdinal("Status")) },
                "StudentId = @StudentId",
                new { StudentId = student.Id }
            );
            Assert.Null(reservation); // Reservation canceled

            // Act 2: Attempt reservation
            var wings = await _studentService.GetAvailableWingsAndLevels(supervisor.Department, supervisor.Location);
            if (!wings.Any()) return;
            var lockerId = await _studentService.ReserveLockerInWingAndLevel(student.Id, supervisor.Department, supervisor.Location, wings[0].Wing, wings[0].Level);
            Assert.Null(lockerId);

            // Act 3: Unblock student
            var unblockMessage = _supervisorService.UnblockStudent(student.Id, supervisor.Id);
            Assert.Equal("Student successfully Unblocked.", unblockMessage);

            // Assert: Verify BlockList cleared
            var blockCheck = await GetRandomEntityAsync(
                "BlockList",
                r => new { StudentId = r.GetInt32(r.GetOrdinal("student_id")) },
                "student_id = @StudentId",
                new { StudentId = student.Id }
            );
            Assert.Null(blockCheck);
        }

        public void Dispose()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        [Fact]
        public async Task BlockedStudent_CannotReserveLocker()
        {
            // Arrange: Get a supervisor and a student in their department/location
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("supervised_department")),
                    Location = r.GetString(r.GetOrdinal("location"))
                },
                "supervised_department IS NOT NULL AND location IS NOT NULL"
            );
            if (supervisor == null) return;

            var student = await GetRandomEntityAsync(
                "Students s LEFT JOIN BlockList b ON s.id = b.student_id",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "s.department = @Department AND s.Location = @Location AND b.student_id IS NULL",
                new { Department = supervisor.Department, Location = supervisor.Location }
            );
            if (student == null) return;

            // Act: Block the student
            await CleanupOrphanedReservations();
            var blockMessage = _supervisorService.BlockStudent(student.Id, supervisor.Id);
            Assert.Equal("Student successfully blocked.", blockMessage);

            // Try to reserve a locker
            var wings = await _studentService.GetAvailableWingsAndLevels(supervisor.Department, supervisor.Location);
            if (!wings.Any()) return;
            var lockerId = await _studentService.ReserveLockerInWingAndLevel(student.Id, supervisor.Department, supervisor.Location, wings[0].Wing, wings[0].Level);
            Assert.Null(lockerId);
        }

        [Fact]
        public async Task UnblockedStudent_CanReserveLocker()
        {
            // Arrange: Get a supervisor and a student in their department/location
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("supervised_department")),
                    Location = r.GetString(r.GetOrdinal("location"))
                },
                "supervised_department IS NOT NULL AND location IS NOT NULL"
            );
            if (supervisor == null) return;

            var student = await GetRandomEntityAsync(
                "Students s LEFT JOIN BlockList b ON s.id = b.student_id",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "s.department = @Department AND s.Location = @Location AND b.student_id IS NULL",
                new { Department = supervisor.Department, Location = supervisor.Location }
            );
            if (student == null) return;

            // Block and then unblock the student
            await CleanupOrphanedReservations();
            _supervisorService.BlockStudent(student.Id, supervisor.Id);
            _supervisorService.UnblockStudent(student.Id, supervisor.Id);

            // Act: Try to reserve a locker
            var wings = await _studentService.GetAvailableWingsAndLevels(supervisor.Department, supervisor.Location);
            if (!wings.Any()) return;
            var lockerId = await _studentService.ReserveLockerInWingAndLevel(student.Id, supervisor.Department, supervisor.Location, wings[0].Wing, wings[0].Level);

            // Assert: Should be able to reserve
            Assert.NotNull(lockerId);
        }

        [Fact]
        public async Task BlockedStudent_CannotBeBlockedAgain()
        {
            // Arrange: Get a supervisor and a student in their department/location
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("supervised_department")),
                    Location = r.GetString(r.GetOrdinal("location"))
                },
                "supervised_department IS NOT NULL AND location IS NOT NULL"
            );
            if (supervisor == null) return;

            var student = await GetRandomEntityAsync(
                "Students s LEFT JOIN BlockList b ON s.id = b.student_id",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "s.department = @Department AND s.Location = @Location AND b.student_id IS NULL",
                new { Department = supervisor.Department, Location = supervisor.Location }
            );
            if (student == null) return;

            // Block the student
            await CleanupOrphanedReservations();
            var blockMessage1 = _supervisorService.BlockStudent(student.Id, supervisor.Id);
            Assert.Equal("Student successfully blocked.", blockMessage1);

            // Try to block again
            var blockMessage2 = _supervisorService.BlockStudent(student.Id, supervisor.Id);
            Assert.Equal("Student is already blocked.", blockMessage2);
        }

        [Fact]
        public async Task UnblockedStudent_CannotBeUnblockedAgain()
        {
            // Arrange: Get a supervisor and a student in their department/location
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("supervised_department")),
                    Location = r.GetString(r.GetOrdinal("location"))
                },
                "supervised_department IS NOT NULL AND location IS NOT NULL"
            );
            if (supervisor == null) return;

            var student = await GetRandomEntityAsync(
                "Students s LEFT JOIN BlockList b ON s.id = b.student_id",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "s.department = @Department AND s.Location = @Location AND b.student_id IS NULL",
                new { Department = supervisor.Department, Location = supervisor.Location }
            );
            if (student == null) return;

            // Ensure student is not blocked
            await CleanupOrphanedReservations();
            var unblockMessage1 = _supervisorService.UnblockStudent(student.Id, supervisor.Id);
            Assert.NotEqual("Student successfully Unblocked.", unblockMessage1);
        }
    }
}

