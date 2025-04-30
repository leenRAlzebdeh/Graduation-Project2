using Microsoft.AspNetCore.Mvc;
using JUSTLockers.Service;
using JUSTLockers.Classes;
using System.Collections.Generic;
using System.Threading.Tasks;
using JUSTLockers.Services;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace JUSTLockers.Controllers
{
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

        // View available lockers for a department
        [HttpGet]
        public async Task<IActionResult> ViewAvailableLockers(string departmentName)
        {
            if (string.IsNullOrEmpty(departmentName))
            {
                return BadRequest("Department name is required");
            }

            try
            {
                var availableLockers = await _studentService.ViewAvailableLockers(departmentName);
                return Ok(availableLockers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving available lockers: {ex.Message}");
            }
        }

        // Reserve a locker
        [HttpPost]
        public async Task<IActionResult> ReserveLocker(int StudentId, string LockerId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _studentService.ReserveLocker(StudentId,LockerId);
                if (result)
                {
                    return Ok(new { Message = "Locker reserved successfully" });
                }
                return BadRequest("Locker is not available for reservation");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error reserving locker: {ex.Message}");
            }
        }

        // View reservation information for a student
        [HttpGet]
        public async Task<IActionResult> ViewReservationInfo(int studentId)
        {
            if (studentId <= 0)
            {
                return BadRequest("Invalid student ID");
            }

            try
            {
                var reservationInfo = await _studentService.ViewReservationInfo(studentId);
                if (reservationInfo != null)
                {
                    return Ok(reservationInfo);
                }
                return NotFound("No reservation found for this student");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving reservation info: {ex.Message}");
            }
        }
        [HttpGet]
        public IActionResult StudentDashboard()
        {
            // Logic to show the Assign Cabinet page
            // return View();
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



        // Cancel a reservation
        [HttpDelete]
        public async Task<IActionResult> CancelReservation(int studentId, string reservationId)
        {
            if (studentId <= 0 || string.IsNullOrEmpty(reservationId))
            {
                return BadRequest("Invalid student ID or reservation ID");
            }

            try
            {
                var result = await _studentService.CancelReservation(studentId, reservationId);
                if (result)
                {
                    return Ok(new { Message = "Reservation canceled successfully" });
                }
                return NotFound("Reservation not found or already canceled");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error canceling reservation: {ex.Message}");
            }
        }
    }

}