using Dapper;
using JUSTLockers.Classes;
using JUSTLockers.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using MySqlConnector;
using System.Data;

namespace JUSTLockers.Testing
{
    public class SupervisorServiceTest
    {
        private readonly SupervisorService _service;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<AdminService> _adminServiceMock;
        private readonly IConfiguration _configuration;
        private readonly string connectionString = "Server=localhost;Database=testing;User=root;Password=1234;";
        public SupervisorServiceTest()
        {
           

            // Create an in-memory configuration
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"ConnectionStrings:DefaultConnection", connectionString}
                })
                .Build();

            _configuration = config;
            _adminServiceMock = new Mock<AdminService>(config);

            _service = new SupervisorService(config, _adminServiceMock.Object);
        }

        #region Helper Methods
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

        public async Task<int> GetNextIdAsync(string tableName, string idColumn = "Id")
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT IFNULL(MAX({idColumn}), 0) + 1 FROM {tableName}";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        #endregion

        #region ViewReportedIssues Tests
        [Fact]
        public async Task ViewReportedIssues_ShouldReturnReports_WhenReportsExist()
        {
            // Get a random supervisor with reports
            var supervisor = await GetRandomEntityAsync(
                "Supervisors s JOIN Reports r ON s.supervised_department = (SELECT department FROM Students WHERE id = r.ReporterId)",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) }
            );
            Assert.NotNull(supervisor);

            var reports = await _service.ViewReportedIssues(supervisor.Id);
            Assert.NotNull(reports);
            Assert.NotEmpty(reports);
        }

        [Fact]
        public async Task ViewReportedIssues_ShouldReturnEmptyList_WhenNoReports()
        {
            // Get a supervisor with no reports in their department
            var supervisor = await GetRandomEntityAsync(
                "Supervisors s LEFT JOIN Reports r ON s.supervised_department = (SELECT department FROM Students WHERE id = r.ReporterId)",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "r.Id IS NULL"
            );
            Assert.NotNull(supervisor);

            var reports = await _service.ViewReportedIssues(supervisor.Id);
            Assert.NotNull(reports);
            Assert.Empty(reports);
        }

        [Fact]
        public async Task ViewReportedIssues_ShouldHandleNullUserId()
        {
            var reports = await _service.ViewReportedIssues(null);
            Assert.NotNull(reports);
            Assert.Empty(reports);
        }
        #endregion

        #region TheftIssues Tests
        [Fact]
        public async Task TheftIssues_ShouldReturnTheftReports_WhenFilterIsTheft()
        {
            var reports = await _service.TheftIssues("THEFT");
            Assert.NotNull(reports);
            Assert.All(reports, r => Assert.Equal(ReportType.THEFT, r.Type));
        }

        [Fact]
        public async Task TheftIssues_ShouldReturnAllReports_WhenFilterIsNull()
        {
            var reports = await _service.TheftIssues(null);
            Assert.NotNull(reports);
            Assert.True(reports.Count > 0);
        }
        #endregion

        #region ViewAllStudentReservations Tests
        [Fact]
        public async Task ViewAllStudentReservations_ShouldReturnStudents_WhenReservationsExist()
        {
            // Get a supervisor with students in their department
            var supervisor = await GetRandomEntityAsync(
                "Supervisors s JOIN Students st ON s.supervised_department = st.department AND s.location = st.Location",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) }
            );
            Assert.NotNull(supervisor);

            var students = await _service.ViewAllStudentReservations(supervisor.Id);
            Assert.NotNull(students);
            Assert.NotEmpty(students);
        }

        [Fact]
        public async Task ViewAllStudentReservations_ShouldReturnEmptyList_WhenNoReservations()
        {
            // Get a supervisor with no students in their department
            var supervisor = await GetRandomEntityAsync(
                "Supervisors s LEFT JOIN Students st ON s.supervised_department = st.department AND s.location = st.Location",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "st.id IS NULL"
            );
            Assert.NotNull(supervisor);

            var students = await _service.ViewAllStudentReservations(supervisor.Id);
            Assert.NotNull(students);
            Assert.Empty(students);
        }

        [Fact]
        public async Task ViewAllStudentReservations_ShouldFilterByStudentId()
        {
            // Get a student with a reservation
            var student = await GetRandomEntityAsync(
                "Students s JOIN Reservations r ON s.id = r.StudentId",
                r => new {
                    StudentId = r.GetInt32(r.GetOrdinal("StudentId")),
                    Department = r.GetString(r.GetOrdinal("department"))
                }
            );
            Assert.NotNull(student);

            // Get a supervisor for that department
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                $"supervised_department = '{student.Department}'"
            );
            Assert.NotNull(supervisor);

            var students = await _service.ViewAllStudentReservations(supervisor.Id, student.StudentId);
            Assert.NotNull(students);
            Assert.Single(students);
            Assert.Equal(student.StudentId, students[0].Id);
        }
        #endregion

        #region ReallocationRequestFormSameDep Tests
        [Fact]
        public async Task ReallocationRequestFormSameDep_ShouldSucceed_WhenValid()
        {
            // Get a supervisor with covenant
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("supervised_department")),
                    Location = r.GetString(r.GetOrdinal("location"))
                },
                "supervised_department IS NOT NULL AND location IS NOT NULL"
            );
            Assert.NotNull(supervisor);

            // Get department info to know available wings
            var department = await GetRandomEntityAsync(
                "Departments",
                r => new {
                    Name = r.GetString(r.GetOrdinal("name")),
                    TotalWings = r.GetInt32(r.GetOrdinal("total_wings"))
                },
                $"name = '{supervisor.Department}' AND Location = '{supervisor.Location}'"
            );
            Assert.NotNull(department);
            Assert.True(department.TotalWings > 0, "Department should have at least one wing");

            // Get a random cabinet from same department to use as reference
            var existingCabinet = await GetRandomEntityAsync(
                "Cabinets",
                r => new {
                    Number = r.GetInt32(r.GetOrdinal("number_cab")),
                    Wing = r.GetString(r.GetOrdinal("wing")),
                    Level = r.GetInt32(r.GetOrdinal("level")),
                    CabinetId = r.GetString(r.GetOrdinal("cabinet_id"))
                },
                $"department_name = '{supervisor.Department}' AND location = '{supervisor.Location}'"
            );
            Assert.NotNull(existingCabinet);

            // Choose a random wing (1 to total_wings)
            var random = new Random();
            string newWing = random.Next(1, department.TotalWings + 1).ToString();

            // Choose a random level (1 to 3)
            int newLevel = random.Next(1, 4);

            // Ensure we don't accidentally select the same wing/level as existing cabinet
            while (newWing == existingCabinet.Wing && newLevel == existingCabinet.Level)
            {
                newWing = random.Next(1, department.TotalWings + 1).ToString();
                newLevel = random.Next(1, 4);
            }

            var model = new Reallocation
            {
                SupervisorID = supervisor.Id,
                CurrentDepartment = supervisor.Department,
                RequestedDepartment = supervisor.Department,
                CurrentLocation = supervisor.Location,
                RequestLocation = supervisor.Location,
                RequestWing = newWing,
                RequestLevel = newLevel,
                NumberCab = existingCabinet.Number,
                CurrentCabinetID = existingCabinet.CabinetId
            };

            var (message, requestId) = await _service.ReallocationRequestFormSameDep(model);
            Assert.Equal("Cabinet reallocation was successful.", message);
            Assert.True(requestId > 0);
        }

        [Fact]
        public async Task ReallocationRequestFormSameDep_ShouldFail_WhenSupervisorNotFound()
        {
            int maxId = 0;
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(id), 0) FROM Supervisors";
                    var results = await cmd.ExecuteScalarAsync();
                    maxId = Convert.ToInt32(results);
                }
            }
            var random = new Random();
            int nonExistingId = maxId + random.Next(1, 10000);

            var model = new Reallocation { SupervisorID = nonExistingId };
            var (message, requestId) = await _service.ReallocationRequestFormSameDep(model);
            Assert.Equal("Supervisor not found.", message);
            Assert.Equal(0, requestId);
        }
        #endregion

        #region ReallocationRequest Tests
        [Fact]
        public async Task ReallocationRequest_ShouldSucceed_WhenValid()
        {
            // Get a supervisor with covenant
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("supervised_department")),
                    Location = r.GetString(r.GetOrdinal("location"))
                },
                "supervised_department IS NOT NULL AND location IS NOT NULL"
            );
            Assert.NotNull(supervisor);

            // Get a different department
            var targetDept = await GetRandomEntityAsync(
                "Departments",
                r => new {
                    Name = r.GetString(r.GetOrdinal("name")),
                    Location = r.GetString(r.GetOrdinal("Location"))
                },
                $"name != '{supervisor.Department}' OR Location != '{supervisor.Location}'"
            );
            Assert.NotNull(targetDept);

            // Get a cabinet from current department
            var cabinet = await GetRandomEntityAsync(
                "Cabinets",
                r => new {
                    Number = r.GetInt32(r.GetOrdinal("number_cab")),
                    Wing = r.GetString(r.GetOrdinal("wing")),
                    Level = r.GetInt32(r.GetOrdinal("level")),
                    CabinetId = r.GetString(r.GetOrdinal("cabinet_id"))
                },
                $"department_name = '{supervisor.Department}' AND location = '{supervisor.Location}'"
            );
            Assert.NotNull(cabinet);

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

            var message = await _service.ReallocationRequest(model);
            Assert.Equal("Request sent successfully! Wait Admin Response", message);
        }
        #endregion

        #region BlockedStudents Tests
        [Fact]
        public async Task BlockedStudents_ShouldReturnList_WhenBlockedStudentsExist()
        {
            // Get a blocked student
            var blockedStudent = await GetRandomEntityAsync(
                "BlockList",
                r => new { StudentId = r.GetInt32(r.GetOrdinal("student_id")) }
            );

            if (blockedStudent != null)
            {
                var blockedStudents = await _service.BlockedStudents();
                Assert.NotNull(blockedStudents);
                Assert.NotEmpty(blockedStudents);
            }
            else
            {
                // If no blocked students exist, the list should be empty
                var blockedStudents = await _service.BlockedStudents();
                Assert.NotNull(blockedStudents);
                Assert.Empty(blockedStudents);
            }
        }
        #endregion

        #region GetStudentById Tests
        [Fact]
        public async Task GetStudentById_ShouldReturnStudent_WhenExists()
        {
            var student = await GetRandomEntityAsync(
                "Students",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) }
            );
            Assert.NotNull(student);

            var result = _service.GetStudentById(student.Id);
            Assert.NotNull(result);
            Assert.Equal(student.Id, result.Id);
        }
        #endregion

        #region Block/Unblock Student Tests
        [Fact]
        public async Task BlockStudent_ShouldSucceed_WhenValid()
        {
            // Get a supervisor with covenant
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("supervised_department")),
                    Location = r.GetString(r.GetOrdinal("location"))
                },
                "supervised_department IS NOT NULL AND location IS NOT NULL"
            );
            Assert.NotNull(supervisor+"super");

            // Get a student in same department
            var student = await GetRandomEntityAsync(
                "Students",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                $"department = '{supervisor.Department}' AND Location = '{supervisor.Location}' AND id NOT IN (SELECT student_id FROM BlockList)"
            );
            Assert.NotNull(student+"student");

            var message = _service.BlockStudent(student.Id, supervisor.Id);
            Assert.Equal("Student successfully blocked.", message);
        }

        [Fact]
        public async Task UnblockStudent_ShouldSucceed_WhenValid()
        {
            // Get a blocked student
            var blockedStudent = await GetRandomEntityAsync(
                "BlockList",
                r => new {
                    StudentId = r.GetInt32(r.GetOrdinal("student_id")),
                    BlockedBy = r.GetInt32(r.GetOrdinal("blocked_by"))
                }
            );

            if (blockedStudent == null)
            {
                // If no blocked students, create one
                var supervisor = await GetRandomEntityAsync(
                    "Supervisors",
                    r => new { Id = r.GetInt32(r.GetOrdinal("id")) }
                );
                Assert.NotNull(supervisor);

                var student = await GetRandomEntityAsync(
                    "Students",
                    r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                    $"department = (SELECT supervised_department FROM Supervisors WHERE id = {supervisor.Id})"
                );
                Assert.NotNull(student);

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"INSERT INTO BlockList (student_id, blocked_by) VALUES ({student.Id}, {supervisor.Id})";
                await cmd.ExecuteNonQueryAsync();

                blockedStudent = new { StudentId = student.Id, BlockedBy = supervisor.Id };
            }

            var message = _service.UnblockStudent(blockedStudent.StudentId, blockedStudent.BlockedBy);
            Assert.Equal("Student successfully Unblocked.", message);
        }
        #endregion

        #region ViewCabinetInfo Tests
        [Fact]
        public async Task ViewCabinetInfo_ShouldReturnCabinets_WhenExist()
        {
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("supervised_department")),
                    Location = r.GetString(r.GetOrdinal("location"))
                },
                "supervised_department IS NOT NULL AND location IS NOT NULL"
            );
            Assert.NotNull(supervisor);

            var cabinets = await _service.ViewCabinetInfo(supervisor.Id, null, null, null, null, null);
            Assert.NotNull(cabinets);
            Assert.NotEmpty(cabinets);
        }
        #endregion

        #region Department Info Tests
        [Fact]
        public async Task GetDepartmentInfo_ShouldReturnInfo_WhenSupervisorExists()
        {
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Department = r.GetString(r.GetOrdinal("supervised_department")),
                    Location = r.GetString(r.GetOrdinal("location"))
                },
                "supervised_department IS NOT NULL AND location IS NOT NULL"
            );
            Assert.NotNull(supervisor);

            var info = await _service.GetDepartmentInfo(supervisor.Id);
            Assert.NotNull(info);
            Assert.Equal(supervisor.Department, info.DepartmentName);
            Assert.Equal(supervisor.Location, info.Location);
        }
        #endregion

        #region HasCovenant Tests
        [Fact]
        public async Task HasCovenant_ShouldReturnTrue_WhenCovenantAssigned()
        {
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "supervised_department IS NOT NULL AND location IS NOT NULL"
            );
            Assert.NotNull(supervisor);

            var hasCovenant = await _service.HasCovenant(supervisor.Id);
            Assert.True(hasCovenant);
        }
        #endregion
    }
}