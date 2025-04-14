using JUSTLockers.DataBase;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using JUSTLockers.Classes;
using MySqlConnector;
using JUSTLockers.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace JUSTLockers.Controllers
{

    public class AdminController : Controller
    {
        //  private readonly IDbConnectionFactory _context;

        /*  public AdminController(IDbConnectionFactory context)
           {
               _context = context;
           }
        */
        private readonly IConfiguration _configuration;

       
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService, IConfiguration configuration)
        {
            _adminService = adminService;
            _configuration = configuration;
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
            //var _adminService = new AdminService(_context);
            if (ModelState.IsValid)
            {
                // Call the AssignCabinet method and capture the returned message
                string message = _adminService.AssignCabinet(model);

                // Set the message to TempData
                if (message.StartsWith("Cabinet added"))
                {
                    TempData["SuccessMessage"] = message; // Success message
                }
                else
                {
                    TempData["ErrorMessage"] = message; // Error message
                }

                // Redirect to the desired view (e.g., Index)
                return View("~/Views/Admin/AddCabinet.cshtml");
            }
            //return View("~/Views/Admin/AddCabinet.cshtml");
             return View(model); // If the model is invalid, return the same view
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
        public async Task<IActionResult> LockerIssues()
        {
            var reports = await _adminService.ViewForwardedReports();
            return View("~/Views/Admin/Reports.cshtml", reports);
        }

        [HttpPost]
        public async Task<IActionResult> AssignCovenant(int supervisorId, string departmentName)
        {


            var result = await _adminService.AssignCovenant(supervisorId, departmentName);

            if (result.StartsWith("Covenant assigned"))
            {
                TempData["SuccessMessage"] = result;
            }
            else
            {
                TempData["ErrorMessage"] = result;
            }

            return RedirectToAction("SignCovenant");
        }


        //public JsonResult GetEmployee(int id)
        //{
        //    try
        //    {
        //        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //        {
        //            string query = "SELECT name,email FROM Employees WHERE id = @id";
        //            using (var command = new MySqlCommand(query, connection))
        //            {
        //                command.Parameters.AddWithValue("@id", id);

        //                connection.Open();
        //                var empName = command.ExecuteScalar()?.ToString();
        //                var empEmail = command.ExecuteScalar()?.ToString();
        //                if (string.IsNullOrEmpty(empName) && string.IsNullOrEmpty(empEmail))
        //                {
        //                    return Json(new { status = "Not Found", employee = "" });
        //                }

        //                return Json(new { status = "Success", employee = empName ,email=empEmail });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Database error: {ex.Message}");
        //        return Json(new { status = "Error", message = "Database error occurred" });
        //    }
        //}
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







        // Delete a covenant from a supervisor
        [HttpPost]
        public async Task<IActionResult> DeleteCovenant(int supervisorId)
        {
            var result = await _adminService.DeleteCovenant(supervisorId);

            if (result.StartsWith("Covenant deleted"))
            {
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
        public async Task<IActionResult> ResolveReport(int reportId, string? resolutionDetails)
        {
            

            var success = await _adminService.ResolveReport(reportId,resolutionDetails);

            if (success)
            {
                TempData["SuccessMessage"] = "Report resolved successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to resolve report.";
            }

            return RedirectToAction("LockerIssues");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReport(int reportId)
        {
            var success = await _adminService.DeleteReport(reportId);
            if (success)
            {
                TempData["SuccessMessage"] = "Report deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete report.";
            }
            return RedirectToAction("LockerIssues");
        }



    }
}


