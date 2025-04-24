using JUSTLockers.Classes;
using JUSTLockers.DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MySqlConnector;

namespace WebApplication5.Controllers
{
    public class AccountController : Controller
    {

        private readonly IConfiguration _configuration;
      //  private readonly IDbConnectionFactory _connectionFactory;
        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;

        }

        [HttpPost]
        public IActionResult Login(int id, string password)
        {
            string role = IsValidUser(id, password);
          

            if (role == "Admin")
            {
                HttpContext.Session.SetInt32("UserId", id);
                return View("~/Views/Home/AdminDashboard.cshtml");
            }
            else if (role == "Supervisor")

            {
                HttpContext.Session.SetInt32("UserId", id);
                return RedirectToAction("SupervisorDashboard");
            }
            else if (role == "Student")
            {
                HttpContext.Session.SetInt32("UserId", id);
                return View("~/Views/Home/StudentDashboard.cshtml");
            }
            else
            {
                // Invalid credentials
                ViewBag.Error = "Invalid ID or Password.";
                return View("~/Views/Home/Login.cshtml");
            }



            //if (IsValidUser(id, password))
            //{
            //    // return RedirectToAction("LockerIssues", "Admin");
            //    // return RedirectToAction("~/Views/Admin/AddCabinet.cshtml");
            //    //  return View("~/Views/Admin/AddCabinet.cshtml");
            //    HttpContext.Session.SetInt32("UserId", id);
            //    return View("~/Views/Home/AdminDashboard.cshtml");
            //  //  return RedirectToAction("AdminDashboard", "Admin");
            //} 
            //else
            //{
            //    ViewBag.Error = "Invalid ID or password.";
            //    return View("~/Views/Home/Login.cshtml"); // Show the login page again


            //}
        }


        private string  IsValidUser(int id, string password)
        {

            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = @"
                SELECT 'Admin' AS role FROM Admins WHERE id = @ID AND password = @Password
                UNION
                SELECT 'Supervisor' AS role FROM Supervisors WHERE id = @ID AND password = @Password
                UNION
                SELECT 'Student' AS role FROM Students WHERE id = @ID AND password = @Password
                LIMIT 1"; // To ensure only one role is returned if duplicates exist

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ID", id);
                        command.Parameters.AddWithValue("@Password", password); // Consider using hashed passwords

                        connection.Open();
                        var result = command.ExecuteScalar();

                        return result != null ? result.ToString() : null;
                    }
                }
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
