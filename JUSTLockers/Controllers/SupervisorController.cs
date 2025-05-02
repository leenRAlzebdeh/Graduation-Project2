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

namespace JUSTLockers.Controllers
{

    public class SupervisorController : Controller
    {
        
        private readonly IConfiguration _configuration;


        private readonly SupervisorService _superService;
        private readonly IEmailService _emailService;
        public SupervisorController(SupervisorService superService, IConfiguration configuration,IEmailService emailService)
        {
            _superService = superService;
            _configuration = configuration;
            _emailService = emailService;
        }

        //[HttpGet]
        //public IActionResult ReportedIssues()
        //{
        //    return View("~/Views/Supervisor/ReportedIssues.cshtml");
        //}


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
            var reports = await _superService.ViewReportedIssues();
            return View("~/Views/Supervisor/ReportedIssues.cshtml", reports);
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

    }
}


