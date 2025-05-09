using JUSTLockers.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApplication5.Controllers
{
    public class AccountController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly UserActions _userActions;
        //  private readonly IDbConnectionFactory _connectionFactory;
        public AccountController(IConfiguration configuration, UserActions userActions)
        {
            _configuration = configuration;
            _userActions = userActions;

        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Clear the session or authentication cookie
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync("MyCookieAuth");

            return RedirectToAction("Login", "Home");
        }



        //[HttpPost]
        //public IActionResult Login(int id, string password)
        //{
        //    string role = IsValidUser(id, password);
        //    HttpContext.Session.SetInt32("UserId", id);
        //    var claims = new List<Claim>
        //    {
        //        new Claim(ClaimTypes.NameIdentifier, id.ToString())
        //    };

        //    var identity = new ClaimsIdentity(claims, "MyCookieAuth");
        //    var principal = new ClaimsPrincipal(identity);

        //     HttpContext.SignInAsync("MyCookieAuth", principal);

        //    if (role == "Admin")
        //    {

        //        return View("~/Views/Home/AdminDashboard.cshtml");
        //    }
        //    else if (role == "Supervisor")

        //    {
        //       // HttpContext.Session.SetInt32("UserId", id);
        //        return RedirectToAction("SupervisorDashboard", "Supervisor");
        //    }
        //    else if (role == "Student")
        //    {
        //       // HttpContext.Session.SetInt32("UserId", id);
        //        //return View("~/Views/Home/StudentDashboard.cshtml");
        //        return RedirectToAction("StudentDashboard", "Student");
        //    }
        //    else
        //    {
        //        // Invalid credentials
        //        ViewBag.Error = "Invalid ID or Password.";
        //        return View("~/Views/Home/Login.cshtml");
        //    }



        //    //if (IsValidUser(id, password))
        //    //{
        //    //    // return RedirectToAction("LockerIssues", "Admin");
        //    //    // return RedirectToAction("~/Views/Admin/AddCabinet.cshtml");
        //    //    //  return View("~/Views/Admin/AddCabinet.cshtml");
        //    //    HttpContext.Session.SetInt32("UserId", id);
        //    //    return View("~/Views/Home/AdminDashboard.cshtml");
        //    //  //  return RedirectToAction("AdminDashboard", "Admin");
        //    //} 
        //    //else
        //    //{
        //    //    ViewBag.Error = "Invalid ID or password.";
        //    //    return View("~/Views/Home/Login.cshtml"); // Show the login page again


        //    //}
        //}

        [HttpPost]
        public async Task<IActionResult> Login(int id, string password)
        {
            await HttpContext.SignOutAsync("MyCookieAuth");

            string role = IsValidUser(id, password);
            HttpContext.Session.SetInt32("UserId", id);

            if (role != null)
            {
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("UserId", id.ToString())
        };

                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("MyCookieAuth", principal);

                // Redirect based on role
                if (role == "Admin")
                {

                    return View("~/Views/Home/AdminDashboard.cshtml");
                }
                else if (role == "Supervisor")

                {
                    
                    return RedirectToAction("SupervisorDashboard", "Supervisor");
                }
                else if (role == "Student")
                { 
                    return RedirectToAction("StudentDashboard", "Student");
                }
                else
                {
                  
                    ViewBag.Error = "Invalid ID or Password.";
                    return View("~/Views/Home/Login.cshtml");
                }
            }

            ViewBag.Error = "Invalid ID or Password.";
            return View("~/Views/Home/Login.cshtml");
        }

        private string  IsValidUser(int id, string password)
        {

            try
            {
                //using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                //{
                //    string query = @"
                //SELECT 'Admin' AS role FROM Admins WHERE id = @ID AND password = @Password
                //UNION
                //SELECT 'Supervisor' AS role FROM Supervisors WHERE id = @ID AND password = @Password
                //UNION
                //SELECT 'Student' AS role FROM Students WHERE id = @ID AND password = @Password
                //LIMIT 1"; // To ensure only one role is returned if duplicates exist

                //    using (var command = new MySqlCommand(query, connection))
                //    {
                //        command.Parameters.AddWithValue("@ID", id);
                //        command.Parameters.AddWithValue("@Password", password); // Consider using hashed passwords

                //        connection.Open();
                //        var result = command.ExecuteScalar();

                //        return result != null ? result.ToString() : null;
                //    }
                //}
                return _userActions.Login(id, password);
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Database error: {ex.Message}");
                return null;
            }


        }
    }
}
