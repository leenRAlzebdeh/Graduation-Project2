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
        private readonly IDbConnectionFactory _context;

        public AdminController(IDbConnectionFactory context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LockerIssues()
        {
            var adminService = new AdminService(_context);
            var reports = await adminService.ViewForwardedReports();
            return View("~/Views/Admin/Reports.cshtml", reports);
        }
    }
}
