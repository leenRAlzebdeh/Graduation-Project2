using JUSTLockers.Classes;
using JUSTLockers.Controllers;
using JUSTLockers.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Moq;


namespace JUSTLockers.Testing
{
    public class AdminControllerTest
    {
        private readonly AdminService _service;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly AdminController _controller;
        private readonly IConfiguration _configuration;
        private readonly Mock<IStudentService> _studentService;
        private readonly Mock<NotificationService> _notificationServiceMock;
        private readonly Mock<AdminService> _adminServiceMock;
        public AdminControllerTest()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            _service = new AdminService(config);

            // Mock only what you need
            _emailServiceMock = new Mock<IEmailService>();
            _notificationServiceMock = new Mock<NotificationService>(_emailServiceMock.Object, _service);
            _studentService = new Mock<IStudentService>();

            _controller = new AdminController(
                _service,
                config,
                _emailServiceMock.Object,
                _notificationServiceMock.Object,
                _studentService.Object);

            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        }

        #region CheckEmployeeExists Tests
        [Fact]
        public async Task CheckEmployeeExists_ShouldReturnTrue_ForExistingEmployee()
        { 
            var result = await _service.CheckEmployeeExists(987681);
            Assert.True(result);
        }

        [Fact]
        public async Task CheckEmployeeExists_ShouldReturnFalse_ForNonExistingEmployee()
        {
            var result = await _service.CheckEmployeeExists(999999); 
            Assert.False(result);
        }

        [Fact]
        public async Task CheckEmployeeExists_ShouldHandleNegativeId()
        {
            var result = await _service.CheckEmployeeExists(-1);
            Assert.False(result);
        }
        #endregion

        #region AddSupervisor Tests
        [Fact]
        public async Task AddSupervisor_ShouldSucceed_WhenValidEmployee()
        {

            var supervisor = new Supervisor
            {
                Id = 987681, 
                DepartmentName = "A",
                Location = "Engineering"
            };
            var result = await _service.AddSupervisor(supervisor);

            Assert.True(result.Success);
            Assert.Equal("Supervisor added successfully!", result.Message);
        }

        [Fact]
        public async Task AddSupervisor_ShouldFail_WhenEmployeeNotFound()
        {
            var supervisor = new Supervisor
            {
                Id = 999999,
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
            var supervisor = new Supervisor
            {
                Id = 987655, // Existing supervisor ID
                DepartmentName = "A",
                Location = "Engineering"
            };
            var result = await _service.AddSupervisor(supervisor);

            Assert.False(result.Success);
            Assert.Equal("Supervisor ID already exists.", result.Message);
        }

        [Fact]
        public async Task AddSupervisor_ShouldHandleNullDepartment()
        {
            var supervisor = new Supervisor
            {
                Id = 987660,
                DepartmentName = null,
                Location = null
            };
            var result = await _service.AddSupervisor(supervisor);
            Console.WriteLine(result.Message);
            Assert.True(result.Success);
            Assert.Equal("Supervisor added successfully!", result.Message);
        }
        #endregion

        #region SupervisorExists Tests
        [Fact]
        public async Task SupervisorExists_ShouldReturnTrue_IfExists()
        {
            var result = await _service.SupervisorExists(987655); // Existing supervisor ID
            Assert.True(result);
        }

        [Fact]
        public async Task SupervisorExists_ShouldReturnFalse_IfNotExists()
        {
            var result = await _service.SupervisorExists(999999);
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
            var cabinet = new Cabinet
            {
                CabinetNumber = 22,
                Wing = "2",
                Level = 1,
                Location = "Engineering",
                Department = "CH",
                Capacity = 6,
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
        }

        [Fact]
        public async Task AssignCabinet_ShouldFail_WhenInvalidDepartment()
        {
            var cabinet = new Cabinet
            {
                CabinetNumber = 21,
                Wing = "1",
                Level = 1,
                Location = "Engineering",
                Department = "NonExist",
                Capacity = 10
            };

            var result = _service.AssignCabinet(cabinet);
            Assert.Contains("Error adding cabinet", result);
        }
        #endregion

        #region AssignCovenant Tests
        [Fact]
        public async Task AssignCovenant_ShouldSucceed_WhenValid()
        {
            var result = await _service.AssignCovenant(987678, "D", "Medicine");
            Assert.Contains("Covenant assigned successfully", result);
        }

        [Fact]
        public async Task AssignCovenant_ShouldFail_WhenSupervisorNotFound()
        {
            var result = await _service.AssignCovenant(999999, "A", "Engineering");
            Assert.Equal("Supervisor does not exist.", result);
        }

        [Fact]
        public async Task AssignCovenant_ShouldFail_WhenInvalidDepartment()
        {
            var result = await _service.AssignCovenant(987660, "NonExist", "Engineering");
            Assert.Contains("Error", result);
        }
        #endregion

        #region DeleteSupervisor Tests
        [Fact]
        public async Task DeleteSupervisor_ShouldSucceed_WhenValid()
        {
            // First add a test supervisor
            var supervisor = new Supervisor { Id = 987683, DepartmentName = null, Location = null };
            await _service.AddSupervisor(supervisor);

            var result = await _service.DeleteSupervisor(987683);
            Assert.Equal("Supervisor deleted successfully.", result);
        }

        [Fact]
        public async Task DeleteSupervisor_ShouldFail_WhenNotFound()
        {
            var result = await _service.DeleteSupervisor(999999);
            Assert.Equal("Supervisor not found.", result);
        }
        #endregion

        #region DeleteCovenant Tests
        [Fact]
        public async Task DeleteCovenant_ShouldSucceed_WhenAssigned()
        {
            var result = await _service.DeleteCovenant(987660);
            Assert.Equal("Covenant deleted successfully.", result);
        }

        [Fact]
        public async Task DeleteCovenant_ShouldFail_WhenNoCovenant()
        {
            // Use a supervisor with no covenant
            var supervisor = new Supervisor { Id = 987660, DepartmentName = null, Location = null };
      
            var result = await _service.DeleteCovenant(987660);
            Assert.Equal("Supervisor doesn't have a covenant assigned.", result);
        }

        [Fact]
        public async Task DeleteCovenant_ShouldFail_WhenSupervisorNotFound()
        {
            var result = await _service.DeleteCovenant(999999);
            Assert.Equal("Supervisor not found.", result);
        }
        #endregion

        #region GetSupervisorById Tests
        [Fact]
        public async Task GetSupervisorById_ShouldReturnSupervisor_WhenExists()
        {
            var supervisor = await _service.GetSupervisorById(987655);
            Assert.NotNull(supervisor);
            Assert.Equal(987655, supervisor.Id);
        }

        [Fact]
        public async Task GetSupervisorById_ShouldReturnNull_WhenNotExists()
        {
            var supervisor = await _service.GetSupervisorById(999999);
            Assert.Null(supervisor);
        }
        #endregion

        #region IsDepartmentAssigned Tests
        [Fact]
        public async Task IsDepartmentAssigned_ShouldReturnId_IfAssigned()
        {
            var id = await _service.IsDepartmentAssigned("CH", "Engineering");
            Assert.Equal(987655, id);
        }

        [Fact]
        public async Task IsDepartmentAssigned_ShouldReturnZero_IfNotAssigned()
        {
            var id = await _service.IsDepartmentAssigned("NonExist", "Engineering");
            Assert.Equal(0, id);
        }
        #endregion

        #region ViewAllSupervisorInfo Tests
        [Fact]
        public async Task ViewAllSupervisorInfo_ShouldReturnNonEmptyList()
        {
            var result = await _service.ViewAllSupervisorInfo();
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task ViewAllSupervisorInfo_ShouldIncludeDepartmentInfo_WhenAssigned()
        {
            var result = await _service.ViewAllSupervisorInfo();
            var supervisorWithDept = result.Find(s => s.SupervisedDepartment != null);
            Assert.NotNull(supervisorWithDept);
            Assert.NotNull(supervisorWithDept.SupervisedDepartment);
        }
        #endregion

        #region GetDepartments Tests
        [Fact]
        public async Task GetDepartments_ShouldReturnAllDepartments()
        {
            var result = await _service.GetDepartments();
            Assert.NotNull(result);
            Assert.True(result.Count >= 12);
        }

        [Fact]
        public async Task GetDepartmentsByLocation_ShouldReturnMatchingDepartments()
        {
            var result = await _service.GetDepartmentsByLocation("Engineering");
            Assert.NotNull(result);
            Assert.Contains(result, d => d.Name == "A" && d.Location == "Engineering");
        }

        [Fact]
        public async Task GetDepartmentsByLocation_ShouldReturnEmpty_WhenInvalidLocation()
        {
            var result = await _service.GetDepartmentsByLocation("Invalid");
            Assert.Empty(result);
        }
        #endregion

        #region Report Management Tests
        [Fact]
        public async Task ResolveReport_ShouldSucceed_WhenValid()
        {
            var result = await _service.ResolveReport(1, "Test resolution");
            Assert.True(result);
        }

        [Fact]
        public async Task ResolveReport_ShouldFail_WhenInvalidId()
        {
            var result = await _service.ResolveReport(999999, "Test resolution");
            Assert.False(result);
        }

        [Fact]
        public async Task ReviewReport_ShouldSucceed_WhenValid()
        {
            var result = await _service.ReviewReport(1);
            Assert.True(result);
        }

        [Fact]
        public async Task ReviewReport_ShouldFail_WhenInvalidId()
        {
            var result = await _service.ReviewReport(999999);
            Assert.False(result);
        }

        [Fact]
        public async Task RejectReport_ShouldSucceed_WhenValid()
        {
            var result = await _service.RejectReport(1);
            Assert.True(result);
        }

        [Fact]
        public async Task RejectReport_ShouldFail_WhenInvalidId()
        {
            var result = await _service.RejectReport(999999);
            Assert.False(result);
        }

        [Fact]
        public async Task GetReportDetails_ShouldReturnReport_WhenExists()
        {
            var report = await _service.GetReportDetails(1);
            Assert.NotNull(report);
            Assert.Equal(1, report.ReportId);
        }

        [Fact]
        public async Task GetReportDetails_ShouldReturnNull_WhenNotExists()
        {
            var report = await _service.GetReportDetails(999999);
            Assert.Null(report);
        }
        #endregion

        #region Reallocation Tests
        [Fact]
        public async Task ApproveRequestReallocation_ShouldSucceed_WhenValid()
        {
            var result = await _service.ApproveRequestReallocation(1);
            Assert.True(result);
        }

        [Fact]
        public async Task ApproveRequestReallocation_ShouldFail_WhenInvalidId()
        {
            var result = await _service.ApproveRequestReallocation(999999);
            Assert.False(result);
        }

        [Fact]
        public async Task RejectRequestReallocation_ShouldSucceed_WhenValid()
        {
            var result = await _service.RejectRequestReallocation(1);
            Assert.True(result);
        }

        [Fact]
        public async Task RejectRequestReallocation_ShouldFail_WhenInvalidId()
        {
            var result = await _service.RejectRequestReallocation(999999);
            Assert.False(result);
        }
        #endregion

        #region Controller Action Tests
        [Fact]
        public async Task AddSupervisorAction_ShouldReturnFailJson_WhenValid()
        {
            var supervisor = new Supervisor
            {
                Id = 987683,
                DepartmentName = "A",
                Location = "Engineering"
            }; 
            var result = await _service.AddSupervisor(supervisor);
            Assert.False(result.Success);
            Assert.Equal("Department is already assigned to another supervisor.", result.Message);
        }


        [Fact]
        public async Task ViewCabinetInfo_ShouldReturnViewWithModel()
        {
            var result = await _controller.ViewCabinetInfo(null, null, null, null, null, null) as ViewResult;
            Assert.NotNull(result);
            Assert.IsType<List<Cabinet>>(result.Model);
        }

        [Fact]
        public async Task SignCovenant_ShouldReturnViewWithSupervisors()
        {
            var result = await _controller.SignCovenant() as ViewResult;
            Assert.NotNull(result);
            Assert.IsType<List<Supervisor>>(result.Model);
            Assert.NotNull(_controller.ViewBag.Departments);
        }
        #endregion
    }
}