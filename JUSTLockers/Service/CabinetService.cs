using JUSTLockers.Services;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;

namespace JUSTLockers.Service
{
    public class CabinetService
    {
        private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;
        private readonly IStudentService _studentService;
        private readonly IMemoryCache _memoryCache;
        public CabinetService(IConfiguration configuration,
            NotificationService notificationService, IStudentService studentService, IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _notificationService = notificationService;
            _studentService = studentService;
            _memoryCache = memoryCache;
        }
        public async Task UpdateStatusAsync(string cabinetId, string status)
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                string query = "UPDATE Cabinets SET status = @status WHERE cabinet_id = @id";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@id", cabinetId);
                    await cmd.ExecuteNonQueryAsync();
                }
                if (status == "IN_MAINTENANCE" || status == "OUT_OF_SERVICE" || status == "DAMAGED")
                {
                    await UpdateLockersStatus(cabinetId, status);
                }
                else if (status == "IN_SERVICE")
                {
                    string updateQuery = "UPDATE Lockers SET Status = 'EMPTY' WHERE cabinet_id = @id";
                    using (var cmd = new MySqlCommand(updateQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", cabinetId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    AdminService.ClearCache(_memoryCache, "CabinetInfo_");
                    AdminService.ClearCache(_memoryCache, "AvailableWings_");
                    AdminService.ClearCache(_memoryCache, "AvailableLockers_");
                }
            }
        }
        public async Task UpdateLockersStatus(string cabinetId, string status)
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Step 1: Update locker status
                        string updateQuery = "UPDATE Lockers SET Status = @status WHERE cabinet_id = @id";
                        using (var cmd = new MySqlCommand(updateQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@status", status);
                            cmd.Parameters.AddWithValue("@id", cabinetId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        // Update student record
                        string updateStudentQuery = @"
                            UPDATE Students s
                            JOIN Lockers l ON s.locker_id = l.Id
                            SET s.locker_id = NULL
                            WHERE l.cabinet_id = @cabinetId";
                        using (var studentCmd = new MySqlCommand(updateStudentQuery, connection, transaction))
                        {
                            studentCmd.Parameters.AddWithValue("@cabinetId", cabinetId);
                            await studentCmd.ExecuteNonQueryAsync();
                        }

                        // Delete the reservation record
                        var updateReservationQuery = "DELETE r FROM Reservations r JOIN Lockers l ON r.LockerId = l.Id WHERE l.cabinet_id = @cabinetId";

                        using (var reservationCmd = new MySqlCommand(updateReservationQuery, connection, transaction))
                        {
                            reservationCmd.Parameters.AddWithValue("@cabinetId", cabinetId);
                            await reservationCmd.ExecuteNonQueryAsync();
                        }

                        var updateLockerQuery = "UPDATE Lockers SET Status = @status WHERE cabinet_id = @cabinetId";
                        using (var lockerCmd = new MySqlCommand(updateLockerQuery, connection, transaction))
                        {
                            lockerCmd.Parameters.AddWithValue("@cabinetId", cabinetId);
                            lockerCmd.Parameters.AddWithValue("@status", status);
                            await lockerCmd.ExecuteNonQueryAsync();
                        }

                        AdminService.ClearCache(_memoryCache,"CabinetInfo_");
                        AdminService.ClearCache(_memoryCache,"AvailableWings_");
                        AdminService.ClearCache(_memoryCache,"AvailableLockers_");
                        AdminService.ClearCache(_memoryCache,$"CurrentReservation_"); 
                        AdminService.ClearCache(_memoryCache,$"HasLocker-"); 
                        AdminService.ClearCache(_memoryCache,$"StudentReservation_");


                        // Step 4: Commit transaction
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        // Roll back transaction on error
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Error updating lockers status: {ex.Message}");
                        throw;
                    }


                }
            }
        }
        public async Task<object> GetCabinetAsync(string cabinetId)
        {
            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = "SELECT number_cab, location, department_name, wing, level,status FROM Cabinets WHERE cabinet_id = @cabinet_id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@cabinet_id", cabinetId);
                        await connection.OpenAsync();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new
                                {
                                    number_cab = reader["number_cab"].ToString(),
                                    location = reader["location"].ToString(),
                                    department_name = reader["department_name"].ToString(),
                                    wing = reader["wing"].ToString(),
                                    level = reader["level"].ToString(),
                                    status = reader["status"].ToString(),
                                    cabinet_id = cabinetId
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return null;
            }
        }
        public async Task<List<string>> GetDepartmentsAsync(string location)
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
                        await connection.OpenAsync();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                departments.Add(reader["name"].ToString());
                            }
                        }
                    }
                }
                return departments;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return null;
            }
        }
        public async Task<object> GetSupervisorAsync(string departmentName, string location)
        {
            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = "SELECT name FROM Supervisors WHERE supervised_department = @departmentName AND location = @location";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@departmentName", departmentName);
                        command.Parameters.AddWithValue("@location", location);
                        await connection.OpenAsync();
                        var result = await command.ExecuteScalarAsync();
                        var supervisorName = result.ToString();

                        if (string.IsNullOrEmpty(supervisorName))
                        {
                            return new { status = "Not Found", supervisor = "" };
                        }

                        return new { status = "Success", supervisor = supervisorName };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return new { status = "Error", message = "Database error occurred" };
            }
        }
        public async Task<object> GetSupervisorIdAsync(string departmentName, string location)
        {

            try
            {
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = "SELECT id FROM Supervisors WHERE supervised_department = @departmentName AND location = @location";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@departmentName", departmentName);
                        command.Parameters.AddWithValue("@location", location);
                        await connection.OpenAsync();
                        var result = await command.ExecuteScalarAsync();
                        var supervisorId = result?.ToString();

                        if (string.IsNullOrEmpty(supervisorId))
                        {
                            return new { status = "Not Found", supervisor = "" };
                        }

                        return new { status = "Success", supervisor = supervisorId };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return new { status = "Error", message = "Database error occurred" };
            }
        }
        public async Task<string> GetLastCabinetNumberAsync()
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
                        return result.ToString(); // Return the last cabinet number as a string
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return "Error fetching last cabinet number: " + ex.Message;
            }
        }
        public async Task<List<string>> GetWingsAsync(string departmentName)
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
                        await connection.OpenAsync();

                        totalWings = Convert.ToInt32(await command.ExecuteScalarAsync());
                        return Enumerable.Range(1, totalWings)
                                        .Select(w => w.ToString())
                                        .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return null;
            }
        }
    }

}

