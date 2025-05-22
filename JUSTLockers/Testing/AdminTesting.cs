//using JUSTLockers.Controllers;
//using JUSTLockers.Services;
//using JUSTLockers.Classes;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace JUSTLockers.Testing
//{
//    [TestClass]
//    public class AdminControllerTest
//    {
//        private Mock<AdminService>? _mockAdminService;
//        private Mock<IEmailService>? _mockEmailService;
//        private Mock<NotificationService>? _mockNotificationService;
//        private Mock<IStudentService>? _mockStudentService;
//        private Mock<IConfiguration>? _mockConfiguration;
//        private AdminController? _controller;

//        [TestInitialize]
//        public void Setup()
//        {
//            _mockConfiguration = new Mock<IConfiguration>();
//            _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
//                .Returns("Server=localhost;Database=test_db;Uid=root;Pwd=password;");

//            _mockAdminService = new Mock<AdminService>(_mockConfiguration.Object);
//            _mockEmailService = new Mock<IEmailService>();
//            _mockNotificationService = new Mock<NotificationService>(_mockEmailService.Object, _mockAdminService.Object);
//            _mockStudentService = new Mock<IStudentService>();

//            _controller = new AdminController(
//                _mockAdminService.Object,
//                _mockConfiguration.Object,
//                _mockEmailService.Object,
//                _mockNotificationService.Object,
//                _mockStudentService.Object);

//            // Setup TempData for controller
//            var tempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
//                new DefaultHttpContext(),
//                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
//            _controller.TempData = tempData;
//        }

//        [TestMethod]
//        public async Task SemesterSettings_Returns_View_With_Model()
//        {
//            // Arrange
//            var settings = new { Id = 1, SemesterEndDate = new DateTime(2025, 6, 10), IsManualEnd = false };
//            _mockAdminService!.Setup(a => a.GetSemesterSettings()).ReturnsAsync(settings);

//            // Act
//            var result = await _controller!.SemesterSettings() as ViewResult;

//            // Assert
//            Assert.IsNotNull(result);
//            Assert.AreEqual("~/Views/Admin/SemesterSettings.cshtml", result.ViewName);
//            Assert.AreEqual(settings, result.Model);
//        }

//        [TestMethod]
//        public async Task ScheduleSemesterEnd_Fails_If_Date_Less_Than_1_Day()
//        {
//            // Arrange
//            var now = DateTime.Now;
//            var endDate = now.AddHours(12); // Less than 1 day from now

//            // Act
//            var result = await _controller!.ScheduleSemesterEnd(endDate, null) as RedirectToActionResult;

//            // Assert
//            Assert.IsNotNull(result);
//            Assert.AreEqual("SemesterSettings", result.ActionName);
//            Assert.AreEqual("End date must be at least 1 days in the future.", _controller.TempData["ErrorMessage"]);
//        }

//        [TestMethod]
//        public async Task ScheduleSemesterEnd_Succeeds_If_Date_Valid()
//        {
//            // Arrange
//            var now = DateTime.Now;
//            var endDate = now.AddDays(2); // Valid date (more than 1 day)
//            _mockAdminService!.Setup(a => a.SaveSemesterSettings(endDate, null)).ReturnsAsync(true);
//            _mockAdminService.Setup(a => a.GetAllSupervisorsEmails()).ReturnsAsync(new List<string> { "supervisor@example.com" });

//            // Act
//            var result = await _controller!.ScheduleSemesterEnd(endDate, null) as JsonResult;

//            // Assert
//            Assert.IsNotNull(result);
//            dynamic jsonValue = result.Value!;
//            Assert.IsTrue(jsonValue.success);
//            Assert.AreEqual("Semester end date scheduled successfully.", jsonValue.message);
//            _mockEmailService!.Verify(e => e.SemesterEndNotificationAsync(
//                It.Is<List<string>>(l => l.Contains("supervisor@example.com")),
//                "SupervisorsSemesterEndNotification",
//                It.Is<Dictionary<string, DateTime>>(d => d["EndDate"] == endDate)),
//                Times.Once());
//        }

//        [TestMethod]
//        public async Task ManualSemesterEnd_Succeeds_And_Sends_Notification()
//        {
//            // Arrange
//            var now = DateTime.Now;
//            var endDate = now.AddDays(5); // 5 days from now
//            _mockAdminService!.Setup(a => a.SaveSemesterSettings(It.IsAny<DateTime>(), null)).ReturnsAsync(true);
//            _mockAdminService.Setup(a => a.GetAllSupervisorsEmails()).ReturnsAsync(new List<string> { "supervisor@example.com" });

//            // Act
//            var result = await _controller!.ManualSemesterEnd() as RedirectToActionResult;

//            // Assert
//            Assert.IsNotNull(result);
//            Assert.AreEqual("SemesterSettings", result.ActionName);
//            Assert.AreEqual("Manual semester end triggered. Notifications will be sent, and reservations will be canceled in 5 days.", _controller.TempData["SuccessMessage"]);
//            _mockEmailService!.Verify(e => e.SemesterEndNotificationAsync(
//                It.Is<List<string>>(l => l.Contains("supervisor@example.com")),
//                "SupervisorsSemesterEndNotification",
//                It.IsAny<Dictionary<string, DateTime>>()),
//                Times.Once());
//        }

//        [TestMethod]
//        public async Task AddSupervisor_Succeeds_With_Valid_Data()
//        {
//            // Arrange
//            var supervisor = new JUSTLockers.Classes.Supervisor(1, "Test", "test@test.com", new Department());
//            supervisor.DepartmentName = "CS";
//            supervisor.Location = "Wing 3";
//            _mockAdminService!.Setup(a => a.CheckEmployeeExists(1)).ReturnsAsync(true);
//            _mockAdminService.Setup(a => a.GetSupervisorById(1)).ReturnsAsync((JUSTLockers.Classes.Supervisor?)null);
//            _mockAdminService.Setup(a => a.IsDepartmentAssigned("CS", "Wing 3")).ReturnsAsync(0);
//            _mockAdminService.Setup(a => a.AddSupervisor(supervisor)).ReturnsAsync((true, "Supervisor added successfully."));

//            // Act
//            var result = await _controller!.AddSupervisor(supervisor) as JsonResult;

//            // Assert
//            Assert.IsNotNull(result);
//            dynamic jsonValue = result.Value!;
//            Assert.IsTrue(jsonValue.success);
//            Assert.AreEqual("Supervisor added successfully.", jsonValue.message);
//            _mockNotificationService!.Verify(n => n.SendSupervisorEmail(1, null, EmailMessageType.SupervisorAdded, null), Times.Once());
//        }

//        [TestMethod]
//        public async Task AddSupervisor_Fails_If_Employee_Not_Found()
//        {
//            // Arrange
//            var supervisor = new JUSTLockers.Classes.Supervisor(1, "Test", "test@test.com", new Department());
//            _mockAdminService!.Setup(a => a.CheckEmployeeExists(1)).ReturnsAsync(false);

//            // Act
//            var result = await _controller!.AddSupervisor(supervisor) as JsonResult;

//            // Assert
//            Assert.IsNotNull(result);
//            dynamic jsonValue = result.Value!;
//            Assert.IsFalse(jsonValue.success);
//            Assert.AreEqual("Employee not found in the system.", jsonValue.message);
//        }

//        [TestMethod]
//        public async Task DeleteSupervisor_Succeeds_And_Sends_Notification()
//        {
//            // Arrange
//            var supervisor = new JUSTLockers.Classes.Supervisor(1, "John Doe", "john@example.com", new Department());
//            _mockAdminService!.Setup(a => a.GetSupervisorById(1)).ReturnsAsync(supervisor);
//            _mockAdminService.Setup(a => a.DeleteSupervisor(1)).ReturnsAsync("Supervisor deleted successfully.");

//            // Act
//            var result = await _controller!.DeleteSupervisor(1) as JsonResult;

//            // Assert
//            Assert.IsNotNull(result);
//            dynamic jsonValue = result.Value!;
//            Assert.IsTrue(jsonValue.success);
//            Assert.AreEqual("Supervisor deleted successfully.", jsonValue.message);
//            _mockNotificationService!.Verify(n => n.SendSupervisorEmail(1, supervisor, EmailMessageType.SupervisorDeleted, null), Times.Once());
//        }

//        [TestMethod]
//        public void AssignCabinet_Succeeds_With_Valid_Model()
//        {
//            // Arrange
//            var cabinet = new JUSTLockers.Classes.Cabinet();
//            _mockAdminService!.Setup(a => a.AssignCabinet(cabinet)).Returns("Cabinet added successfully.");

//            // Act
//            var result = _controller!.AssignCabinet(cabinet) as RedirectToActionResult;

//            // Assert
//            Assert.IsNotNull(result);
//            Assert.AreEqual("AddCabinet", result.ActionName);
//            Assert.AreEqual("Cabinet added successfully.", _controller.TempData["SuccessMessage"]);
//        }

//        [TestMethod]
//        public async Task ApproveRequestReallocation_Succeeds_And_Sends_Notifications()
//        {
//            // Arrange
//            var studentList = new List<string> { "student@example.com" };
//            var reallocation = new JUSTLockers.Classes.Reallocation();
//            _mockAdminService!.Setup(a => a.GetAffectedStudentAsync("CAB001")).ReturnsAsync(studentList);
//            _mockAdminService.Setup(a => a.ApproveRequestReallocation(1)).ReturnsAsync(true);
//            _mockAdminService.Setup(a => a.IsDepartmentAssigned("CS", "Wing 3")).ReturnsAsync(1);
//            _mockAdminService.Setup(a => a.GetReallocationRequestById(1)).ReturnsAsync(reallocation);

//            // Act
//            var result = await _controller!.ApproveRequestReallocation(1, 1, "CS", "Wing 3", "CAB001") as RedirectToActionResult;

//            // Assert
//            Assert.IsNotNull(result);
//            Assert.AreEqual("ReallocationResponse", result.ActionName);
//            Assert.AreEqual("Reallocation request approved successfully.", _controller.TempData["Success"]);
//            _mockNotificationService!.Verify(n => n.SendSupervisorEmail(1, null, EmailMessageType.ReallocationApproved, 1), Times.Once());
//            _mockNotificationService.Verify(n => n.SendSupervisorReallocationEmail(1, null, EmailMessageType.ReallocationCabinet, 1, reallocation), Times.Once());
//            _mockNotificationService.Verify(n => n.SendStudentReallocationEmail(studentList, EmailMessageType.StudentReallocation, reallocation), Times.Once());
//        }

//        [TestMethod]
//        public async Task RejectRequestReallocation_Succeeds_And_Sends_Notification()
//        {
//            // Arrange
//            _mockAdminService!.Setup(a => a.RejectRequestReallocation(1)).ReturnsAsync(true);

//            // Act
//            var result = await _controller!.RejectRequestReallocation(1, 1) as RedirectToActionResult;

//            // Assert
//            Assert.IsNotNull(result);
//            Assert.AreEqual("ReallocationResponse", result.ActionName);
//            Assert.AreEqual("Reallocation request rejected successfully.", _controller.TempData["Success"]);
//            _mockNotificationService!.Verify(n => n.SendSupervisorEmail(1, null, EmailMessageType.ReallocationRejected, 1), Times.Once());
//        }

//        [TestMethod]
//        public async Task ChangeReportStatus_Succeeds_And_Sends_Notification()
//        {
//            // Arrange
//            var report = new JUSTLockers.Classes.Report { ReportId = 1 };
//            _mockAdminService!.Setup(a => a.ReviewReport(1)).ReturnsAsync(true);
//            _mockStudentService!.Setup(a => a.GetReportByAsync(1)).ReturnsAsync(("student@example.com", report));

//            // Act
//            var result = await _controller!.ChangeReportStatus(1) as JsonResult;

//            // Assert
//            Assert.IsNotNull(result);
//            dynamic jsonValue = result.Value!;
//            Assert.IsTrue(jsonValue.success);
//            Assert.AreEqual("Report marked as In Review successfully!", jsonValue.message);
//            _mockNotificationService!.Verify(n => n.SendUpdatedReportStudentEmail("student@example.com", ReportStatus.IN_REVIEW, report), Times.Once());
//        }

//        [TestMethod]
//        public async Task SolveReport_Succeeds_And_Sends_Notification()
//        {
//            // Arrange
//            var report = new JUSTLockers.Classes.Report { ReportId = 1 };
//            _mockAdminService!.Setup(a => a.ResolveReport(1, "Fixed issue")).ReturnsAsync(true);
//            _mockStudentService!.Setup(a => a.GetReportByAsync(1)).ReturnsAsync(("student@example.com", report));

//            // Act
//            var result = await _controller!.SolveReport(1, "Fixed issue") as JsonResult;

//            // Assert
//            Assert.IsNotNull(result);
//            dynamic jsonValue = result.Value!;
//            Assert.IsTrue(jsonValue.success);
//            Assert.AreEqual("Report resolved successfully!", jsonValue.message);
//            _mockNotificationService!.Verify(n => n.SendUpdatedReportStudentEmail("student@example.com", ReportStatus.RESOLVED, report), Times.Once());
//        }

//        [TestMethod]
//        public async Task RejectReport_Succeeds_And_Sends_Notification()
//        {
//            // Arrange
//            var report = new JUSTLockers.Classes.Report { ReportId = 1 };
//            _mockAdminService!.Setup(a => a.RejectReport(1)).ReturnsAsync(true);
//            _mockStudentService!.Setup(a => a.GetReportByAsync(1)).ReturnsAsync(("student@example.com", report));

//            // Act
//            var result = await _controller!.RejectReport(1) as JsonResult;

//            // Assert
//            Assert.IsNotNull(result);
//            dynamic jsonValue = result.Value!;
//            Assert.IsTrue(jsonValue.success);
//            Assert.AreEqual("Report rejected successfully!", jsonValue.message);
//            _mockNotificationService!.Verify(n => n.SendUpdatedReportStudentEmail("student@example.com", ReportStatus.REJECTED, report), Times.Once());
//        }
//    }
//}