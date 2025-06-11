using JUSTLockers.Classes;
using JUSTLockers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace JUSTLockers.Controllers
{
  

    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly AdminService _adminService;
        private readonly IEmailService _emailService;
        private readonly NotificationService _notificationService;
        private readonly IStudentService _studentService;
        public AdminController(AdminService adminService, IConfiguration configuration, 
            IEmailService emailService, NotificationService notificationService, IStudentService studentService)
        {
            _adminService = adminService;
            _configuration = configuration;
            _emailService = emailService;
            _notificationService = notificationService;
            _studentService = studentService;
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SignCovenant()
        {
            var supervisors = await _adminService.ViewAllSupervisorInfo();
            var departments = await _adminService.GetDepartments();
            ViewBag.Departments = departments;

            return View(supervisors);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteCovenant()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AddSupervisor()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSupervisor(Supervisor supervisor)
        {
            try
            {
                var employeeExists = await _adminService.CheckEmployeeExists(supervisor.Id);
                if (!employeeExists)
                {
                    return Json(new { success = false, message = "Employee not found in the system." });
                    
                }

                // Check if supervisor already exists
                var supervisorExists = await _adminService.GetSupervisorById(supervisor.Id);
                if (supervisorExists != null)
                {
                    return Json(new { success = false, message = "This employee is already a supervisor." });
                }

                // Validate department assignment if provided
                if (!string.IsNullOrEmpty(supervisor.DepartmentName) && !string.IsNullOrEmpty(supervisor.Location))
                {
                    var isAssigned = await _adminService.IsDepartmentAssigned(supervisor.DepartmentName, supervisor.Location);
                    if (isAssigned != 0)
                    {
                        return Json(new { success = false, message = $"Department {supervisor.DepartmentName} at {supervisor.Location} is already assigned." });
                    }
                }

                // Add supervisor
                var result = await _adminService.AddSupervisor(supervisor);

                if (result.Success)
                {
                    _notificationService.SendSupervisorEmail(supervisor.Id, null, EmailMessageType.SupervisorAdded, null);

                    return Json(new { success = true, message = result.Message });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupervisor([FromBody] int supervisorId)
        {
            try
            {
                var supervisor = await _adminService.GetSupervisorById(supervisorId);
                var result = await _adminService.DeleteSupervisor(supervisorId);

                if (result.StartsWith("Supervisor deleted"))
                {
                    _notificationService.SendSupervisorEmail(supervisorId, supervisor, EmailMessageType.SupervisorDeleted, null);

                    return Json(new { success = true, message = result });

                }

                return Json(new { success = false, message = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }


        [HttpGet]
        public IActionResult ReallocationResponsePage()
        {
            return View("~/Views/Admin/ReallocationResponsePage.cshtml");
        }


        [HttpGet]
        public IActionResult AddCabinet()
        {
            return View("~/Views/Admin/AddCabinet.cshtml");
        }
        [HttpGet]
        public IActionResult AdminDashboard()
        {
           
            return View("~/Views/Home/AdminDashboard.cshtml");
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]

        public IActionResult AssignCabinet(Cabinet model)
        {

            if (ModelState.IsValid)
            {
                string message = _adminService.AssignCabinet(model);

                
                if (message.StartsWith("Cabinet added"))
                {
                    TempData["SuccessMessage"] = message;
                }
                else
                {
                    TempData["ErrorMessage"] = message;
                }
               

                return RedirectToAction("AddCabinet");
            }
            return View("~/Views/Admin/AddCabinet.cshtml");

        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> ViewSupervisorInfo(string filter = "All")
        {
            //sorry emas 
            var supervisors = await _adminService.ViewAllSupervisorInfo();
            ViewData["Filter"] = filter;
            return View("~/Views/Admin/ViewSupervisorInfo.cshtml", supervisors);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Supervisor")]

        public async Task<IActionResult> ViewCabinetInfo(string? searchCab, string? location, string? wing, int? level, string? department, string? status)
        {

            var cabinets = await _adminService.ViewCabinetInfo(searchCab, location, level, department, status, wing);
            return View(cabinets);

        }

        [HttpGet]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> ReallocationResponse()
        {
            var requests = await _adminService.ReallocationResponse();

            if (requests == null || !requests.Any())
            {
                ViewBag.Message = "No reallocation requests to Approve.";
            }


            return View("~/Views/Admin/ReallocationResponse.cshtml", requests);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> ApproveRequestReallocation(int requestId, int SupervisorID, string RequestedDepartment, string RequestLocation, string CurrentCabinetID)
        {
            var student = await _adminService.GetAffectedStudentAsync(CurrentCabinetID);
            var success = await _adminService.ApproveRequestReallocation(requestId);
           
            if (success)
            {
                //some method for the notification system
                _notificationService.SendSupervisorEmail(SupervisorID, null, EmailMessageType.ReallocationApproved, requestId);
                var affectedSuper = await _adminService.IsDepartmentAssigned(RequestedDepartment, RequestLocation);
                var reallocationRequest = await _adminService.GetReallocationRequestById(requestId);
                if(affectedSuper != 0)
                {
                    _notificationService.SendSupervisorReallocationEmail(affectedSuper, null, EmailMessageType.ReallocationCabinet, requestId, reallocationRequest);
                }
                if(student != null)
                    _notificationService.SendStudentReallocationEmail(student, EmailMessageType.StudentReallocation, reallocationRequest);
                TempData["Success"] = "Reallocation request approved successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to approve the reallocation request. "+ $"{success}";
            }
            return RedirectToAction("ReallocationResponse", "Admin");
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> RejectRequestReallocation(int requestId, int SupervisorID)
        {
            bool success = await _adminService.RejectRequestReallocation(requestId);

            if (success)
            {
                _notificationService.SendSupervisorEmail(SupervisorID, null, EmailMessageType.ReallocationRejected, requestId);
                TempData["Success"] = "Reallocation request rejected successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to reject the reallocation request.";
            }
            return RedirectToAction("ReallocationResponse", "Admin");
        }



        [HttpGet]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> LockerIssues()
        {
            var reports = await _adminService.ViewForwardedReports();
            return View("~/Views/Admin/Reports.cshtml", reports);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> AssignCovenant(int supervisorId, string departmentName, string location)
        {


            var result = await _adminService.AssignCovenant(supervisorId, departmentName, location);

            if (result.StartsWith("Covenant assigned"))
            {
                _notificationService.SendSupervisorEmail(supervisorId, null, EmailMessageType.CovenantSigned, null);
                TempData["SuccessMessage"] = result;
            }
            else
            {
                TempData["ErrorMessage"] = result;
            }

            return RedirectToAction("SignCovenant");
        }
        

        public JsonResult GetEmployee(int id)
        {
            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = "SELECT name, email FROM Employees WHERE id = @id";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        connection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var empName = reader["name"].ToString();
                                var empEmail = reader["email"].ToString();

                                return Json(new { status = "Success", employee = empName, email = empEmail });
                            }
                            else
                            {
                                return Json(new { status = "Not Found", employee = "", email = "" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json(new { status = "Error", message = "Database error occurred" });
            }
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> DeleteCovenant(int supervisorId)
        {
            var supervisor = await _adminService.GetSupervisorById(supervisorId);
            var result = await _adminService.DeleteCovenant(supervisorId);

            if (result.StartsWith("Covenant deleted"))
            {
                _notificationService.SendSupervisorEmail(supervisorId, supervisor, EmailMessageType.CovenantDeleted, null);
                TempData["SuccessMessage"] = result;
            }
            else
            {
                TempData["ErrorMessage"] = result;
            }
            return RedirectToAction("SignCovenant");


        }
        [HttpPost]
        [Authorize(Roles = "Admin,Supervisor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeReportStatus(int reportId)
        {
            try
            {
                var success = await _adminService.ReviewReport(reportId);
                if (success)
                {
                    var (email, report) = await _studentService.GetReportByAsync(reportId);
                    _notificationService.SendUpdatedReportStudentEmail(email, ReportStatus.IN_REVIEW, report);
                    return Json(new { success = true, message = "Report marked as In Review successfully!" });
                }
                return Json(new { success = false, message = "Failed to update report status." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating report status: {ex.Message}" });
            }
        }


        [HttpPost]
        [Authorize(Roles = "Admin,Supervisor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolveReport(int reportId, string? resolutionDetails)
        {
            try
            {
                var success = await _adminService.SolveReport(reportId, resolutionDetails);
                if (success)
                {
                    var (email, report) = await _studentService.GetReportByAsync(reportId);
                    _notificationService.SendUpdatedReportStudentEmail(email, ReportStatus.RESOLVED, report);
                    return Json(new { success = true, message = "Report resolved successfully!" });
                }
                return Json(new { success = false, message = "Failed to resolve report." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error resolving report: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Supervisor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReport(int reportId)
        {
            try
            {
                var success = await _adminService.RejectReport(reportId);
                if (success)
                {
                    var (email, report) = await _studentService.GetReportByAsync(reportId);
                    _notificationService.SendUpdatedReportStudentEmail(email, ReportStatus.REJECTED, report);
                    return Json(new { success = true, message = "Report rejected successfully!" });
                }
                return Json(new { success = false, message = "Failed to reject report." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error rejecting report: {ex.Message}" });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> GetReportDetails(int reportId)
        {
            try
            {
                var report = await _adminService.GetReportDetails(reportId);
                if (report == null)
                {
                    return NotFound(new { status = "Error", message = "Report not found" });
                }
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "Error", message = "Internal server error: " + ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> IsDepartmentAssigned(string departmentName, string location)
        {
            try
            {
                // Check if department is assigned to any supervisor
                var isAssigned = await _adminService.IsDepartmentAssigned(departmentName, location);

                return Json(new { isAssigned });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByLocation(string location)
        {
            try
            {
                var departments = await _adminService.GetDepartmentsByLocation(location);
                return Json(departments);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Student")]

        public async Task<IActionResult> SemesterSettings()
        {
            var settings = await _adminService.GetSemesterSettings();
            return View("~/Views/Admin/SemesterSettings.cshtml", settings);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleSemesterEnd(DateTime? endDate, int? settingsId)
        {
            try
            {
                if (!endDate.HasValue || endDate.Value < DateTime.Now.AddDays(1))
                {
                    TempData["ErrorMessage"] = "End date must be at least 1 days in the future.";
                    return RedirectToAction("SemesterSettings");
                }

                var result = await _adminService.SaveSemesterSettings(endDate, settingsId);
                if (result)
                {
                    var message = settingsId.HasValue ? "Semester end date updated successfully." : "Semester end date scheduled successfully.";
                    var users = await _adminService.GetAllSupervisorsEmails();
                    await _emailService.SemesterEndNotificationAsync(
                        users,
                        "SupervisorsSemesterEndNotification",
                         new Dictionary<string, DateTime>
                        {
                            { "EndDate", endDate.Value }
                        });
                    return Json(new { success = true, message });
                }
                return Json(new { success = false, message = "Failed to save semester end date." });
               
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error scheduling semester end: {ex.Message}";
            }
            return RedirectToAction("SemesterSettings");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualSemesterEnd()
        {
            try
            {    DateTime dateTime = DateTime.Now.AddDays(5);
                var result = await _adminService.SaveSemesterSettings(DateTime.Now.AddDays(5));
                if (result)
                {
                    var users = await _adminService.GetAllSupervisorsEmails();
                    await _emailService.SemesterEndNotificationAsync(
                        users,
                        "SupervisorsSemesterEndNotification",
                        new Dictionary<string, DateTime>
                        {
                            { "EndDate", dateTime }
                        }
                    );
                    TempData["SuccessMessage"] = "Manual semester end triggered. Notifications will be sent, and reservations will be canceled in 5 days.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to trigger manual semester end.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error triggering manual semester end: {ex.Message}";
            }
            return RedirectToAction("SemesterSettings");
        }


    }
}


