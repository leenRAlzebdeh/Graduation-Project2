using JUSTLockers.Classes;
using JUSTLockers.Service;
using JUSTLockers.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using MySqlConnector;
using System.Data;

namespace JUSTLockers.Tests.IntegartionTest
{
    public class CabinetReallocationIntegrationTest : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly StudentService _studentService;
        private readonly AdminService _adminService;
        private readonly SupervisorService _supervisorService;
        private MySqlConnection _connection;
        private MySqlTransaction? _transaction;
        private readonly string connectionString = "Server=localhost;Database=testing;User=root;Password=1234;";
        private readonly IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        public CabinetReallocationIntegrationTest()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                {"ConnectionStrings:DefaultConnection", connectionString}
                })
                .Build();

            _configuration = config;
            _emailServiceMock = new Mock<IEmailService>();
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
            cmd.CommandText = $"SELECT * FROM {tableName}" + (whereClause != null ? $" WHERE {whereClause}" : "") + " /*ORDER BY RAND()*/ LIMIT 1";
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

        [Fact]
        public async Task SameDepartmentReallocation_Succeeds_UpdatesCabinetAndLockers()
        {
            // Arrange: Get a supervisor with a covenant
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
                Console.WriteLine("No supervisor with covenant found; skipping test.");
                return;
            }

            // Get a cabinet in their department
            var cabinet = await GetRandomEntityAsync(
                "Cabinets",
                r => new
                {
                    Number = r.GetInt32(r.GetOrdinal("number_cab")),
                    Wing = r.GetString(r.GetOrdinal("wing")),
                    Level = r.GetInt32(r.GetOrdinal("level")),
                    CabinetId = r.GetString(r.GetOrdinal("cabinet_id"))
                },
                "department_name = @Department AND location = @Location",
                new { supervisor.Department, supervisor.Location }
            );
            if (cabinet == null)
            {
                Console.WriteLine("No cabinet found; skipping test.");
                return;
            }

            // Get a different wing/level in the same department
            var newWingLevel = await GetRandomEntityAsync(
                "Cabinets",
                r => new { Wing = r.GetString(r.GetOrdinal("wing")), Level = r.GetInt32(r.GetOrdinal("level")) },
                "department_name = @Department AND location = @Location AND wing != @Wing AND level != @Level",
                new { supervisor.Department, supervisor.Location, cabinet.Wing, cabinet.Level }
            );
            if (newWingLevel == null)
            {
                Console.WriteLine("No different wing/level found; skipping test.");
                return;
            }

            var model = new Reallocation
            {
                SupervisorID = supervisor.Id,
                CurrentDepartment = supervisor.Department,
                RequestedDepartment = supervisor.Department,
                CurrentLocation = supervisor.Location,
                RequestLocation = supervisor.Location,
                RequestWing = newWingLevel.Wing,
                RequestLevel = newWingLevel.Level,
                NumberCab = cabinet.Number,
                CurrentCabinetID = cabinet.CabinetId
            };

            // Act: Perform same-department reallocation
            await CleanupOrphanedReservations();
            var (message, requestId) = await _supervisorService.ReallocationRequestFormSameDep(model);

            // Assert
            Assert.Equal("Cabinet reallocation was successful.", message);
            Assert.True(requestId > 0);

            // Verify cabinet updated
            var updatedCabinet = await GetRandomEntityAsync(
                "Cabinets",
                r => new { Wing = r.GetString(r.GetOrdinal("wing")), Level = r.GetInt32(r.GetOrdinal("level")) },
                "cabinet_id = @CabinetId",
                new { cabinet.CabinetId }
            );
            Assert.NotNull(updatedCabinet);


            // Verify lockers reattached
            var lockers = new List<string>();
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Transaction = _transaction;
                cmd.CommandText = "SELECT Id FROM Lockers WHERE cabinet_id = @CabinetId";
                cmd.Parameters.AddWithValue("@CabinetId", cabinet.CabinetId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lockers.Add(reader.GetString(reader.GetOrdinal("Id")));
                    }
                }
            }
            Assert.NotEmpty(lockers);

            // Verify notification triggered
            _emailServiceMock.Verify(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task CrossDepartmentReallocation_AdminApproves_UpdatesCabinet()
        {
            // Arrange: Get a supervisor with a covenant
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
                Console.WriteLine("No supervisor with covenant found; skipping test.");
                return;
            }

            // Get a cabinet
            var cabinet = await GetRandomEntityAsync(
                "Cabinets",
                r => new
                {
                    Number = r.GetInt32(r.GetOrdinal("number_cab")),
                    Wing = r.GetString(r.GetOrdinal("wing")),
                    Level = r.GetInt32(r.GetOrdinal("level")),
                    CabinetId = r.GetString(r.GetOrdinal("cabinet_id"))
                },
                "department_name = @Department AND location = @Location",
                new { supervisor.Department, supervisor.Location }
            );
            if (cabinet == null)
            {
                Console.WriteLine("No cabinet found; skipping test.");
                return;
            }
            

            // Get a different department
            var targetDept = await GetRandomEntityAsync(
                "Departments",
                r => new { Name = r.GetString(r.GetOrdinal("name")), Location = r.GetString(r.GetOrdinal("Location")) },
                "name != @Department OR Location != @Location",
                new { supervisor.Department, supervisor.Location }
            );
            if (targetDept == null)
            {
                Console.WriteLine("No different department found; skipping test.");
                return;
            }

            var model = new Reallocation
            {
                SupervisorID = supervisor.Id,
                CurrentDepartment = supervisor.Department,
                RequestedDepartment = targetDept.Name,
                CurrentLocation = supervisor.Location,
                RequestLocation = targetDept.Location,
                RequestWing = cabinet.Wing,
                RequestLevel = cabinet.Level,
                NumberCab = cabinet.Number,
                CurrentCabinetID = cabinet.CabinetId
            };

            // Act: Submit cross-department request
            await CleanupOrphanedReservations();
            var message = await _supervisorService.ReallocationRequest(model);

            // Verify pending request
            var request = await GetRandomEntityAsync(
                "Reallocation",
                 r => new { RequestID = r.GetInt32(r.GetOrdinal("RequestID")), RequestStatus = r.GetString(r.GetOrdinal("RequestStatus")), NewCabinetID = r.GetString(r.GetOrdinal("NewCabinetID")) },
                "SupervisorID = @SupervisorID AND CurrentCabinetID = @CabinetId AND RequestedDepartment= @RequestedDepartment AND RequestLocation=@RequestLocation AND RequestStatus='Pending'",
                new { SupervisorID = supervisor.Id, cabinet.CabinetId, RequestedDepartment = targetDept.Name, RequestLocation=supervisor.Location }
            );
            Assert.NotNull(request);
            //var approveResult = await _adminService.ApproveRequestReallocation(request.RequestID);
            var approveResult = await ApproveRequestReallocation(request.RequestID);

            Assert.Equal("Reallocation", approveResult.message);
            Assert.True(approveResult.success);

            _emailServiceMock.Verify(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task CrossDepartmentReallocation_AdminRejects_UpdatesStatus()
        {
            // Arrange: Get a supervisor with a covenant
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

            // Get a cabinet in their department/location
            var cabinet = await GetRandomEntityAsync(
                "Cabinets",
                r => new
                {
                    Number = r.GetInt32(r.GetOrdinal("number_cab")),
                    Wing = r.GetString(r.GetOrdinal("wing")),
                    Level = r.GetInt32(r.GetOrdinal("level")),
                    CabinetId = r.GetString(r.GetOrdinal("cabinet_id"))
                },
                "department_name = @Department AND location = @Location",
                new { supervisor.Department, supervisor.Location }
            );
            if (cabinet == null) return;

            // Get a different department/location
            var targetDept = await GetRandomEntityAsync(
                "Departments",
                r => new { Name = r.GetString(r.GetOrdinal("name")), Location = r.GetString(r.GetOrdinal("Location")) },
                "name != @Department OR Location != @Location",
                new { supervisor.Department, supervisor.Location }
            );
            if (targetDept == null) return;

            var model = new Reallocation
            {
                SupervisorID = supervisor.Id,
                CurrentDepartment = supervisor.Department,
                RequestedDepartment = targetDept.Name,
                CurrentLocation = supervisor.Location,
                RequestLocation = targetDept.Location,
                RequestWing = cabinet.Wing,
                RequestLevel = cabinet.Level,
                NumberCab = cabinet.Number,
                CurrentCabinetID = cabinet.CabinetId
            };

            // Act: Submit reallocation request
            await CleanupOrphanedReservations();
            var message = await _supervisorService.ReallocationRequest(model);
            //Assert.Equal("Request sent successfully! Wait Admin Response", message);

            // Get the created request
            var request = await GetRandomEntityAsync(
                "Reallocation",
                r => new { RequestID = r.GetInt32(r.GetOrdinal("RequestID")) },
                "SupervisorID = @SupervisorID AND CurrentCabinetID = @CabinetId AND RequestedDepartment = @RequestedDepartment AND RequestStatus='Pending'",
                new { SupervisorID = supervisor.Id, CabinetId = cabinet.CabinetId, RequestedDepartment = targetDept.Name }
            );
            Assert.NotNull(request);

            // Admin rejects the request
            var rejectResult = await _adminService.RejectRequestReallocation(request.RequestID);
            Assert.True(rejectResult);


            // Assert: Cabinet department/location unchanged
            var cabinetCheck = await GetRandomEntityAsync(
                "Cabinets",
                r => new { Department = r.GetString(r.GetOrdinal("department_name")), Location = r.GetString(r.GetOrdinal("location")) },
                "cabinet_id = @CabinetId",
                new { cabinet.CabinetId }
            );
            //Assert.NotNull(cabinetCheck);
            Assert.Equal(supervisor.Department, cabinetCheck.Department);
            Assert.Equal(supervisor.Location, cabinetCheck.Location);
        }
        public void Dispose()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        public async Task<(bool success, string message)> ApproveRequestReallocation(int requestId)
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Get reallocation data
                        string selectQuery = @"
                    SELECT 
                        r.RequestedDepartment, 
                        r.RequestLocation, 
                        r.number_cab, 
                        r.RequestWing, 
                        r.RequestLevel,
                        r.CurrentCabinetID,
                        r.CurrentDepartment,
                        r.CurrentLocation,
                        r.RequestStatus
                    FROM Reallocation r
                    WHERE r.RequestID = @RequestID";

                        string? newDepartment = null;
                        string? newLocation = null;
                        int cabinetNumber = 0;
                        string? newWing = null;
                        int newLevel = 0;
                        string? oldCabinetId = null;
                        string? oldDepartment = null;
                        string? oldLocation = null;
                        string? requestStatus = null;

                        using (var selectCmd = new MySqlCommand(selectQuery, connection, transaction))
                        {
                            selectCmd.Parameters.AddWithValue("@RequestID", requestId);
                            using (var reader = await selectCmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    requestStatus = reader["RequestStatus"]?.ToString();
                                    if (requestStatus != "Pending")
                                    {
                                        await transaction.RollbackAsync();
                                        Console.WriteLine($"Request {requestId} is not pending. Status: {requestStatus}");
                                        return (false, $"Request {requestId} is not pending. Status: {requestStatus}");
                                    }
                                    newDepartment = reader["RequestedDepartment"]?.ToString();
                                    newLocation = reader["RequestLocation"]?.ToString();
                                    cabinetNumber = reader["number_cab"] != DBNull.Value ? Convert.ToInt32(reader["number_cab"]) : 0;
                                    newWing = reader["RequestWing"]?.ToString();
                                    newLevel = reader["RequestLevel"] != DBNull.Value ? Convert.ToInt32(reader["RequestLevel"]) : 0;
                                    oldCabinetId = reader["CurrentCabinetID"]?.ToString();
                                    oldDepartment = reader["CurrentDepartment"]?.ToString();
                                    oldLocation = reader["CurrentLocation"]?.ToString();
                                }
                                else
                                {
                                    await transaction.RollbackAsync();
                                    Console.WriteLine($"Request {requestId} not found.");
                                    return (false, $"Request {requestId} not found.");
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(oldCabinetId))
                        {
                            await transaction.RollbackAsync();
                            Console.WriteLine($"Invalid old cabinet ID for request {requestId}.");
                            return (false, $"Invalid old cabinet ID for request {requestId}.");
                        }

                        string updateCabinetQuery = @"
                    UPDATE Cabinets 
                    SET 
                        department_name = @NewDepartment,
                        wing = @NewWing,
                        level = @NewLevel,
                        location = @NewLocation
                    WHERE cabinet_id = @OldCabinetId";

                        using (var updateCmd = new MySqlCommand(updateCabinetQuery, connection, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@NewDepartment", newDepartment);
                            updateCmd.Parameters.AddWithValue("@NewWing", newWing);
                            updateCmd.Parameters.AddWithValue("@NewLevel", newLevel);
                            updateCmd.Parameters.AddWithValue("@NewLocation", newLocation);
                            updateCmd.Parameters.AddWithValue("@OldCabinetId", oldCabinetId);

                            int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                            if (rowsAffected == 0)
                            {
                                await transaction.RollbackAsync();
                                Console.WriteLine($"Failed to update cabinet {oldCabinetId}. Rows affected: {rowsAffected}. " +
        $"Parameters: department_name={newDepartment}, wing={newWing}, level={newLevel}, location={newLocation}");
                                return (false, $"Failed to update cabinet {oldCabinetId}. Rows affected: {rowsAffected}. " +
        $"Parameters: department_name={newDepartment}, wing={newWing}, level={newLevel}, location={newLocation}");
                            }
                        }

                        // Get the new cabinet_id that was generated
                        string? newCabinetId = null;
                        string getNewCabinetIdQuery = @"
    SELECT cabinet_id FROM Cabinets 
    WHERE department_name = @NewDepartment 
    AND wing = @NewWing 
    AND level = @NewLevel 
    AND number_cab = @CabinetNumber";

                        using (var getCabinetCmd = new MySqlCommand(getNewCabinetIdQuery, connection, transaction))
                        {
                            getCabinetCmd.Parameters.AddWithValue("@NewDepartment", newDepartment);
                            getCabinetCmd.Parameters.AddWithValue("@NewWing", newWing);
                            getCabinetCmd.Parameters.AddWithValue("@NewLevel", newLevel);
                            getCabinetCmd.Parameters.AddWithValue("@CabinetNumber", cabinetNumber);

                            newCabinetId = await getCabinetCmd.ExecuteScalarAsync() as string;
                            if (string.IsNullOrEmpty(newCabinetId))
                            {
                                await transaction.RollbackAsync();
                                Console.WriteLine($"Failed to retrieve new cabinet ID for {newDepartment}, {newWing}, {newLevel}, {cabinetNumber}.");
                                return (false, $"Failed to retrieve new cabinet ID for {newDepartment}, {newWing}, {newLevel}, {cabinetNumber}.");
                            }
                        }



                        var lockerIds = new List<(string OldId, string NewId)>();
                        string selectLockersQuery = @"
    SELECT Id 
    FROM Lockers 
    WHERE Id LIKE CONCAT(@OldCabinetIdPattern, '%')";

                        using (var selectLockersCmd = new MySqlCommand(selectLockersQuery, connection, transaction))
                        {
                            selectLockersCmd.Parameters.AddWithValue("@OldDepartment", oldDepartment);
                            selectLockersCmd.Parameters.AddWithValue("@OldCabinetIdPattern", oldCabinetId);
                            using (var reader = await selectLockersCmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    string oldLockerId = reader["Id"]?.ToString() ?? string.Empty;
                                    string newLockerId = $"{newCabinetId}-{oldLockerId.Split('-').Last()}";
                                    lockerIds.Add((oldLockerId, newLockerId));
                                }
                            }
                        }


                        // Update lockers
                        foreach (var (oldLockerId, newLockerId) in lockerIds)
                        {
                            string updateLockerQuery = @"
                            UPDATE Lockers 
                            SET 
                                Id = @NewLockerId
                            WHERE Id = @OldLockerId";

                            using (var lockerCmd = new MySqlCommand(updateLockerQuery, connection, transaction))
                            {
                                lockerCmd.Parameters.AddWithValue("@NewLockerId", newLockerId);
                                lockerCmd.Parameters.AddWithValue("@NewDepartment", newDepartment);
                                lockerCmd.Parameters.AddWithValue("@NewCabinetId", newCabinetId);
                                lockerCmd.Parameters.AddWithValue("@OldLockerId", oldLockerId);
                                int rowsAffected = await lockerCmd.ExecuteNonQueryAsync();
                                if (rowsAffected == 0)
                                {
                                    await transaction.RollbackAsync();
                                    Console.WriteLine($"Failed to update locker {oldLockerId} to {newLockerId}.");
                                    return (false, $"Failed to update locker {oldLockerId} to {newLockerId}.");
                                }
                            }
                        }


                        // Mark reallocation request as approved
                        string approveQuery = @"
                    UPDATE Reallocation 
                    SET RequestStatus = 'Approved'    
                    WHERE RequestID = @RequestID";

                        using (var approveCmd = new MySqlCommand(approveQuery, connection, transaction))
                        {
                            approveCmd.Parameters.AddWithValue("@RequestID", requestId);
                            await approveCmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                        Console.WriteLine($"Reallocation request {requestId} approved successfully.");
                        return (true, $"Reallocation request {requestId} approved successfully.");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Error approving reallocation {requestId}: {ex.Message}\nStack Trace: {ex.StackTrace}");
                        return (false, $"Error approving reallocation {requestId}: {ex.Message}\nStack Trace: {ex.StackTrace}"); // Changed to return false instead of rethrowing for testability
                    }
                }
            }
        }
    }
}

