using JUSTLockers.Classes;
using JUSTLockers.DataBase;
using JUSTLockers.Service;
using JUSTLockers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JUSTLockers.Controllers
{
    [Authorize]
    public class CabinetController : Controller
    {
       // private readonly DbConnectionFactory _context;

        private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;
        private readonly CabinetService _cabinetService;
        private readonly AdminService _adminService;
        public CabinetController(IConfiguration configuration,NotificationService notificationService, CabinetService cabinet, AdminService adminService)
        {
            _configuration = configuration;
            _notificationService = notificationService;
            _cabinetService = cabinet;
            _adminService = adminService;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string cabinetId, string status)
        {
            //using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            //{
            //    connection.Open();
            //    string query = "UPDATE Cabinets SET status = @status WHERE cabinet_id = @id";

            //    using (var cmd = new MySqlCommand(query, connection))
            //    {
            //        cmd.Parameters.AddWithValue("@status", status);
            //        cmd.Parameters.AddWithValue("@id", cabinetId);
            //        cmd.ExecuteNonQuery();
            //    }OUT_OF_SERVICE
            //}

            await _cabinetService.UpdateStatusAsync(cabinetId, status);

            var student = await _adminService.GetAffectedStudentAsync(cabinetId);
            var cabinet = await _cabinetService.GetCabinetAsync(cabinetId);
           
            switch(status)
            {
                case nameof(CabinetStatus.OUT_OF_SERVICE):
                    _notificationService.SendStudentsEmail(student, CabinetStatus.OUT_OF_SERVICE, cabinet);
                    break;
                case nameof(CabinetStatus.IN_MAINTENANCE):
                    _notificationService.SendStudentsEmail(student, CabinetStatus.IN_MAINTENANCE, cabinet);
                    break;
                case nameof(CabinetStatus.DAMAGED):
                    _notificationService.SendStudentsEmail(student, CabinetStatus.DAMAGED, cabinet);
                    break;

            }
            
            return Ok();
        }

        [HttpGet]
        public async Task<JsonResult> GetCabinet(string cabinet_id)
        {
            try
            {
                //using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                //{
                //    string query = "SELECT number_cab, location, department_name, wing, level FROM Cabinets WHERE cabinet_id = @cabinet_id";

                //    using (var command = new MySqlCommand(query, connection))
                //    {
                //        command.Parameters.AddWithValue("@cabinet_id", cabinet_id);
                //        connection.Open();

                //        using (var reader = command.ExecuteReader())
                //        {
                //            if (reader.Read())
                //            {
                //                var cabinet = new
                //                {
                //                    number_cab = reader["number_cab"].ToString(),
                //                    location = reader["location"].ToString(),
                //                    department_name = reader["department_name"].ToString(),
                //                    wing = reader["wing"].ToString(),
                //                    level = reader["level"].ToString()
                //                };

                //                return Json(cabinet);
                //            }
                //        }
                //    }
  
                //}
               var cabinet= await _cabinetService.GetCabinetAsync(cabinet_id);
                if (cabinet != null)
                {
                    return Json(cabinet);
                }
                else
                {
                    return Json(null); // Not found
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json(null);
            }
        }


        [HttpGet]
        public async Task<JsonResult> GetDepartments(string location)
        {
           // List<string> departments = new List<string>();
            try
            {

                //using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                //{
                //    string query = "SELECT name FROM Departments WHERE Location = @Location";

                //    using (var command = new MySqlCommand(query, connection))
                //    {
                //        command.Parameters.AddWithValue("@Location", location);
                //        connection.Open();

                //        using (var reader = command.ExecuteReader())
                //        {
                //            while (reader.Read())
                //            {
                //                departments.Add(reader["name"].ToString());
                //            }
                //        }
                //    }
                //}
                var departments =await _cabinetService.GetDepartmentsAsync(location);
                return departments != null ? Json(departments) : Json("Not Found");
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
                //using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                //{
                //    string query = "SELECT name FROM Supervisors WHERE supervised_department = @departmentName and location =@location";
                //    using (var command = new MySqlCommand(query, connection))
                //    {
                //        command.Parameters.AddWithValue("@departmentName", departmentName);
                //        command.Parameters.AddWithValue("@location", location);
                //        connection.Open();
                //        var supervisorName = command.ExecuteScalar()?.ToString();

                //        if (string.IsNullOrEmpty(supervisorName))
                //        {
                //            return Json(new { status = "Not Found", supervisor = "" });
                //        }

                //        return Json(new { status = "Success", supervisor = supervisorName });
                //    }
                //}
                var supervisor = _cabinetService.GetSupervisorAsync(departmentName, location);
                return Json(supervisor);
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
                //using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                //{
                //    string query = "SELECT id FROM Supervisors WHERE supervised_department = @departmentName and location =@location";
                //    using (var command = new MySqlCommand(query, connection))
                //    {
                //        command.Parameters.AddWithValue("@departmentName", departmentName);
                //        command.Parameters.AddWithValue("@location", location);
                //        connection.Open();
                //        var supervisorName = command.ExecuteScalar()?.ToString();

                //        if (string.IsNullOrEmpty(supervisorName))
                //        {
                //            return Json(new { status = "Not Found", supervisor = "" });
                //        }

                //        return Json(new { status = "Success", supervisor = supervisorName });
                //    }
                //}
                var supervisorId = _cabinetService.GetSupervisorIdAsync(departmentName, location);
                return Json(supervisorId);
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
                //var lastCabinetNumber = _cabinetService.GetLastCabinetNumberAsync();
                //return Json(lastCabinetNumber.ToString());
            }
            catch (Exception ex)
            {
                return Json("Error fetching last cabinet number: " + ex.Message); // Return error message if an exception occurs
            }
        }
       
        /*
        [HttpGet]
        public List<Cabinet> GetLatestCabinets()
        {
         
                var cabinets = new List<Cabinet>();
                string query = "SELECT * FROM Cabinets ORDER BY Id DESC LIMIT 5";

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    {
                        connection.Open();
                        using (var command = new MySqlCommand(query, connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    cabinets.Add(new Cabinet
                                    {
                                        CabinetNumber = reader.GetInt32("number_cab"),
                                        Location = reader.GetString("location"),
                                        Department = reader.GetString("department_name"),
                                        Wing = reader.GetInt32("wing"),
                                        Level = reader.GetInt32("level"),
                                        Capacity = reader.GetInt32("Capacity"),
                                        EmployeeId = reader.GetInt32("supervisor_id"),
                                        // EmployeeName = reader.GetString("EmployeeName"),
                                        cabinet_id = reader.GetString("cabinet_id"),
                                        Status = (CabinetStatus)Enum.Parse(typeof(CabinetStatus), reader.GetString("Status")),

                                    });
                                }
                            }
                        }

                        return cabinets;

                    }
                }
            
           
        }*/
       

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
                //var wings = _cabinetService.GetWingsAsync(departmentName);
                //return  Json(wings) ;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return Json("Not Found");
            }
        }
    }
}
