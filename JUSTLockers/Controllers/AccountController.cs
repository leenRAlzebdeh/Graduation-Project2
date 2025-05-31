using JUSTLockers.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
            MemoryCache cache = null; // Assuming you have a MemoryCache instance set up
            await HttpContext.SignOutAsync("MyCookieAuth");

            return RedirectToAction("Login", "Home");
        }



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




        private string IsValidUser(int id, string password)
        {

            try
            {
                return _userActions.Login(id, password);
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Database error: {ex.Message}");
                return null;
            }


        }

        public JsonResult GetUser(int id)
        {
            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    // Adjust the query to match the table and fields you want
                    string query = @"SELECT name, email, supervised_department, location 
                             FROM Supervisors 
                             WHERE id = @id";

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
                                var supervisedDepartment = reader["supervised_department"].ToString();
                                var location = reader["location"].ToString();

                                return Json(new
                                {
                                    status = "Success",
                                    employee = empName,
                                    email = empEmail,
                                    supervisedDepartment = supervisedDepartment,
                                    location = location
                                });
                            }
                            else
                            {
                                return Json(new
                                {
                                    status = "Not Found",
                                    employee = "",
                                    email = "",
                                    supervisedDepartment = "",
                                    location = ""
                                });
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






    }
}
