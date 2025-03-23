using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace JUSTLockers.Controllers
{
    public class CabinetController : Controller
    {

        private readonly IConfiguration _configuration;

        public CabinetController(IConfiguration configuration)
        {
            _configuration = configuration;
        }




        [HttpGet]
        public JsonResult GetDepartments(string location)
        {
            List<string> departments = new List<string>();
            try
            {

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = "SELECT name FROM Departments WHERE Location = @Location";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Location", location);
                        connection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                departments.Add(reader["name"].ToString());
                            }
                        }
                    }
                }

                return Json(departments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json("Not Found");
            }
        }

        public JsonResult GetSupervisor(string departmentName, string location)
        {
            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = "SELECT name FROM Supervisors WHERE supervised_department = @departmentName and location =@location";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@departmentName", departmentName);
                        command.Parameters.AddWithValue("@location", location);
                        connection.Open();
                        var supervisorName = command.ExecuteScalar()?.ToString();

                        if (string.IsNullOrEmpty(supervisorName))
                        {
                            return Json(new { status = "Not Found", supervisor = "" });
                        }

                        return Json(new { status = "Success", supervisor = supervisorName });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json(new { status = "Error", message = "Database error occurred" });
            }
        }



        public JsonResult GetID(string departmentName, string location)
        {
            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = "SELECT id FROM Supervisors WHERE supervised_department = @departmentName and location =@location";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@departmentName", departmentName);
                        command.Parameters.AddWithValue("@location", location);
                        connection.Open();
                        var supervisorName = command.ExecuteScalar()?.ToString();

                        if (string.IsNullOrEmpty(supervisorName))
                        {
                            return Json(new { status = "Not Found", supervisor = "" });
                        }

                        return Json(new { status = "Success", supervisor = supervisorName });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json(new { status = "Error", message = "Database error occurred" });
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
                return Json("Error fetching last cabinet number: " + ex.Message); // Return error message if an exception occurs
            }
        }

        [HttpGet]
        public JsonResult GetWings(string departmentName)
        {
            int totalWings = 0;
            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = "SELECT total_wings FROM Departments WHERE name = @DepartmentName";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DepartmentName", departmentName);
                        connection.Open();

                        totalWings = Convert.ToInt32(command.ExecuteScalar());
                    }
                }

                var wings = Enumerable.Range(1, totalWings)
                                      .Select(w => w.ToString())
                                      .ToList();

                return Json(wings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json("Not Found");
            }
        }
    }
}
