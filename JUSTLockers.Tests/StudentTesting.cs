using JUSTLockers.Classes;
using JUSTLockers.Service;
using JUSTLockers.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MySqlConnector;
using System.Data;

namespace JUSTLockers.Testing
{
    public class StudentServiceTest
    {
        private readonly StudentService _service;
        private readonly IConfiguration _configuration;
        private readonly AdminService _serviceAdmin;
        private readonly string connectionString = "Server=localhost;Database=testing;User=root;Password=1234;";
        private readonly IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        public StudentServiceTest()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"ConnectionStrings:DefaultConnection", connectionString}
                })
                .Build();

            _configuration = config;
            _serviceAdmin = new AdminService(_configuration, memoryCache);
            _service = new StudentService(config, memoryCache, _serviceAdmin);
        }
        [Fact]
        public void Setup()
        {
            // Ensure the database is in a known state before each test
            using var connection = new MySqlConnection(connectionString);
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
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

        #region SaveReportAsync Tests
        [Fact]
        public async Task SaveReportAsync_ShouldReturnTrue_WhenReportIsSavedSuccessfully()
        {
            // Arrange
            var student = await GetRandomEntityAsync(
                "Students s JOIN Reservations r ON s.id = r.StudentId JOIN Lockers l ON r.LockerId = l.Id",
                r => new
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    LockerId = r.GetString(r.GetOrdinal("LockerId"))
                },
                "r.Status = 'RESERVED'"
            );
            Assert.NotNull(student);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Callback<Stream, CancellationToken>((s, ct) => s.Write(new byte[1024], 0, 1024))
                    .Returns(Task.CompletedTask);

            var reportId = await GetNextIdAsync("Reports");

            // Use a switch to select a different report type each time
            string[] reportTypes = { "THEFT", "MAINTENANCE", "LOCKED_LOCKER", "OTHER" };
            string reportType = reportTypes[reportId % reportTypes.Length];
            string subject, description;

            switch (reportType)
            {
                case "THEFT":
                    subject = "Theft Issue";
                    description = "Locker broken into";
                    break;
                case "MAINTENANCE":
                    subject = "Damage Issue";
                    description = "Locker is damaged";
                    break;
                case "LOCKED_LOCKER":
                    subject = "Lost Item";
                    description = "Lost item in locker";
                    break;
                default:
                    subject = "Other Issue";
                    description = "Other problem reported";
                    break;
            }

            // Act
            var result = await _service.SaveReportAsync(
                reportId,
                student.Id,
                student.LockerId,
                reportType,
                subject,
                description,
                mockFile.Object
            );

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task SaveReportAsync_ShouldReturnFalse_WhenExceptionOccurs()
        {
            // Arrange
            var student = await GetRandomEntityAsync(
                "Students s JOIN Lockers l ON s.locker_id = l.Id",
               r => new {
                   Id = r.GetInt32(r.GetOrdinal("id")),
                   LockerId = r.GetString(r.GetOrdinal("locker_id"))
               }
            );
            Assert.NotNull(student);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile.Setup(f => f.Length).Returns(1024);
            // Simulate an exception during file copy
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Throws(new IOException("Simulated file copy error"));

            var reportId = await GetNextIdAsync("Reports");

            // Act
            var result = await _service.SaveReportAsync(
                reportId,
                student.Id,
                student.LockerId,
                "THEFT",
                "Theft Issue",
                "Locker broken into",
                mockFile.Object
            );

            // Assert
            Assert.False(result);

            // Verify no report was saved
            var savedReport = await GetRandomEntityAsync(
                "Reports",
                r => new { Id = r.GetInt32(r.GetOrdinal("Id")) },
                $"Id = {reportId}"
            );
            Assert.Null(savedReport);
        }
        #endregion

        #region DeleteReport Tests
        [Fact]
        public async Task DeleteReport_ShouldExecuteDeleteCommand()
        {
            var report = await GetRandomEntityAsync(
                "Reports",
                r => new { Id = r.GetInt32(r.GetOrdinal("Id")) }
            );
            Assert.NotNull(report);

            await _service.DeleteReport(report.Id);

            var deleted = await GetRandomEntityAsync("Reports", r => new { Id = r.GetInt32(r.GetOrdinal("Id")) }, $"Id = {report.Id}");
            Assert.Null(deleted);
        }
        #endregion

        #region GetReportByAsync Tests
        [Fact]
        public async Task GetReportByAsync_ShouldReturnReportAndEmail_WhenReportExists()
        {
            var report = await GetRandomEntityAsync(
                "Reports r JOIN Students s ON r.ReporterId = s.id",
                r => new { Id = r.GetInt32(r.GetOrdinal("Id")), ReporterId = r.GetInt32(r.GetOrdinal("ReporterId")), Email = r.GetString(r.GetOrdinal("email")) }
            );
            Assert.NotNull(report);

            var (email, resultReport) = await _service.GetReportByAsync(report.Id);

            Assert.Equal(report.Email, email);
            Assert.Equal(report.Id, resultReport.ReportId);
        }

        [Fact]
        public async Task GetReportByAsync_ShouldThrowKeyNotFoundException_WhenReportDoesNotExist()
        {
            var nonExistentId = await GetNextIdAsync("Reports");

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetReportByAsync(nonExistentId));
        }
        #endregion

        #region ViewAllReports Tests
        [Fact]
        public async Task ViewAllReports_ShouldReturnReports_WhenReportsExist()
        {
            var student = await GetRandomEntityAsync(
                "Students s JOIN Reports r ON s.id = r.ReporterId",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) }
            );
            Assert.NotNull(student);

            var reports = await _service.ViewAllReports(student.Id);

            Assert.NotNull(reports);
            Assert.NotEmpty(reports);
        }

        [Fact]
        public async Task ViewAllReports_ShouldReturnEmptyList_WhenNoReports()
        {
            var student = await GetRandomEntityAsync(
                "Students s LEFT JOIN Reports r ON s.id = r.ReporterId",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "r.Id IS NULL"
            );
            Assert.NotNull(student);

            var reports = await _service.ViewAllReports(student.Id);

            Assert.NotNull(reports);
            Assert.Empty(reports);
        }

        [Fact]
        public async Task ViewAllReports_ShouldHandleNullStudentId()
        {
            var reports = await _service.ViewAllReports(null);

            Assert.NotNull(reports);
            Assert.Empty(reports);
        }
        #endregion

        #region CancelReservation Tests
        [Fact]
        public async Task CancelReservation_ShouldReturnTrue_WhenReservationExists()
        {
            var student = await GetRandomEntityAsync(
                "Students s JOIN Reservations r ON s.id = r.StudentId",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "r.Status = 'RESERVED'"
            );
            Assert.NotNull(student);

            var result = await _service.CancelReservation(student.Id, "EMPTY");

            Assert.True(result);
        }

        [Fact]
        public async Task CancelReservation_ShouldReturnFalse_WhenNoReservationExists()
        {
            var student = await GetRandomEntityAsync(
                "Students s LEFT JOIN Reservations r ON s.id = r.StudentId",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "r.Id IS NULL"
            );
            Assert.NotNull(student);

            var result = await _service.CancelReservation(student.Id, "EMPTY");

            Assert.False(result);
        }
        #endregion

        #region GetDepartmentInfo Tests
        [Fact]
        public async Task GetDepartmentInfo_ShouldReturnInfo_WhenStudentExists()
        {
            var student = await GetRandomEntityAsync(
                "Students",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")), Department = r.GetString(r.GetOrdinal("department")), Location = r.GetString(r.GetOrdinal("Location")) }
            );
            Assert.NotNull(student);

            var info = await _service.GetDepartmentInfo(student.Id);

            Assert.NotNull(info);
            Assert.Equal(student.Department, info.DepartmentName);
            Assert.Equal(student.Location, info.Location);
        }

        [Fact]
        public async Task GetDepartmentInfo_ShouldReturnNull_WhenStudentDoesNotExist()
        {
            var nonExistentId = await GetNextIdAsync("Students");

            var info = await _service.GetDepartmentInfo(nonExistentId);

            Assert.Null(info);
        }
        #endregion

        #region GetAvailableWingsAndLevels Tests
        [Fact]
        public async Task GetAvailableWingsAndLevels_ShouldReturnWings_WhenDataExists()
        {
            var dept = await GetRandomEntityAsync(
                "Departments d JOIN Cabinets c ON d.name = c.department_name AND d.Location = c.location",
                r => new { Name = r.GetString(r.GetOrdinal("department_name")), Location = r.GetString(r.GetOrdinal("location")) }
            );
            Assert.NotNull(dept);

            var wings = await _service.GetAvailableWingsAndLevels(dept.Name, dept.Location);

            Assert.NotNull(wings);
            Assert.NotEmpty(wings);
        }

        [Fact]
        public async Task GetAvailableWingsAndLevels_ShouldReturnEmptyList_WhenNoData()
        {
            var dept = await GetRandomEntityAsync(
                "Departments d LEFT JOIN Cabinets c ON d.name = c.department_name AND d.Location = c.location",
                r => new { Name = r.GetString(r.GetOrdinal("name")), Location = r.GetString(r.GetOrdinal("Location")) },
                "c.cabinet_id IS NULL"
            );
            Assert.NotNull(dept);

            var wings = await _service.GetAvailableWingsAndLevels(dept.Name, dept.Location);

            Assert.NotNull(wings);
            Assert.Empty(wings);
        }
        #endregion

        #region GetCurrentReservationAsync Tests
        [Fact]
        public async Task GetCurrentReservationAsync_ShouldReturnReservation_WhenExists()
        {
            var student = await GetRandomEntityAsync(
                "Students s JOIN Reservations r ON s.id = r.StudentId",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "r.Status = 'RESERVED'"
            );
            Assert.NotNull(student);

            var reservation = await _service.GetCurrentReservationAsync(student.Id);

            Assert.NotNull(reservation);
            Assert.Equal(student.Id, reservation.StudentId);
        }

        [Fact]
        public async Task GetCurrentReservationAsync_ShouldReturnNull_WhenNoReservation()
        {
            var student = await GetRandomEntityAsync(
                "Students s LEFT JOIN Reservations r ON s.id = r.StudentId",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "r.Id IS NULL"
            );
            Assert.NotNull(student);

            var reservation = await _service.GetCurrentReservationAsync(student.Id);

            Assert.Null(reservation);
        }
        #endregion

        #region ReserveLockerInWingAndLevel Tests
        [Fact]
        public async Task ReserveLockerInWingAndLevel_ShouldReturnLockerId_WhenSuccessful()
        {
            var student = await GetRandomEntityAsync(
                "Students s JOIN Cabinets c ON s.department = c.department_name AND s.Location = c.location LEFT JOIN BlockList b ON s.id = b.student_id",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")), Department = r.GetString(r.GetOrdinal("department_name")), Location = r.GetString(r.GetOrdinal("location")), Wing = r.GetString(r.GetOrdinal("wing")), Level = r.GetInt32(r.GetOrdinal("level")) },
                "b.student_id IS NULL AND s.locker_id IS NULL"
            );
            Assert.NotNull(student);

            var lockerId = await _service.ReserveLockerInWingAndLevel(student.Id, student.Department, student.Location, student.Wing, student.Level);

            Assert.NotNull(lockerId);
        }

        [Fact]
        public async Task ReserveLockerInWingAndLevel_ShouldReturnNull_WhenStudentBlocked()
        {
            var student = await GetRandomEntityAsync(
                "Students s JOIN BlockList b ON s.id = b.student_id JOIN Cabinets c ON s.department = c.department_name AND s.Location = c.location",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")), Department = r.GetString(r.GetOrdinal("department_name")), Location = r.GetString(r.GetOrdinal("location")), Wing = r.GetString(r.GetOrdinal("wing")), Level = r.GetInt32(r.GetOrdinal("level")) }
            );
            Assert.NotNull(student);

            var lockerId = await _service.ReserveLockerInWingAndLevel(student.Id, student.Department, student.Location, student.Wing, student.Level);

            Assert.Null(lockerId);
        }

        [Fact]
        public async Task ReserveLockerInWingAndLevel_ShouldReturnNull_WhenNoLockersAvailable()
        {
            var student = await GetRandomEntityAsync(
                "Students",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) }
            );
            Assert.NotNull(student);

            var lockerId = await _service.ReserveLockerInWingAndLevel(student.Id, "NonExistentDept", "Engineering", "Z", 99);

            Assert.Null(lockerId);
        }
        #endregion

        #region HasLocker Tests
        [Fact]
        public void HasLocker_ShouldReturnTrue_WhenStudentHasLocker()
        {
            var student = GetRandomEntityAsync(
                "Students s JOIN Reservations r ON s.id = r.StudentId",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "r.Status = 'RESERVED'"
            ).GetAwaiter().GetResult();
            Assert.NotNull(student);

            var result = _service.HasLocker(student.Id);

            Assert.True(result);
        }

        [Fact]
        public void HasLocker_ShouldReturnFalse_WhenNoLocker()
        {
            var student = GetRandomEntityAsync(
                "Students s LEFT JOIN Reservations r ON s.id = r.StudentId",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "r.Id IS NULL"
            ).GetAwaiter().GetResult();
            Assert.NotNull(student);

            var result = _service.HasLocker(student.Id);

            Assert.False(result);
        }
        #endregion

        #region IsStudentBlocked Tests
        
        [Fact]
        public async Task IsStudentBlocked_ShouldReturnTrue_WhenBlocked()
        {
            var student = await GetRandomEntityAsync(
                "BlockList",
                r => new { Id = r.GetInt32(r.GetOrdinal("student_id")) }
            );
            Assert.NotNull(student);

            var result = await _service.IsStudentBlocked(student.Id);

            Assert.True(result);
        }

        [Fact]
        public async Task IsStudentBlocked_ShouldReturnFalse_WhenNotBlocked()
        {
            var student = await GetRandomEntityAsync(
                "Students s LEFT JOIN BlockList b ON s.id = b.student_id",
                r => new { Id = r.GetInt32(r.GetOrdinal("id")) },
                "b.student_id IS NULL"
            );
            Assert.NotNull(student);

            var result = await _service.IsStudentBlocked(student.Id);

            Assert.False(result);
        }
        #endregion

        #region GetAllAvailableLockerCounts Tests
        [Fact]
        public async Task GetAllAvailableLockerCounts_ShouldReturnCounts_WhenDataExists()
        {
            var counts = await _service.GetAllAvailableLockerCounts();

            Assert.NotNull(counts);
            Assert.NotEmpty(counts);
        }

        [Fact]
        public async Task GetAllAvailableLockerCounts_ShouldReturnFilteredCounts_WhenFiltersApplied()
        {
            var cabinet = await GetRandomEntityAsync(
                "Cabinets",
                r => new { Location = r.GetString(r.GetOrdinal("location")), Department = r.GetString(r.GetOrdinal("department_name")), Wing = r.GetString(r.GetOrdinal("wing")), Level = r.GetInt32(r.GetOrdinal("level")) }
            );
            Assert.NotNull(cabinet);

            var counts = await _service.GetAllAvailableLockerCounts(cabinet.Location, cabinet.Department, cabinet.Wing, cabinet.Level);

            Assert.NotNull(counts);
            Assert.All(counts, x =>
            {
                Assert.Equal(cabinet.Location, x.Location);
                Assert.Equal(cabinet.Department, x.Department);
                Assert.Equal(cabinet.Wing, x.Wing);
                Assert.Equal(cabinet.Level, x.Level);
            });
        }
        #endregion

        #region GetFilterOptions Tests
        [Fact]
        public async Task GetFilterOptions_ShouldReturnOptions_WhenDataExists()
        {
            var options = await _service.GetFilterOptions();

            Assert.NotNull(options);
            Assert.NotEmpty(options.Locations);
            Assert.NotEmpty(options.DepartmentsByLocation);
            Assert.NotEmpty(options.WingsByDeptLocation);
            Assert.NotEmpty(options.LevelsByWingDeptLocation);
        }
        #endregion
    }
}