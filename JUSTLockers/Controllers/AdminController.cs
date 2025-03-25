using JUSTLockers.DataBase;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using JUSTLockers.Classes;
using MySqlConnector;
using JUSTLockers.Services;
using System.Threading.Tasks;

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
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<IActionResult> SignCovenant()
        {
            var supervisors = await _adminService.ViewAllSupervisorInfo();
            return View(supervisors);
        }

        [HttpGet]
        public IActionResult DeleteCovenant()
        {
            return View();
        }
        [HttpGet]
        public IActionResult AddCabinet()
        {
            return View();
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

            return View(model); // If the model is invalid, return the same view
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> ViewSupervisorInfo(string filter = "All")
        {
            var supervisors = await _adminService.ViewSupervisorInfo(filter);
            ViewData["Filter"] = filter; 
            return View("~/Views/Admin/ViewSupervisorInfo.cshtml", supervisors);
        }


        [HttpGet]
        public async Task<IActionResult> LockerIssues()
        {
           // var adminService = new AdminService(_context);
            var reports = await _adminService.ViewForwardedReports();
            return View("~/Views/Admin/Reports.cshtml", reports);
        }

        [HttpPost]
        public async Task<IActionResult> AssignCovenant(int supervisorId, string departmentName)
        {
            //Supervisor supervisor = await _adminService.GetSupervisorById(supervisorId);
            //var result = await _adminService.AssignCovenant(supervisor, departmentName);

            //if (result.StartsWith("Covenant assigned"))
            //{
            //    TempData["SuccessMessage"] = result;
            //}
            //else
            //{
            //    TempData["ErrorMessage"] = result;
            //}
            //return RedirectToAction("SignCovenant");

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
    }
}
