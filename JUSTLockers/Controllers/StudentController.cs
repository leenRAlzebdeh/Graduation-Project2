using Microsoft.AspNetCore.Mvc;
using JUSTLockers.Service;
using JUSTLockers.Classes;
using System.Collections.Generic;
using System.Threading.Tasks;
using JUSTLockers.Services;

namespace JUSTLockers.Controllers
{
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
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