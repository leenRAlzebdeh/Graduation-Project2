using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MySqlConnector;

namespace WebApplication5.Controllers
{
    public class AccountController : Controller
    {

        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public IActionResult Login(int id, string password)
        {

            if (IsValidUser(id, password))
            {
                return RedirectToAction("Index", "Home");

            }
            else
            {
                ViewBag.Error = "Invalid ID or password.";
                return View("~/Views/Home/Login.cshtml"); // Show the login page again


            }
        }


        private bool IsValidUser(int id, string password)
        {
            try {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = "SELECT COUNT(1) FROM Admins WHERE id = @ID AND password = @Password";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ID", id);
                        command.Parameters.AddWithValue("@Password", password); // For demonstration only; consider hashing.

                        connection.Open();
                        int count = Convert.ToInt32(command.ExecuteScalar());

                        return count == 1;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Database error: {ex.Message}");
                return false;

            }



        }
    }
}
