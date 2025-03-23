using JUSTLockers.DataBase;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using JUSTLockers.Classes;
using MySqlConnector;
using JUSTLockers.Services;

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
        public IActionResult AddCabinet()
        {
            // Logic to show the Assign Cabinet page
            // return View();
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
        public async Task<IActionResult> LockerIssues()
        {
           // var adminService = new AdminService(_context);
            var reports = await _adminService.ViewForwardedReports();
            return View("~/Views/Admin/Reports.cshtml", reports);
        }
    }
}
