using JUSTLockers.Classes;
using JUSTLockers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace JUSTLockers.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {

        private readonly IConfiguration _configuration;


        private readonly AdminService _adminService;
        private readonly IEmailService _emailService;
        private readonly NotificationService _notificationService;
        public AdminController(AdminService adminService, IConfiguration configuration, IEmailService emailService, NotificationService notificationService)
        {
            _adminService = adminService;
            _configuration = configuration;
            _emailService = emailService;
            _notificationService = notificationService;
        }



        [HttpGet]
        public async Task<IActionResult> SignCovenant()
        {
            var supervisors = await _adminService.ViewAllSupervisorInfo();
            var departments = await _adminService.GetDepartments();
            ViewBag.Departments = departments;
            return View(supervisors);
        }

        [HttpGet]
        public IActionResult DeleteCovenant()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AddSupervisor()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSupervisor(Supervisor supervisor)
        {
            try
            {

                // Check if employee exists
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
            // Logic to show the Assign Cabinet page
            // return View();
            return View("~/Views/Home/AdminDashboard.cshtml");
        }
        [HttpPost]
        public IActionResult AssignCabinet(Cabinet model)
        {

            if (ModelState.IsValid)
            {
                // Call the AssignCabinet method and capture the returned message
                string message = _adminService.AssignCabinet(model);

                // Set the message to TempData
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
        public async Task<IActionResult> ViewSupervisorInfo(string filter = "All")
        {
            //sorry emas 
            // var supervisors = await _adminService.ViewAllSupervisorInfo(filter);
            var supervisors = await _adminService.ViewAllSupervisorInfo();
            ViewData["Filter"] = filter;
            return View("~/Views/Admin/ViewSupervisorInfo.cshtml", supervisors);
        }

        [HttpGet]
        public async Task<IActionResult> ViewCabinetInfo(string? searchCab, string? location, string? wing, int? level, string? department, string? status)
        {

            var cabinets = await _adminService.ViewCabinetInfo(searchCab, location, level, department, status, wing);
            return View(cabinets);

        }

        [HttpGet]
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
        public async Task<IActionResult> ApproveRequestReallocation(int requestId, int SupervisorID, string RequestedDepartment, string RequestLocation, string CurrentCabinetID)
        {
            var student = await _adminService.GetAffectedStudentAsync(CurrentCabinetID);
            var success = await _adminService.ApproveRequestReallocation(requestId);
           
            if (success)
            {

                _notificationService.SendSupervisorEmail(SupervisorID, null, EmailMessageType.ReallocationApproved, requestId);
                var affectedSuper = await _adminService.IsDepartmentAssigned(RequestedDepartment, RequestLocation);
                var reallocationRequest = await _adminService.GetReallocationRequestById(requestId);
                _notificationService.SendSupervisorReallocationEmail(affectedSuper, null, EmailMessageType.ReallocationCabinet, requestId , reallocationRequest);
                _notificationService.SendStudentReallocationEmail(student, EmailMessageType.StudentReallocation, reallocationRequest);
                TempData["Success"] = "Reallocation request approved successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to approve the reallocation request."+$"{success}";
            }
            return RedirectToAction("ReallocationResponse", "Admin");
        }
        [HttpPost]
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
        public async Task<IActionResult> LockerIssues()
        {
            var reports = await _adminService.ViewForwardedReports();
            return View("~/Views/Admin/Reports.cshtml", reports);
        }

        [HttpPost]
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeReportStatus(int reportId)
        {
            try
            {
                var success = await _adminService.ReviewReport(reportId);
                if (success)
                {
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int reportId, string? resolutionDetails)
        {
            try
            {
                var success = await _adminService.ResolveReport(reportId, resolutionDetails);
                if (success)
                {
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReport(int reportId)
        {
            try
            {
                var success = await _adminService.RejectReport(reportId);
                if (success)
                {
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

                //if (isAssigned!=0)
                //{
                //    // You might want to add logic here to check if it's assigned to the current supervisor
                //    // being edited, in which case it wouldn't be an error
                //}

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


    }
}


