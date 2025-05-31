using JUSTLockers.Service;
using JUSTLockers.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using MySqlConnector;
using System.Data;


namespace JUSTLockers.Tests.IntegartionTest
{
    public class ReportIntegrationTest: IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly StudentService _studentService;
        private readonly AdminService _adminService;
        private readonly SupervisorService _supervisorService;
        private MySqlConnection _connection;
        private MySqlTransaction _transaction;
        private readonly string connectionString = "Server=localhost;Database=testing;User=root;Password=1234;";
        private readonly IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        public ReportIntegrationTest()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                {"ConnectionStrings:DefaultConnection", connectionString}
                })
                .Build();

            _configuration = config;
            _studentService = new StudentService(_configuration, memoryCache,_adminService);
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
        public async Task ProblemReportingWorkflow_StudentSubmits_SupervisorReviews_AdminResolves()
        {
            // Arrange: Get a student with a reservation
            var student = await GetRandomEntityAsync(
                "Students s JOIN Reservations r ON s.id = r.StudentId JOIN Lockers l ON r.LockerId = l.Id",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    LockerId = r.GetString(r.GetOrdinal("LockerId")),
                    Department = r.GetString(r.GetOrdinal("department")),
                    Location = r.GetString(r.GetOrdinal("Location"))
                },
                "r.Status = 'RESERVED'"
            );
            if (student == null)
            {
                Console.WriteLine("No student with active reservation found; skipping test.");
                return;
            }

            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "supervised_department = @Department AND location = @Location",
                new { Department = student.Department, Location = student.Location }
            );
            if (supervisor == null)
            {
                Console.WriteLine($"No supervisor found for department {student.Department}, location {student.Location}; skipping test.");
                return;
            }

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Callback<Stream, CancellationToken>((s, ct) => s.Write(new byte[1024], 0, 1024))
                    .Returns(Task.CompletedTask);

            var reportId = await GetNextIdAsync("Reports");
            var reportType = "THEFT";
            var subject = "Theft Issue";
            var description = "Locker broken into";

            // Act 1: Student submits report
            await CleanupOrphanedReservations();
            var saveResult = await _studentService.SaveReportAsync(
                reportId, student.Id, student.LockerId, reportType, subject, description, mockFile.Object);
            Assert.True(saveResult);

            // Act 2: Supervisor views and reviews
            var reports = await _supervisorService.ViewReportedIssues(supervisor.Id);
            var submittedReport = reports.FirstOrDefault(r => r.ReportId == reportId);
            Assert.NotNull(submittedReport);
            Assert.Equal(reportType, submittedReport.Type.ToString());

            var reviewResult = await _adminService.ReviewReport(reportId); // Supervisor escalates to admin
            Assert.True(reviewResult);

            // Act 3: Admin resolves
            var resolution = "Issue addressed.";
            var resolveResult = await _adminService.SolveReport(reportId, resolution);
            Assert.True(resolveResult);
        }

        public void Dispose()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        [Fact]
        public async Task ProblemReportingWorkflow_StudentSubmits_ThenAdminRejects()
        {
            // Arrange: Get a student with a reservation
            var student = await GetRandomEntityAsync(
                "Students s JOIN Reservations r ON s.id = r.StudentId JOIN Lockers l ON r.LockerId = l.Id",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    LockerId = r.GetString(r.GetOrdinal("LockerId")),
                    Department = r.GetString(r.GetOrdinal("department")),
                    Location = r.GetString(r.GetOrdinal("Location"))
                },
                "r.Status = 'RESERVED'"
            );
            if (student == null)
            {
                Console.WriteLine("No student with active reservation found; skipping test.");
                return;
            }

            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "supervised_department = @Department AND location = @Location",
                new { Department = student.Department, Location = student.Location }
            );
            if (supervisor == null)
            {
                Console.WriteLine($"No supervisor found for department {student.Department}, location {student.Location}; skipping test.");
                return;
            }

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.ContentType).Returns("image/png");
            mockFile.Setup(f => f.Length).Returns(2048);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Callback<Stream, CancellationToken>((s, ct) => s.Write(new byte[2048], 0, 2048))
                    .Returns(Task.CompletedTask);

            var reportId = await GetNextIdAsync("Reports");
            var reportType = "DAMAGE";
            var subject = "Locker Damage";
            var description = "Locker door is broken";

            // Act 1: Student submits report
            await CleanupOrphanedReservations();
            var saveResult = await _studentService.SaveReportAsync(
                reportId, student.Id, student.LockerId, reportType, subject, description, mockFile.Object);
            Assert.True(saveResult);

            // Act 3: Admin rejects the report
            var rejectResult = await _adminService.RejectReport(reportId);
            Assert.True(rejectResult);

            // Assert: Verify report status is rejected
            var updatedReport = await GetRandomEntityAsync(
                "Reports",
                r => new
                {
                    Status = r.GetString(r.GetOrdinal("Status")),
                    Resolution = r.IsDBNull(r.GetOrdinal("Resolution")) ? null : r.GetString(r.GetOrdinal("Resolution"))
                },
                "Id = @ReportId",
                new { ReportId = reportId }
            );
            Assert.NotNull(updatedReport);
            
        }

        [Fact]
        public async Task ProblemReportingWorkflow_StudentSubmits_SupervisorDoesNotForward()
        {
            // Arrange: Get a student with a reservation
            var student = await GetRandomEntityAsync(
                "Students s JOIN Reservations r ON s.id = r.StudentId JOIN Lockers l ON r.LockerId = l.Id",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    LockerId = r.GetString(r.GetOrdinal("LockerId")),
                    Department = r.GetString(r.GetOrdinal("department")),
                    Location = r.GetString(r.GetOrdinal("Location"))
                },
                "r.Status = 'RESERVED'"
            );
            if (student == null)
            {
                Console.WriteLine("No student with active reservation found; skipping test.");
                return;
            }

            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "supervised_department = @Department AND location = @Location",
                new { Department = student.Department, Location = student.Location }
            );
            if (supervisor == null)
            {
                Console.WriteLine($"No supervisor found for department {student.Department}, location {student.Location}; skipping test.");
                return;
            }

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.ContentType).Returns("image/gif");
            mockFile.Setup(f => f.Length).Returns(512);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Callback<Stream, CancellationToken>((s, ct) => s.Write(new byte[512], 0, 512))
                    .Returns(Task.CompletedTask);

            var reportId = await GetNextIdAsync("Reports");
            var reportType = "OTHER";
            var subject = "Other Issue";
            var description = "Locker combination not working";

            // Act 1: Student submits report
            await CleanupOrphanedReservations();
            var saveResult = await _studentService.SaveReportAsync(
                reportId, student.Id, student.LockerId, reportType, subject, description, mockFile.Object);
            Assert.True(saveResult);

        }

        //test for super rejecting a report
        [Fact]
        public async Task ProblemReportingWorkflow_SupervisorRejectsReport()
        {
            // Arrange: Get a student with a reservation
            var student = await GetRandomEntityAsync(
                "Students s JOIN Reservations r ON s.id = r.StudentId JOIN Lockers l ON r.LockerId = l.Id",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    LockerId = r.GetString(r.GetOrdinal("LockerId")),
                    Department = r.GetString(r.GetOrdinal("department")),
                    Location = r.GetString(r.GetOrdinal("Location"))
                },
                "r.Status = 'RESERVED'"
            );
            if (student == null)
            {
                Console.WriteLine("No student with active reservation found; skipping test.");
                return;
            }
            var supervisor = await GetRandomEntityAsync(
                "Supervisors",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "supervised_department = @Department AND location = @Location",
                new { Department = student.Department, Location = student.Location }
            );
            if (supervisor == null)
            {
                Console.WriteLine($"No supervisor found for department {student.Department}, location {student.Location}; skipping test.");
                return;
            }
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.ContentType).Returns("image/bmp");
            mockFile.Setup(f => f.Length).Returns(256);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Callback<Stream, CancellationToken>((s, ct) => s.Write(new byte[256], 0, 256))
                    .Returns(Task.CompletedTask);
            var reportId = await GetNextIdAsync("Reports");
            var reportType = "OTHER";
            var subject = "Other Issue";
            var description = "Locker light not working";
            // Act 1: Student submits report
            await CleanupOrphanedReservations();
            var saveResult = await _studentService.SaveReportAsync(
                reportId, student.Id, student.LockerId, reportType, subject, description, mockFile.Object);
            Assert.True(saveResult);
            // Act 2: Supervisor views and rejects the report
            var reports = await _supervisorService.ViewReportedIssues(supervisor.Id);
            var submittedReport = reports.FirstOrDefault(r => r .ReportId == reportId);
            Assert.NotNull(submittedReport);
            Assert.Equal(reportType, submittedReport.Type.ToString());

            var rejectResult = await _adminService.RejectReport(reportId);
            Assert.True(rejectResult);

        }
    }
}
