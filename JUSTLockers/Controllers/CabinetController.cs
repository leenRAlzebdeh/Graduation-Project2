using JUSTLockers.Classes;
using JUSTLockers.Service;
using JUSTLockers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace JUSTLockers.Controllers
{
    [Authorize]
    public class CabinetController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;
        private readonly CabinetService _cabinetService;
        private readonly AdminService _adminService;
        public CabinetController(IConfiguration configuration,NotificationService notificationService, CabinetService cabinet, AdminService adminService)
        {
            _configuration = configuration;
            _notificationService = notificationService;
            _cabinetService = cabinet;
            _adminService = adminService;
        }
        [HttpPost]
        [Authorize(Roles = "Admin,Supervisor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string cabinetId, string status)
        {

            var student = await _adminService.GetAffectedStudentAsync(cabinetId);
            await _cabinetService.UpdateStatusAsync(cabinetId, status); 
            var cabinet = await _cabinetService.GetCabinetAsync(cabinetId);
           
            switch(status)
            {
                case nameof(CabinetStatus.OUT_OF_SERVICE):
                    _notificationService.SendStudentsEmail(student, CabinetStatus.OUT_OF_SERVICE, cabinet);
                    break;
                case nameof(CabinetStatus.IN_MAINTENANCE):
                    _notificationService.SendStudentsEmail(student, CabinetStatus.IN_MAINTENANCE, cabinet);
                    break;
                case nameof(CabinetStatus.DAMAGED):
                    _notificationService.SendStudentsEmail(student, CabinetStatus.DAMAGED, cabinet);
                    break;
            }
            
            return Ok();
        }
        [HttpGet]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<JsonResult> GetCabinet(string cabinet_id)
        {
            try
            {
               
               var cabinet= await _cabinetService.GetCabinetAsync(cabinet_id);
                if (cabinet != null)
                {
                    return Json(cabinet);
                }
                else
                {
                    return Json(null); // Not found
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json(null);
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetDepartments(string location)
        {
            try
            {
                var departments =await _cabinetService.GetDepartmentsAsync(location);
                return departments != null ? Json(departments) : Json("Not Found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json("Not Found");
            }
        }
        public JsonResult GetSupervisor(string departmentName, string location)
        {
            try
            {
                var supervisor = _cabinetService.GetSupervisorAsync(departmentName, location);
                return Json(supervisor);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json(new { status = "Error", message = "Database error occurred" });
            }
        }
        public JsonResult GetID(string departmentName, string location)
        {
            try
            {
                var supervisorId = _cabinetService.GetSupervisorIdAsync(departmentName, location);
                return Json(supervisorId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json(new { status = "Error", message = "Database error occurred" });
            }
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> GetLastCabinetNumberJson()
        {
            
            try
            {
                string lastCabinetNumber =await _cabinetService.GetLastCabinetNumberAsync();

                Console.WriteLine($"Last Cabinet Number: {lastCabinetNumber}");
                return Json(lastCabinetNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json("Error fetching last cabinet number: " + ex.Message); // Return error message if an exception occurs
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetWings(string departmentName)
        {
            try
            {
                var wings =await _cabinetService.GetWingsAsync(departmentName);
                return Json(wings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json("Not Found");
            }
        }
    }
}
