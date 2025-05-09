using JUSTLockers.DataBase;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using JUSTLockers.Classes;
using MySqlConnector;
using JUSTLockers.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Threading.Channels;
using JUSTLockers.Service;
using Microsoft.EntityFrameworkCore;

namespace JUSTLockers.Controllers
{

    public class SupervisorController : Controller
    {
        
        private readonly IConfiguration _configuration;


        private readonly SupervisorService _superService;
        private readonly IStudentService _studentService;

        private readonly IEmailService _emailService;
        public SupervisorController(SupervisorService superService, IConfiguration configuration,IEmailService emailService, IStudentService studentService)
        {
            _superService = superService;
            _configuration = configuration;
            _emailService = emailService;
            _studentService = studentService;
        }

        //[HttpGet]
        //public IActionResult ReportedIssues()
        //{
        //    return View("~/Views/Supervisor/ReportedIssues.cshtml");
        //}
        [HttpGet]
        public IActionResult SupervisorDashboard()
        {
            // Logic to show the Assign Cabinet page
            // return View();

            if(HasConvinent(HttpContext.Session.GetInt32("UserId")))
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
                else
                {
                    TempData["ErrorMessage"] = message;
                }

                return RedirectToAction("ReallocationRequestForm");
            }

            return View("~/Views/Supervisor/ReallocationRequest.cshtml", model);
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


            if (block)
            {
                if (_studentService.HasLocker(id))
                {
                    _studentService.CancelReservation(id);
                }
                string message = _superService.BlockStudent(id, supervisorId);
                TempData["Message"] = message;
                //int? userId = HttpContext.Session.GetInt32("UserId");
                //_superService.BlockStudent(id, userId);
                //TempData["SuccessMessage"] = "Student blocked successfully.";

            }
            else
            {
                string message = _superService.UnblockStudent(id, supervisorId);
                TempData["Message"] = message;
                //_superService.UnblockStudent(id);
                //TempData["SuccessMessage"] = "Student unblocked successfully.";
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

        public bool HasConvinent(int? userId)
        {

            bool hasConvinent = false;

            try

            {
                int count;

                string query = @"SELECT COUNT(*) FROM Supervisors 
                 WHERE id = @userId
                 AND supervised_department IS NOT NULL 
                ";

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);

                        count = Convert.ToInt32(cmd.ExecuteScalar());
                        hasConvinent = count > 0;

                    }
                }


            }
            catch (Exception ex)
            {

                TempData["ErrorMessage"] = "An error occurred while checking super Convinent status: " + ex.Message;
            }


            return hasConvinent;
        }



    }
}


