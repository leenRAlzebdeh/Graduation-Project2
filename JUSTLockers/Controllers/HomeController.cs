using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JUSTLockers.Models;
using JUSTLockers.DataBase;

namespace JUSTLockers.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DbConnectionFactory _db;

    public HomeController(ILogger<HomeController> logger, DbConnectionFactory db)
    {
        _db = db;
        _logger = logger;
    }


    public IActionResult Login()
    {
        // IEnumerable<Student> Student = _db.Student.ToList();
        return View("Login");
    }
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
