using JUSTLockers.Classes;
using JUSTLockers.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Data;
using System.Linq.Expressions;

namespace JUSTLockers.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {

        private readonly IConfiguration _configuration;

        public DashboardController(IConfiguration configuration)
        {
            _configuration = configuration;

        }

        [HttpGet]
        public JsonResult GetsupervisorNumberJson()
        {
            try
            {
                string query = "SELECT COUNT(*) AS totalSupervisors FROM Supervisors WHERE user_type = 'supervisor' ";

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return Json(result.ToString()); // Return the last cabinet number as a string
                    }
                }
            }
            catch (Exception ex)
            {
                return Json("Error fetching last super num number: " + ex.Message); // Return error message if an exception occurs
            }
        }

        [HttpGet]
        public JsonResult GetPendingRequestsNumberJson()
        {
            try
            {
                string query = "SELECT COUNT(*) AS totalPending FROM Reallocation  WHERE RequestStatus = 'Pending'";

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return Json(result.ToString()); // Return the last cabinet number as a string
                    }
                }
            }
            catch (Exception ex)
            {
                return Json("Error fetching pending  number: " + ex.Message); // Return error message if an exception occurs
            }
        }


        [HttpGet]
        public JsonResult GetReportsNumberJson()
        {
            try
            {
                string query = "SELECT COUNT(*) AS totalPending FROM Reports WHERE Status = 'IN_REVIEW'";

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return Json(result.ToString()); // Return the last cabinet number as a string
                    }
                }
            }
            catch (Exception ex)
            {
                return Json("Error fetching Reports number: " + ex.Message); // Return error message if an exception occurs
            }
        }


        [HttpGet]
        public JsonResult GetNameJson()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Json("User ID not found in session");

            try
            {
                string query = @"
            SELECT name FROM Admins WHERE id = @userId
            UNION
            SELECT name FROM Supervisors WHERE id = @userId
            UNION
            SELECT name FROM Students WHERE id = @userId";

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);

                        var result = command.ExecuteScalar();
                        return Json(result?.ToString() ?? "Name not found");
                    }
                }
            }
            catch (Exception ex)
            {
                return Json("Error fetching name: " + ex.Message);
            }
        }



        [HttpGet]
        public JsonResult GetMajorJson()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Json("User ID not found in session");

            try
            {
                string query = @"
            
            SELECT Major FROM Students WHERE id = @userId";

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);

                        var result = command.ExecuteScalar();
                        return Json(result?.ToString() ?? "Major not found");
                    }
                }
            }
            catch (Exception ex)
            {
                return Json("Error fetching : " + ex.Message);
            }
        }

        [HttpGet]
        public JsonResult GetLastCabinetNumberJson()
        {
            try
            {
                string query = "SELECT MAX(number_cab) AS LastCabinetNumber FROM Cabinets";

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return Json(result.ToString()); // Return the last cabinet number as a string
                    }
                }
            }
            catch (Exception ex)
            {
                return Json("Error fetching  Last cab  number: " + ex.Message); // Return error message if an exception occurs
            }
        }


    }
}
