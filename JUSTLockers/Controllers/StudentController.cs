using Microsoft.AspNetCore.Mvc;
using JUSTLockers.Service;
using JUSTLockers.Classes;
using System.Collections.Generic;
using System.Threading.Tasks;
using JUSTLockers.Services;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Microsoft.AspNetCore.Authorization;

namespace JUSTLockers.Controllers
{
    //[Authorize]
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly IConfiguration _configuration;

        public StudentController(IStudentService studentService , IConfiguration configuration)
        {
            _studentService = studentService;
            _configuration = configuration;
        }
       

      


        public IActionResult Index()
        {
            return View();
        }

        

        [HttpGet]
        public async Task<IActionResult> ReservationView()
        {
            // Get student ID from session
            int? studentId = HttpContext.Session.GetInt32("UserId");

            if (studentId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if student is blocked
            bool isBlocked = await _studentService.IsStudentBlocked(studentId.Value);
            ViewBag.IsBlocked = isBlocked;

            // Get department info
            DepartmentInfo departmentInfo = await _studentService.GetDepartmentInfo(studentId.Value);
            if (departmentInfo == null)
            {
                return NotFound("Student not found");
            }

            // Get available wings/levels
            var wingsInfo = await _studentService.GetAvailableWingsAndLevels(departmentInfo.DepartmentName, departmentInfo.Location);

            // Check for existing reservation
            var reservation = await _studentService.GetCurrentReservationAsync(studentId.Value);

            ViewBag.DepartmentName = departmentInfo.DepartmentName;
            ViewBag.Location = departmentInfo.Location;
            ViewBag.StudentId = studentId.Value;
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

        

        [HttpGet]
        public IActionResult ReserveLocker()
        {
            return View("~/Views/Student/ReservationView.cshtml");
        }


        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReserveLocker([FromBody] ReservationRequest request)
        {
            try
            {
                // Validate request
                if (request == null ||
                    request.StudentId <= 0 ||
                    string.IsNullOrEmpty(request.DepartmentName) ||
                    string.IsNullOrEmpty(request.Location) ||
                    string.IsNullOrEmpty(request.Wing) ||
                    request.Level < 0)
                {
                    return BadRequest(new { success = false, message = "Invalid request parameters" });
                }

                var lockerId = await _studentService.ReserveLockerInWingAndLevel(
                    request.StudentId,
                    request.DepartmentName,
                    request.Location,
                    request.Wing,
                    request.Level);

                if (lockerId != null)
                {
                    return Ok(new { success = true, lockerId });
                }

                return BadRequest(new { success = false, message = "No available lockers" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetCurrentReservation(int studentId)
        {
            if (studentId <= 0)
            {
                return BadRequest("Invalid student ID");
            }

            try
            {
                var reservation = await _studentService.GetCurrentReservationAsync(studentId);
                if (reservation != null)
                {
                    return Ok(reservation);
                }
                return NotFound("No active reservation found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving reservation: {ex.Message}");
            }
        }


        [HttpGet]
        public async Task<IActionResult> ViewReservationInfo(int studentId)
        {

            if (studentId <= 0)
            {
                return BadRequest("Invalid student ID");
            }

            try
            {
                
                return await GetCurrentReservation(studentId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving reservation info: {ex.Message}");
            }
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(int studentId)
        {
            try
            {
                var result = await _studentService.CancelReservation(studentId);
                if (result)
                {
                    return Ok(new { success = true });
                }
                return BadRequest(new { success = false, message = "No active reservation found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Reserve a locker

        [HttpGet]
        public IActionResult StudentDashboard()
        {
            return View("~/Views/Home/StudentDashboard.cshtml");
        }
        [HttpGet]
        public async Task<IActionResult> DisplayReports()
        {
            
                var reports = await _studentService.ViewAllReports();
      
                return View("~/Views/Student/DisplayReports.cshtml", reports);
        }


        [HttpGet]
        public async Task<IActionResult> DeleteReport(int id)
        {
            await _studentService.DeleteReport(id);
            return RedirectToAction("DisplayReports"); 
        }
      
        [HttpPost]
        public async Task<IActionResult> SubmitProblemReport(IFormFile ImageFile, int ReportID, string LockerId, string ProblemType,string Subject, string Description)
        {
            int? reporterId = HttpContext.Session.GetInt32("UserId");
            if (reporterId == null)
                return Unauthorized();

            var result = await _studentService.SaveReportAsync(ReportID, reporterId.Value, LockerId, ProblemType, Subject, Description, ImageFile);

            if (result)
            {
                TempData["SuccessMessage"] = "Report Sent Successfully"; 
                return RedirectToAction("ReportProblem", "Student");
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to send the report. Please try again.";
                return StatusCode(500, "Failed to save the report.");
            }
        }

       
        [HttpGet]
        public IActionResult ReportProblem()
        {
            return View("~/Views/Student/ReportProblem.cshtml");
        }
        [HttpGet]
        public JsonResult GetLastReportIDJson()
        {
            try
            {
                string query = "SELECT MAX(Id) AS LastReportID FROM Reports";

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return Json(result.ToString()); // Return the last cabinet number as a string
                    }
                }
            }
            catch (Exception ex)
            {
                return Json("Error fetching last Report id: " + ex.Message); // Return error message if an exception occurs
            }
        }



        [HttpGet]
        public JsonResult GetLockerIDJson(int ReporterId)
        {
            try
            {
                string query = "SELECT LockerId FROM Reports WHERE ReporterId = @ReporterId";

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReporterId", ReporterId);

                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            return Json(result.ToString());
                        }
                        else
                        {
                            return Json("No locker found for this reporter.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json("Error fetching locker ID: " + ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AllAvailableLockers(string location = null, string department = null, string wing = null, int? level = null)
        {
            int? studentId = HttpContext.Session.GetInt32("UserId");
            if (studentId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if student is blocked
            bool isBlocked = await _studentService.IsStudentBlocked(studentId.Value);
            ViewBag.IsBlocked = isBlocked;

            // Get filter options
            var filterOptions = await _studentService.GetFilterOptions();
            ViewBag.FilterOptions = filterOptions;

            // Get current reservation if exists
            var reservation = await _studentService.GetCurrentReservationAsync(studentId.Value);
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

            // Get available lockers
            var wingsInfo = await _studentService.GetAllAvailableLockerCounts(location, department, wing, level);

            // Pass current filter values to view
            ViewBag.CurrentLocation = location;
            ViewBag.CurrentDepartment = department;
            ViewBag.CurrentWing = wing;
            ViewBag.CurrentLevel = level;

            return View("~/Views/Student/ViewAllAvailableLockers.cshtml", wingsInfo);
        }

        [HttpGet]
        public async Task<JsonResult> GetDepartments(string location)
        {
            var options = await _studentService.GetFilterOptions();
            if (options.DepartmentsByLocation.ContainsKey(location))
            {
                return Json(options.DepartmentsByLocation[location]);
            }
            return Json(new List<string>());
        }

        [HttpGet]
        public async Task<JsonResult> GetWings(string location, string department)
        {
            var options = await _studentService.GetFilterOptions();
            var key = $"{location}|{department}";
            if (options.WingsByDeptLocation.ContainsKey(key))
            {
                return Json(options.WingsByDeptLocation[key]);
            }
            return Json(new List<string>());
        }

        [HttpGet]
        public async Task<JsonResult> GetLevels(string location, string department, string wing)
        {
            var options = await _studentService.GetFilterOptions();
            var key = $"{location}|{department}|{wing}";
            if (options.LevelsByWingDeptLocation.ContainsKey(key))
            {
                return Json(options.LevelsByWingDeptLocation[key]);
            }
            return Json(new List<int>());
        }
    }

}