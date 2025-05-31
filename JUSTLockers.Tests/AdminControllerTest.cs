using JUSTLockers.Classes;
using JUSTLockers.Controllers;
using JUSTLockers.Service;
using JUSTLockers.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using MySqlConnector;
using System.Data;


namespace JUSTLockers.Testing
{
    public class AdminControllerTest
    {
        private readonly AdminService _service;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly AdminController _controller;
        private readonly IConfiguration _configuration;
        private readonly IStudentService _studentService;
        private readonly Mock<NotificationService> _notificationServiceMock;
        private readonly Mock<AdminService> _adminServiceMock;
        private readonly SupervisorService _supervisorServiceMock;
        private readonly string connectionString = "Server=localhost;Database=testing;User=root;Password=1234;";
        private readonly IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        public AdminControllerTest()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"ConnectionStrings:DefaultConnection", connectionString}
                })
                .Build();

            _configuration = config;
            _studentService = new StudentService(_configuration, memoryCache, _service);
            _service = new AdminService(_configuration, memoryCache);
            _supervisorServiceMock = new SupervisorService(_configuration, _service, memoryCache);
            // Mock only what you need
            _emailServiceMock = new Mock<IEmailService>();
            _notificationServiceMock = new Mock<NotificationService>(_emailServiceMock.Object, _service);

            _controller = new AdminController(
                _service,
                config,
                _emailServiceMock.Object,
                _notificationServiceMock.Object,
                _studentService);

            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        }

        public async Task<T?> GetRandomEntityAsync<T>(string tableName, Func<IDataReader, T> mapFunc)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM {tableName} ORDER BY RAND() LIMIT 1";
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return mapFunc(reader);
            }
            return default;
        }

        public async Task<T?> GetRandomEntityAsync<T>(string tableName, Func<IDataReader, T> mapFunc, string? whereClause = null)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM {tableName}" + (whereClause != null ? $" WHERE {whereClause}" : "") + " ORDER BY RAND() LIMIT 1";
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return mapFunc(reader);
            }
            return default;
        }

        public async Task<int> GetNextIdAsync(string tableName)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT IFNULL(MAX(number_cab), 0) + 1 FROM {tableName}";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        #region CheckEmployeeExists Tests
        // Usage in a test:
        [Fact]
        public async Task CheckEmployeeExists_ShouldReturnTrue_ForRandomEmployee()
        {
            var employee = await GetRandomEntityAsync("Employees", r => new { Id = r.GetInt32(r.GetOrdinal("Id")) });
            Assert.NotNull(employee);
            var result = await _service.CheckEmployeeExists(employee.Id);
            Assert.True(result);
        }

        [Fact]
        public async Task CheckEmployeeExists_ShouldReturnFalse_ForNonExistingEmployee()
        {
            int maxId = 0;
            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM employees";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);
            var result = await _service.CheckEmployeeExists(999999);
            Assert.False(result);
            result = await _service.CheckEmployeeExists(-1);
            Assert.False(result);
            
        }

        #endregion

        #region AddSupervisor Tests
        [Fact]
        public async Task AddSupervisor_ShouldSucceed_WhenValidEmployee_Random()
        {
            // 1. Get a random employee who is not a supervisor
            var employee = await GetRandomEntityAsync(
                "Employees e LEFT JOIN Supervisors s ON e.Id = s.Id",
                r => new { Id = r.GetInt32(r.GetOrdinal("Id")) },
                "s.Id IS NULL"
            );
            Assert.NotNull(employee);

            // Get a random department and location that is NOT already assigned to a supervisor
            var department = await GetRandomEntityAsync(
                "Departments d LEFT JOIN Supervisors s ON d.Name = s.supervised_department AND d.Location = s.Location",
                r => new { Name = r.GetString(r.GetOrdinal("Name")), Location = r.GetString(r.GetOrdinal("Location")) },
                "s.Id IS NULL"
            );
            Assert.NotNull(department);

            var supervisor = new Supervisor
            {
                Id = employee.Id,
                DepartmentName = department.Name,
                Location = department.Location
            };

            // 3. Add supervisor and assert
            var result = await _service.AddSupervisor(supervisor);
            Assert.True(result.Success);
            Assert.Equal("Supervisor added successfully!", result.Message);
        }

        // Overload utility to support WHERE clause
        [Fact]        
        
        public async Task AddSupervisor_ShouldFail_WhenEmployeeNotFound_Random()
        {
            // Find a random employee ID that does NOT exist in the Employees table
            // 1. Get the max ID in Employees
            int maxId = 0;
            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM Employees";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            // 2. Use a random ID above the max (guaranteed not to exist)
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);

            var supervisor = new Supervisor
            {
                Id = nonExistingId,
                DepartmentName = "A",
                Location = "Engineering"
            };
             var result = await _service.AddSupervisor(supervisor);

            Assert.False(result.Success);
            Assert.Equal("Employee not found.", result.Message);
        }

        [Fact]
        public async Task AddSupervisor_ShouldFail_WhenDuplicateId()
        {
            // Get a random existing supervisor from the database
            var existingSupervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new { Id = r.GetInt32(r.GetOrdinal("Id")), DepartmentName = r.IsDBNull(r.GetOrdinal("supervised_department")) ? null : r.GetString(r.GetOrdinal("supervised_department")), Location = r.IsDBNull(r.GetOrdinal("Location")) ? null : r.GetString(r.GetOrdinal("Location")) }
            );
            Assert.NotNull(existingSupervisor);

            var supervisor = new Supervisor
            {
                Id = existingSupervisor.Id, // Existing supervisor ID
                DepartmentName = existingSupervisor.DepartmentName ?? "A",
                Location = existingSupervisor.Location ?? "Engineering"
            };
            var result = await _service.AddSupervisor(supervisor);

            Assert.False(result.Success);
            Assert.Equal("Supervisor ID already exists.", result.Message);
        }

        [Fact]
        public async Task AddSupervisor_ShouldHandleNullDepartment()
        {
            // Get a random employee who is not a supervisor
            var employee = await GetRandomEntityAsync(
                "Employees e LEFT JOIN Supervisors s ON e.Id = s.Id",
                r => new { Id = r.GetInt32(r.GetOrdinal("Id")) },
                "s.Id IS NULL"
            );
            Assert.NotNull(employee);

            var supervisor = new Supervisor
            {
                Id = employee.Id,
                DepartmentName = null,
                Location = null
            };
            var result = await _service.AddSupervisor(supervisor);
            Console.WriteLine(result.Message);
            Assert.False(result.Success);
            Assert.Equal("Department and location cannot be null.", result.Message);
        }
        #endregion

        #region SupervisorExists Tests
        [Fact]
        public async Task SupervisorExists_ShouldReturnTrue_IfExists()
        {
            // Get a random existing supervisor ID from the database
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new { Id = r.GetInt32(r.GetOrdinal("Id")) }
            );
            Assert.NotNull(supervisor);

            var result = await _service.SupervisorExists(supervisor.Id);
            Assert.True(result);
        }

        [Fact]
        public async Task SupervisorExists_ShouldReturnFalse_IfNotExists()
        {
            // Get the max supervisor ID to generate a non-existing ID
            int maxId = 0;
            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM Supervisors";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);

            var result = await _service.SupervisorExists(nonExistingId);
            Assert.False(result);
        }

        [Fact]
        public async Task SupervisorExists_ShouldHandleNegativeId()
        {
            var result = await _service.SupervisorExists(-1);
            Assert.False(result);
        }
        #endregion

        #region AssignCabinet Tests
        [Fact]
        public async Task AssignCabinet_ShouldSucceed_WhenValid()
        {
            // Generate random values for Cabinet properties
            var random = new Random();
            int cabinetNumber = await GetNextIdAsync("Cabinets");
            string[] possibleWings = {"1", "2", "3" ,"4"};
            string[] possibleLocations = { "Engineering", "Medicine"};

            // Get a random department from the database
            var department = await GetRandomEntityAsync(
                "Departments",
                r => new { Name = r.GetString(r.GetOrdinal("Name")), Location = r.GetString(r.GetOrdinal("Location")) }
            );
            Assert.NotNull(department);

            var cabinet = new Cabinet
            {
                CabinetNumber = cabinetNumber,
                Wing = possibleWings[random.Next(possibleWings.Length)],
                Level = random.Next(1, 5),
                Location = department.Location,
                Department = department.Name,
                Capacity = random.Next(1, 20),
                Status = CabinetStatus.IN_SERVICE
            };

            var result = _service.AssignCabinet(cabinet);
            Assert.StartsWith("Cabinet added successfully!", result);
        }

        [Fact]
        public async Task AssignCabinet_ShouldFail_WhenDuplicateNumber()
        {
            var cabinet = new Cabinet
            {
                CabinetNumber = 1, // Existing cabinet number
                Wing = "1",
                Level = 1,
                Location = "Engineering",
                Department = "A",
                Capacity = 10
            };

            var result = _service.AssignCabinet(cabinet);
            Assert.Contains("Error adding cabinet", result);
            
             cabinet = new Cabinet
            {
                CabinetNumber = 21,
                Wing = "1",
                Level = 1,
                Location = "Engineering",
                Department = "NonExist",
                Capacity = 10
            };

             result = _service.AssignCabinet(cabinet);
            Assert.Contains("Error adding cabinet", result);
        }

        #endregion

        #region AssignCovenant Tests
        [Fact]
        public async Task AssignCovenant_ShouldSucceed_WhenValid_Random()
        {
            // 1. Get a random supervisor who exists
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new { Id = r.GetInt32(r.GetOrdinal("Id")) },
                "supervised_department IS NULL AND Location IS NULL"
            );
            Assert.NotNull(supervisor);

            // Example: Get a random department that is NOT assigned to any supervisor
            var department = await GetRandomEntityAsync(
                "Departments d LEFT JOIN Supervisors s ON d.Name = s.supervised_department AND d.Location = s.Location",
                r => new { Name = r.GetString(r.GetOrdinal("Name")), Location = r.GetString(r.GetOrdinal("Location")) },
                "s.Id IS NULL"
            );
            Assert.NotNull(department);

            // 3. Call AssignCovenant
            var result = await _service.AssignCovenant(supervisor.Id, department.Name, department.Location);
            Assert.Contains("Covenant assigned successfully", result);
        }

        [Fact]
        public async Task AssignCovenant_ShouldFail_WhenSupervisorNotFound_Random()
        {
            // Find a random supervisor ID that does NOT exist in the Supervisors table
            int maxId = 0;
            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM Supervisors";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);

            // Use a valid department/location
            var department = await GetRandomEntityAsync(
                "Departments",
                r => new { Name = r.GetString(r.GetOrdinal("Name")), Location = r.GetString(r.GetOrdinal("Location")) }
            );
            Assert.NotNull(department);

            var result = await _service.AssignCovenant(nonExistingId, department.Name, department.Location);
            Assert.Equal("Supervisor does not exist.", result);
        }

        [Fact]
        public async Task AssignCovenant_ShouldFail_WhenInvalidDepartment_Random()
        {
            // Get a random supervisor who exists
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new { Id = r.GetInt32(r.GetOrdinal("Id")) }
            );
            Assert.NotNull(supervisor);

            // Use a department name/location that does not exist
            var result = await _service.AssignCovenant(supervisor.Id, "NonExistDept" + Guid.NewGuid(), "NonExistLoc" + Guid.NewGuid());
            Assert.Contains("Error", result);
        }
        #endregion

        #region DeleteSupervisor Tests
        [Fact]
        public async Task DeleteSupervisor_ShouldSucceed_WhenValid()
        {
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new { Id = r.GetInt32(r.GetOrdinal("Id")) }
            );
            Assert.NotNull(supervisor);
            var result = await _service.DeleteSupervisor(supervisor.Id);
            Assert.Equal("Supervisor deleted successfully.", result);
        }

        [Fact]
        public async Task DeleteSupervisor_ShouldFail_WhenNotFound()
        {
            // Use a random non-existing supervisor ID
            int maxId = 0;
            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM Supervisors";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);

            var result = await _service.DeleteSupervisor(nonExistingId);
            Assert.Equal("Supervisor not found.", result);
        }
        #endregion

        #region DeleteCovenant Tests
        [Fact]
        public async Task DeleteCovenant_ShouldSucceed_WhenAssigned()
        {
            var supervisor = await GetRandomEntityAsync(
                "Supervisors ",
                r => new{Id = r.GetInt32(r.GetOrdinal("Id"))  },
                 "supervised_department IS NOT NULL AND Location IS NOT NULL"
                );
           
            Assert.NotNull(supervisor);
            var result = await _service.DeleteCovenant(supervisor.Id);
            Assert.Equal("Covenant deleted successfully.", result);
        }

        [Fact]
        public async Task DeleteCovenant_ShouldFail_WhenNoCovenant()
        { 
                // Get a random supervisor who has no covenant assigned (DepartmentName and Location are NULL)
                var supervisor = await GetRandomEntityAsync(
                    "Supervisors",
                    r => new { Id = r.GetInt32(r.GetOrdinal("Id")) },
                    "supervised_department IS NULL AND Location IS NULL"
                );
                Assert.NotNull(supervisor);

                var result = await _service.DeleteCovenant(supervisor.Id);
                Assert.Equal("Supervisor doesn't have a covenant assigned.", result);   
        }

        [Fact]
        public async Task DeleteCovenant_ShouldFail_WhenSupervisorNotFound()
        {
            int maxId = 0;
            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM Supervisors";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);

            var result = await _service.DeleteCovenant(nonExistingId);
            Assert.Equal("Supervisor not found.", result);
        }
        #endregion

        #region GetSupervisorById Tests
        [Fact]
        public async Task GetSupervisorById_ShouldReturnSupervisor_WhenExists()
        {
            // Get a random existing supervisor ID from the database
            var supervisorEntity = await GetRandomEntityAsync(
                "Supervisors",
                r => new { Id = r.GetInt32(r.GetOrdinal("Id")) }
            );
            Assert.NotNull(supervisorEntity);

            var supervisor = await _service.GetSupervisorById(supervisorEntity.Id);
            Assert.NotNull(supervisor);
            Assert.Equal(supervisorEntity.Id, supervisor.Id);
        }

        [Fact]
        public async Task GetSupervisorById_ShouldReturnNull_WhenNotExists()
        {
            // Get the max supervisor ID to generate a non-existing ID
            int maxId = 0;
            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM Supervisors";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);

            var supervisor = await _service.GetSupervisorById(nonExistingId);
            Assert.Null(supervisor);
        }
        #endregion

        #region IsDepartmentAssigned Tests
        [Fact]
        public async Task IsDepartmentAssigned_ShouldReturnId_IfAssigned_Random()
        {
            // Get a random department that is assigned to a supervisor
            var assigned = await GetRandomEntityAsync(
                "Departments d JOIN Supervisors s ON d.Name = s.supervised_department AND d.Location = s.Location",
                r => new
                {
                    DepartmentName = r.GetString(r.GetOrdinal("Name")).Trim(),
                    Location = r.GetString(r.GetOrdinal("Location")).Trim(),
                    SupervisorId = r.GetInt32(r.GetOrdinal("Id"))
                }
            );
            Assert.NotNull(assigned);
            var id = await _service.IsDepartmentAssigned(assigned.DepartmentName, assigned.Location);
            Console.WriteLine($"Expected: {assigned.SupervisorId}, Actual: {id}, Dept: '{assigned.DepartmentName}', Loc: '{assigned.Location}'");
            Assert.Equal(assigned.SupervisorId, id);
        }

        [Fact]
        public async Task IsDepartmentAssigned_ShouldReturnZero_IfNotAssigned_Random()
        {
            // Get a random department/location that is NOT assigned to any supervisor
            var unassigned = await GetRandomEntityAsync(
                "Departments d LEFT JOIN Supervisors s ON d.Name = s.supervised_department AND d.Location = s.Location",
                r => new { DepartmentName = r.GetString(r.GetOrdinal("Name")), Location = r.GetString(r.GetOrdinal("Location")) },
                "s.Id IS NULL"
            );
            Assert.NotNull(unassigned);

            var id = await _service.IsDepartmentAssigned(unassigned.DepartmentName, unassigned.Location);
            Assert.Equal(0, id);
        }
        #endregion

        #region Report Management Tests
        [Fact]
        public async Task ResolveReport_ShouldSucceed_WhenValid()
        {
            // Get a random existing report ID
            var report = await GetRandomEntityAsync(
                "Reports",
                r => new { ReportId = r.GetInt32(r.GetOrdinal("Id")) }
            );
            Assert.NotNull(report);

            var result = await _service.SolveReport(report.ReportId, "Test resolution " + Guid.NewGuid());
            Assert.True(result);
        }

        [Fact]
        public async Task ResolveReport_ShouldFail_WhenInvalidId()
        {
            // Get a non-existing report ID
            int maxId = 0;
            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM Reports";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);

            var result = await _service.SolveReport(nonExistingId, "Test resolution " + Guid.NewGuid());
            Assert.False(result);
        }

        [Fact]
        public async Task ReviewReport_ShouldSucceed_WhenValid()
        {
            // Get a random existing report ID
            var report = await GetRandomEntityAsync(
                "Reports",
                r => new { ReportId = r.GetInt32(r.GetOrdinal("Id")) }
            );
            Assert.NotNull(report);

            var result = await _service.ReviewReport(report.ReportId);
            Assert.True(result);
        }

        [Fact]
        public async Task ReviewReport_ShouldFail_WhenInvalidId()
        {
            // Get a non-existing report ID
            int maxId = 0;
            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM Reports";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);

            var result = await _service.ReviewReport(nonExistingId);
            Assert.False(result);
        }

        [Fact]
        public async Task RejectReport_ShouldSucceed_WhenValid()
        {
            // Get a random existing report ID
            var report = await GetRandomEntityAsync(
                "Reports",
                r => new { ReportId = r.GetInt32(r.GetOrdinal("Id")) }
            );
            Assert.NotNull(report);

            var result = await _service.RejectReport(report.ReportId);
            Assert.True(result);
        }

        [Fact]
        public async Task RejectReport_ShouldFail_WhenInvalidId()
        {
            // Get a non-existing report ID
            int maxId = 0;
            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM Reports";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);

            var result = await _service.RejectReport(nonExistingId);
            Assert.False(result);
        }

        [Fact]
        public async Task GetReportDetails_ShouldReturnReport_WhenExists()
        {
            // Get a random existing report ID
            var reportEntity = await GetRandomEntityAsync(
                "Reports",
                r => new { ReportId = r.GetInt32(r.GetOrdinal("Id")) }
            );
            Assert.NotNull(reportEntity);

            var report = await _service.GetReportDetails(reportEntity.ReportId);
            Assert.NotNull(report);
            Assert.Equal(reportEntity.ReportId, report.ReportId);
        }

        [Fact]
        public async Task GetReportDetails_ShouldReturnNull_WhenNotExists()
        {
            // Get a non-existing report ID
            int maxId = 0;
            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(Id), 0) FROM Reports";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);

            var report = await _service.GetReportDetails(nonExistingId);
            Assert.Null(report);
        }
        #endregion

        #region Reallocation Tests
        [Fact]
        public async Task ApproveRequestReallocation_ShouldSucceed_WhenValid()
        {
            // Get a random existing reallocation request with status 'Pending'
            var request = await GetRandomEntityAsync(
                "Reallocation",
                r => new { RequestID = r.GetInt32(r.GetOrdinal("RequestID")) },
                "RequestStatus = 'Pending'"
            );
            Assert.NotNull(request);

            var result = await _service.ApproveRequestReallocation(request.RequestID);
            Assert.True(result);
        }

        [Fact]
        public async Task ApproveRequestReallocation_ShouldFail_WhenInvalidId()
        {
            // Get the max RequestID to generate a non-existing ID
            int maxId = 0;
            using (var connection = new MySqlConnector.MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(RequestID), 0) FROM Reallocation";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);

            var result = await _service.ApproveRequestReallocation(nonExistingId);
            Assert.False(result);
        }

        [Fact]
        public async Task RejectRequestReallocation_ShouldSucceed_WhenValid()
        {
            // Get a random existing reallocation request with status 'Pending'
            var request = await GetRandomEntityAsync(
                "Reallocation",
                r => new { RequestID = r.GetInt32(r.GetOrdinal("RequestID")) },
                "RequestStatus = 'Pending'"
            );
            Assert.NotNull(request);

            var result = await _service.RejectRequestReallocation(request.RequestID);
            Assert.True(result);
        }

        [Fact]
        public async Task ReallocationRequest_ShouldSucceed_WhenValid()
        {
            // 1. Get a random supervisor with a covenant (supervised_department and location not null)
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    Department = r.GetString(r.GetOrdinal("supervised_department")),
                    Location = r.GetString(r.GetOrdinal("Location"))
                },
                "supervised_department IS NOT NULL AND Location IS NOT NULL"
            );
            Assert.NotNull(supervisor);

            // 2. Get a random cabinet assigned to this department/location
            var cabinet = await GetRandomEntityAsync(
                "Cabinets",
                r => new
                {
                    CabinetNumber = r.GetInt32(r.GetOrdinal("number_cab")),
                    Wing = r.GetString(r.GetOrdinal("wing")),
                    Level = r.GetInt32(r.GetOrdinal("level")),
                    CabinetId = r.GetString(r.GetOrdinal("cabinet_id"))
                },
                $"department_name = '{supervisor.Department}' AND location = '{supervisor.Location}'"
            );
            Assert.NotNull(cabinet);

            // 3. Get a different department/location for the request (simulate a valid reallocation)
            var targetDept = await GetRandomEntityAsync(
                "Departments",
                r => new
                {
                    Name = r.GetString(r.GetOrdinal("Name")),
                    Location = r.GetString(r.GetOrdinal("Location"))
                },
                $"(Name != '{supervisor.Department}' OR Location != '{supervisor.Location}')"
            );
            Assert.NotNull(targetDept);

            // 4. Build the reallocation model
            var reallocation = new Reallocation
            {
                SupervisorID = supervisor.Id,
                CurrentDepartment = supervisor.Department,
                RequestedDepartment = targetDept.Name,
                CurrentLocation = supervisor.Location,
                RequestLocation = targetDept.Location,
                RequestWing = cabinet.Wing,
                RequestLevel = cabinet.Level,
                NumberCab = cabinet.CabinetNumber,
                CurrentCabinetID = cabinet.CabinetId
            };

            // 5. Call the method and assert
            var result = await _supervisorServiceMock.ReallocationRequest(reallocation);
            Assert.Contains("Request sent successfully", result);
        }



        #endregion
     
    }
}