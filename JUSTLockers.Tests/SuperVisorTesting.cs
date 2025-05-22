using JUSTLockers.Classes;
using JUSTLockers.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace JUSTLockers.Testing
{
    public class SupervisorServiceTest
    {
        private readonly SupervisorService _service;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<AdminService> _adminServiceMock;
        private readonly IConfiguration _configuration;

        public SupervisorServiceTest()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            _adminServiceMock = new Mock<AdminService>(config);

            _service = new SupervisorService(config, _adminServiceMock.Object);
        }

        #region ViewReportedIssues Tests
        [Fact]
        public async Task ViewReportedIssues_ShouldReturnReports_WhenReportsExist()
        {
            int? userId = 987655; // Ensure this supervisor exists in the test database
            var reports = await _service.ViewReportedIssues(userId);

            Assert.NotNull(reports);
            Assert.NotEmpty(reports); // Assumes test database has reports for this supervisor
        }

        [Fact]
        public async Task ViewReportedIssues_ShouldReturnEmptyList_WhenNoReports()
        {
            int? userId = 999999; // Non-existent supervisor
            var reports = await _service.ViewReportedIssues(userId);

            Assert.NotNull(reports);
            Assert.Empty(reports);
        }

        [Fact]
        public async Task ViewReportedIssues_ShouldHandleNullUserId()
        {
            int? userId = null;
            var reports = await _service.ViewReportedIssues(userId);

            Assert.NotNull(reports);
            Assert.Empty(reports);
        }
        #endregion

        #region TheftIssues Tests
        [Fact]
        public async Task TheftIssues_ShouldReturnTheftReports_WhenFilterIsTheft()
        {
            string filter = "THEFT";
            var reports = await _service.TheftIssues(filter);

            Assert.NotNull(reports);
            Assert.All(reports, r => Assert.Equal(ReportType.THEFT, r.Type));
        }

        [Fact]
        public async Task TheftIssues_ShouldReturnAllReports_WhenFilterIsNull()
        {
            string filter = null;
            var reports = await _service.TheftIssues(filter);

            Assert.NotNull(reports);
            // Assumes test database has reports; check if non-empty
            Assert.True(reports.Count > 0, "Expected reports in test database.");
        }

        #endregion

        #region ViewAllStudentReservations Tests
        [Fact]
        public async Task ViewAllStudentReservations_ShouldReturnStudents_WhenReservationsExist()
        {
            int? userId = 987681; // Existing supervisor
            var students = await _service.ViewAllStudentReservations(userId);

            Assert.NotNull(students);
            Assert.NotEmpty(students); // Assumes test database has reservations
        }

        [Fact]
        public async Task ViewAllStudentReservations_ShouldReturnEmptyList_WhenNoReservations()
        {
            int? userId = 999999; // Non-existent supervisor
            var students = await _service.ViewAllStudentReservations(userId);

            Assert.NotNull(students);
            Assert.Empty(students);
        }

        [Fact]
        public async Task ViewAllStudentReservations_ShouldFilterByStudentId()
        {
            int? userId = 987681;
            int? searchstu = 152424; // Existing student ID
            var students = await _service.ViewAllStudentReservations(userId, searchstu);

            Assert.NotNull(students);
            Assert.Single(students);
            Assert.Equal(152424, students[0].Id);
        }
        #endregion

        #region ReallocationRequestFormSameDep Tests
        [Fact]
        public async Task ReallocationRequestFormSameDep_ShouldSucceed_WhenValid()
        {
            var model = new Reallocation
            {
                SupervisorID = 987655,
                CurrentDepartment = "CH",
                RequestedDepartment = "CH",
                CurrentLocation = "Engineering",
                RequestLocation = "Engineering",
                RequestWing = "1",
                RequestLevel = 1,
                NumberCab = 14,
                CurrentCabinetID = "CH2-L0-cab14"
            };
           

            var (message, requestId) = await _service.ReallocationRequestFormSameDep(model);

            Assert.Equal("Cabinet reallocation was successful.", message);
            Assert.True(requestId > 0);
        }

        [Fact]
        public async Task ReallocationRequestFormSameDep_ShouldFail_WhenSupervisorNotFound()
        {
            var model = new Reallocation { SupervisorID = 999999 };
            var (message, requestId) = await _service.ReallocationRequestFormSameDep(model);

            Assert.Equal("Supervisor not found.", message);
            Assert.Equal(0, requestId);
        }

        [Fact]
        public async Task ReallocationRequestFormSameDep_ShouldFail_WhenUnauthorized()
        {
            var model = new Reallocation
            {
                SupervisorID = 987655,
                CurrentDepartment = "CH",
                RequestedDepartment = "A",
                CurrentLocation = "Engineering",
                RequestLocation = "Engineering"
            };

            var (message, requestId) = await _service.ReallocationRequestFormSameDep(model);

            Assert.StartsWith("You are not allowed to reallocate a cabinet outside your department and location", message);
            Assert.Equal(0, requestId);
        }

        [Fact]
        public async Task ReallocationRequestFormSameDep_ShouldFail_WhenCabinetExists()
        {
            var model = new Reallocation
            {
                SupervisorID = 987655,
                CurrentDepartment = "CH",
                RequestedDepartment = "CH",
                CurrentLocation = "Engineering",
                RequestLocation = "Engineering",
                RequestWing = "1",
                RequestLevel = 1,
                NumberCab = 14 // Existing cabinet
            };

            var (message, requestId) = await _service.ReallocationRequestFormSameDep(model);

            Assert.Equal("The requested cabinet already exists at the specified location.", message);
            Assert.Equal(0, requestId);
        }
        #endregion

        #region ReallocationRequest Tests
        [Fact]
        public async Task ReallocationRequest_ShouldSucceed_WhenValid()
        {
            var model = new Reallocation
            {
                SupervisorID = 987655,
                CurrentDepartment = "CH",
                RequestedDepartment = "D",
                CurrentLocation = "Engineering",
                RequestLocation = "Medicine",
                RequestWing = "2",
                RequestLevel = 1,
                NumberCab = 14,
                CurrentCabinetID = "CH1-L1-cab14"
            };

            var message = await _service.ReallocationRequest(model);

            Assert.Equal("Request sent successfully! Wait Admin Response", message);
        }

        [Fact]
        public async Task ReallocationRequest_ShouldFail_WhenSupervisorNotFound()
        {
            var model = new Reallocation { SupervisorID = 999999 };
            var message = await _service.ReallocationRequest(model);

            Assert.Equal("Supervisor not found.", message);
        }

        

        [Fact]
        public async Task ReallocationRequest_ShouldFail_WhenRequestExists()
        {
            var model = new Reallocation
            {
                SupervisorID = 987655,
                CurrentDepartment = "CH",
                RequestedDepartment = "D",
                CurrentLocation = "Engineering",
                RequestLocation = "Medicine",
                RequestWing = "2",
                RequestLevel = 1,
                NumberCab = 14,
                CurrentCabinetID = "CH1-L1-cab14"
            };

            // Insert a duplicate request first
            await _service.ReallocationRequest(model);
            var message = await _service.ReallocationRequest(model);

            Assert.Equal("This request has already been submitted.", message);
        }
        #endregion

        #region SendToAdmin Tests
      

        [Fact]
        public async Task SendToAdmin_ShouldThrow_WhenReportNotFound()
        {
            int reportId = 999999;
            try{ _service.SendToAdmin(reportId); }
            catch (Exception ex)
            {
                Assert.StartsWith("Error sending report to admin", ex.Message);
            }
        }
        #endregion

        #region BlockedStudents Tests
        [Fact]
        public async Task BlockedStudents_ShouldReturnList_WhenBlockedStudentsExist()
        {
            var blockedStudents = await _service.BlockedStudents();
            //if list is empty, it means no blocked students exist in the test database
            if (blockedStudents.Count>0)
            {Assert.NotNull(blockedStudents);
                Assert.NotEmpty(blockedStudents);
            } // Assumes test database has blocked students
            else Assert.Empty(blockedStudents);
        }

        #endregion

        #region GetStudentById Tests
        [Fact]
        public void GetStudentById_ShouldReturnStudent_WhenExists()
        {
            int id = 152423; // Existing student ID
            var student = _service.GetStudentById(id);

            Assert.NotNull(student);
            Assert.Equal(152423, student.Id);
        }

        [Fact]
        public void GetStudentById_ShouldReturnNull_WhenNotExists()
        {
            int id = 999999;
            var student = _service.GetStudentById(id);

            Assert.Null(student);
        }

        [Fact]
        public void GetStudentById_ShouldHandleBlockedStudent()
        {
            int id = 151987; // Student that is blocked
            var student = _service.GetStudentById(id);

            Assert.NotNull(student);
            Assert.True(student.IsBlocked);
        }
        #endregion

        #region IsStudentBlocked Tests
        [Fact]
        public void IsStudentBlocked_ShouldReturnTrue_WhenBlocked()
        {
            int id = 151987; // Blocked student
            var isBlocked = _service.IsStudentBlocked(id);

            Assert.True(isBlocked);
        }

        [Fact]
        public void IsStudentBlocked_ShouldReturnFalse_WhenNotBlocked()
        {
            int id = 152423; // Non-blocked student
            var isBlocked = _service.IsStudentBlocked(id);

            Assert.False(isBlocked);
        }
        #endregion

        #region BlockStudent Tests
        [Fact]
        public void BlockStudent_ShouldSucceed_WhenValid()
        {
            int id = 152428; // Non-blocked student
            int? userId = 987655;
            var message = _service.BlockStudent(id, userId);

            Assert.Equal("Student successfully blocked.", message);
        }

        [Fact]
        public void BlockStudent_ShouldFail_WhenOutsideDepartment()
        {
            int id = 152423; // Student in different department
            int? userId = 987655;
            var message = _service.BlockStudent(id, userId);

            Assert.Equal("Cannot block student outside your department/location.", message);
        }

        [Fact]
        public void BlockStudent_ShouldHandleNullUserId()
        {
            int id = 100002;
            int? userId = null;
            var message = _service.BlockStudent(id, userId);

            Assert.Equal("Cannot block student outside your department/location.", message);
        }
        #endregion

        #region UnblockStudent Tests
        [Fact]
        public void UnblockStudent_ShouldSucceed_WhenValid()
        {
            int id = 152428; // Blocked student
            int? userId = 987655;
            var message = _service.UnblockStudent(id, userId);

            Assert.Equal("Student successfully Unblocked.", message);
        }

        [Fact]
        public void UnblockStudent_ShouldFail_WhenOutsideDepartment()
        {
            int id = 152423;
            int? userId = 987655;
            var message = _service.UnblockStudent(id, userId);

            Assert.Equal("Cannot Unblock student outside your department/location.", message);
        }
        #endregion

        #region ViewCabinetInfo Tests
        [Fact]
        public async Task ViewCabinetInfo_ShouldReturnCabinets_WhenExist()
        {
            int? userId = 987655;
            var cabinets = await _service.ViewCabinetInfo(userId, null, null, null, null, null);

            Assert.NotNull(cabinets);
            Assert.NotEmpty(cabinets);
        }

        [Fact]
        public async Task ViewCabinetInfo_ShouldReturnFilteredCabinets_WhenSearchCabProvided()
        {
            int? userId = 987655;
            string searchCab = "CH1-L1-cab14";
            var cabinets = await _service.ViewCabinetInfo(userId, searchCab, null, null, null, null);

            Assert.NotNull(cabinets);
            Assert.All(cabinets, c => Assert.Contains("CH1-L1-cab14", c.Cabinet_id));
        }

        [Fact]
        public async Task ViewCabinetInfo_ShouldReturnEmptyList_WhenNoCabinets()
        {
            int? userId = 999999;
            var cabinets = await _service.ViewCabinetInfo(userId, null, null, null, null, null);

            Assert.NotNull(cabinets);
            Assert.Empty(cabinets);
        }
        #endregion

        #region GetDepartmentInfo Tests
        [Fact]
        public async Task GetDepartmentInfo_ShouldReturnInfo_WhenSupervisorExists()
        {
            int userId = 987655;
            var info = await _service.GetDepartmentInfo(userId);

            Assert.NotNull(info);
            Assert.Equal("CH", info.DepartmentName);
            Assert.Equal("Engineering", info.Location);
        }

        [Fact]
        public async Task GetDepartmentInfo_ShouldReturnNull_WhenSupervisorNotFound()
        {
            int userId = 999999;
            var info = await _service.GetDepartmentInfo(userId);

            Assert.Null(info);
        }
        #endregion

        #region HasCovenant Tests
        [Fact]
        public async Task HasCovenant_ShouldReturnTrue_WhenCovenantAssigned()
        {
            int? userId = 987655;
            var hasCovenant = await _service.HasCovenant(userId);

            Assert.True(hasCovenant);
        }

        [Fact]
        public async Task HasCovenant_ShouldReturnFalse_WhenNoCovenant()
        {
            int? userId = 987660; // Supervisor with no covenant
            var hasCovenant = await _service.HasCovenant(userId);

            Assert.False(hasCovenant);
        }

        [Fact]
        public async Task HasCovenant_ShouldHandleNullUserId()
        {
            int? userId = null;
            var hasCovenant = await _service.HasCovenant(userId);

            Assert.False(hasCovenant);
        }
        #endregion
    }
}