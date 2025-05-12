using Microsoft.AspNetCore.Mvc;
using JUSTLockers.Classes;
using MySqlConnector;
using JUSTLockers.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace JUSTLockers.Controllers
{
    [Authorize]
    public class SupervisorController : Controller
    {
        
        private readonly IConfiguration _configuration;


        private readonly SupervisorService _superService;
        private readonly IStudentService _studentService;
        private readonly AdminService _adminService;
        private readonly NotificationService _notificationService;
        public SupervisorController(SupervisorService superService, IConfiguration configuration, IStudentService studentService , AdminService adminService,NotificationService notificationService)
        {
            _superService = superService;
            _configuration = configuration;
            _studentService = studentService;
            _adminService = adminService;
            _notificationService = notificationService;
        }

        //}
        [HttpGet]
        public async Task<IActionResult> SupervisorDashboard()
        {
            // Logic to show the Assign Cabinet page
            // return View();
            var hasCovenant = await HasCovenant(HttpContext.Session.GetInt32("UserId"));
            if (hasCovenant)
            {
                return View("~/Views/Home/SupervisorDashboard.cshtml");
            }
            else
            {
                //TempData["ErrorMessage"] = "You don't have a convenient department assigned. Contact the Admin To Re assign!";
                return RedirectToAction("SupervisorDashboardNoCon","Supervisor");
            }
           
        }
        [HttpGet]
        public IActionResult SupervisorDashboardNoCon()
        {
            return View("~/Views/Supervisor/SupervisorDashboardNoCon.cshtml");

        }
        [HttpGet]
        public async Task<IActionResult> ViewCabinetInfoSuper(string? searchCab, string? location, string? wing, int? level, string? department, string? status)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            var cabinets = await _superService.ViewCabinetInfo(userId,searchCab, location, level, department, status, wing);
            

            return View("~/Views/Supervisor/ViewCabinetInfoSuper.cshtml", cabinets);

        }

        [HttpPost]
        public async Task<IActionResult> ReallocationRequest(Reallocation model)
        {
            if (ModelState.IsValid)
            {   
                string message = await _superService.ReallocationRequest(model); // Pass model

                if (message.StartsWith("Request sent"))
                {
                    TempData["SuccessMessage"] = message;
                }
                else if (message.StartsWith("You are not allowed"))
                {
                    TempData["cabinetError"] = message;
                }
                else if (message.StartsWith("This request has"))
                {
                    TempData["ErrorMessage"] = message;
                }
                else
                {
                    TempData["ErrorMessage"] = message;
                }

                return RedirectToAction("ReallocationRequestForm");
            }

            return View("~/Views/Supervisor/ReallocationRequest.cshtml", model);
        }


        [HttpPost]
        public async Task<IActionResult> ReallocationRequestFormSameDep(Reallocation model)
        {
            
            if (ModelState.IsValid)
            {
                var student = await _adminService.GetAffectedStudentAsync(model.CurrentCabinetID);
                
                var message = await _superService.ReallocationRequestFormSameDep(model); 
                var reallocation = await _adminService.GetReallocationRequestById(message.requestId);

                if (message.message.StartsWith("Cabinet reallocation was successful"))
                {
                    TempData["SuccessMessage"] = message.message;
                    if(student != null) 
                    _notificationService.SendStudentReallocationEmail(student, EmailMessageType.StudentReallocation, reallocation);

                }
                else if(message.message.StartsWith("You are not allowed"))
                {
                    TempData["cabinetError"] = message.message;
                }
                else if (message.message.StartsWith("You Need"))
                {
                    TempData["ErrorMessage"] = message.message;
                }
                else
                {
                    TempData["ErrorMessage"] = message.message;
                }

                return RedirectToAction("ReallocationRequestFormSameDepartment");
            }

            return View("~/Views/Supervisor/ReallocationRequestFormSameDep.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ReportedIssues()
        {
            int? userId=HttpContext.Session.GetInt32("UserId");
            var reports = await _superService.ViewReportedIssues(userId);
            return View("~/Views/Supervisor/ReportedIssues.cshtml", reports);
        }
        [HttpGet]
        public async Task<IActionResult> BlockStudent()
        {
          var BlockedStudents = await _superService.BlockedStudents();
            return View("~/Views/Supervisor/BlockStudent.cshtml", BlockedStudents);
        }

        [HttpGet]
        public async Task<IActionResult> ViewStudentInfo(int? searchstu )
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            var students = await _superService.ViewAllStudentReservations(userId, searchstu);
            return View("~/Views/Supervisor/ViewStudentInfo.cshtml", students);
        }


        [HttpGet]
        public async Task<IActionResult> TheftIssues(string filter)
        {
            var reports = await _superService.TheftIssues(filter);
            return View("~/Views/Supervisor/ReportedIssues.cshtml", reports);
        }

        public async Task<IActionResult> SendToAdmin(int reportId)
        {
            await _superService.SendToAdmin(reportId);
            return RedirectToAction("ReportedIssues");
        }

        [HttpGet]
        public IActionResult ReallocationRequestForm()
        {
            return View("~/Views/Supervisor/ReallocationRequest.cshtml");
        }
        [HttpGet]
        public IActionResult ReallocationRequestFormSameDepartment()
        {
            return View("~/Views/Supervisor/ReallocationRequestFormSameDep.cshtml");
        }

        [HttpPost]
    public JsonResult SearchStudent(int id)
    {
        var student = _superService.GetStudentById(id);
        if (student == null)
            return Json(new { exists = false });

        return Json(new
        {
            exists = true,
            student = new
            {
                id = student.Id,
                name = student.Name,
                email = student.Email,
                department = student.Department,
                location = student.Location,
                lockerId = student.LockerId,
                isBlocked = student.IsBlocked
            }
        });
    }

        [HttpPost]
        public IActionResult ToggleBlock(int id, bool block)
        {
            int? supervisorId = HttpContext.Session.GetInt32("UserId");

            var student = _superService.GetStudentById(id);
            if (block)
            {
                if (_studentService.HasLocker(id))
                {
                    _studentService.CancelReservation(id, "EMPTY");
                }
                string message = _superService.BlockStudent(id, supervisorId);
                
                _notificationService.SendStudentEmail(student.Email, EmailMessageType.StudentBlocked, null);
                TempData["Message"] = message;
           }
            else
            {
                string message = _superService.UnblockStudent(id, supervisorId);
                _notificationService.SendStudentEmail(student.Email, EmailMessageType.StudentUnblocked, null);
                TempData["Message"] = message;
            }

            return RedirectToAction("BlockStudent");
        }

        [HttpGet]
        public IActionResult GetUserLocationAndDepartment(int userId)
        {
            try
            {
                string location = null;
                string department = null;

                string query = "SELECT location, supervised_department FROM Supervisors WHERE id = @userId";

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                location = reader["location"]?.ToString();
                                department = reader["supervised_department"]?.ToString();
                            }
                            else
                            {
                                return NotFound(new { message = "Supervisor not found." });
                            }
                        }
                    }
                }

                return Json(new
                {
                    location,
                    department
                });
            }
            catch (Exception ex)
            {
                // Optional: log the error here
                return StatusCode(500, new { message = "An error occurred.", details = ex.Message });
            }
        }
         [HttpGet]
        public IActionResult ReserveLocker()
        {
            return View("~/Views/Supervisor/ReservationView.cshtml");
        }
        public async Task<IActionResult> ReservationView()
        {
          
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if student is blocked
            //bool isBlocked = await _studentService.IsStudentBlocked(studentId.Value);
            //ViewBag.IsBlocked = isBlocked;

            // Get department info
            DepartmentInfo departmentInfo = await _superService.GetDepartmentInfo(userId.Value);
            if (departmentInfo == null)
            {
                return NotFound("super not found");
            }

            // Get available wings/levels
            var wingsInfo = await _studentService.GetAvailableWingsAndLevels(departmentInfo.DepartmentName, departmentInfo.Location);

            // Check for existing reservation
            var reservation = await _studentService.GetCurrentReservationAsync(userId.Value);

            ViewBag.DepartmentName = departmentInfo.DepartmentName;
            ViewBag.Location = departmentInfo.Location;
            ViewBag.StudentId = userId.Value;
            ViewBag.HasReservation = reservation != null;

            if (reservation != null)
            {
                ViewBag.ReservationInfo = new
                {
                    LockerId = reservation.LockerId,
                    Date = reservation.Date.ToString("yyyy-MM-dd HH:mm"),

                    Status = reservation.Status.ToString()
                };
            }

            return View(wingsInfo);
        }


        public async Task<bool> HasCovenant(int? userId)
        {

            bool hasCovenant = await _superService.HasCovenant(userId);

            //try

            //{
            //    int count;

            //    string query = @"SELECT COUNT(*) FROM Supervisors 
            //     WHERE id = @userId
            //     AND supervised_department IS NOT NULL 
            //    ";

            //    using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            //    {
            //        connection.Open();
            //        using (var cmd = new MySqlCommand(query, connection))
            //        {
            //            cmd.Parameters.AddWithValue("@userId", userId);

            //            count = Convert.ToInt32(cmd.ExecuteScalar());
            //            hasConvinent = count > 0;

            //        }
            //    }


            //}
            //catch (Exception ex)
            //{

            //    TempData["ErrorMessage"] = "An error occurred while checking super Convinent status: " + ex.Message;
            //}


            return hasCovenant;
        }



    }
}


