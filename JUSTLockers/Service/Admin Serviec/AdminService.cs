using JUSTLockers.Classes;
using JUSTLockers.DataBase;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.ComponentModel;
namespace JUSTLockers.Services;

public class AdminService : IAdminService
{
   
   // private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConfiguration _configuration;

    public AdminService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    //emas
    public string AssignCabinet(Cabinet model)
    {
        try
        {
            string query = @"
        INSERT INTO Cabinets 
        (number_cab, wing, level, location, department_name, supervisor_id, Capacity) 
        VALUES 
        (@NumberCab, @Wing, @Level, @Location, @DepartmentName, @SupervisorId, @Capacity)";

          //  string query2 = @"update Supervisors SET  supervised_department =@DepartmentName WHERE id=@SupervisorId";


            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NumberCab", model.CabinetNumber);
                    command.Parameters.AddWithValue("@Wing", model.Wing);
                    command.Parameters.AddWithValue("@Level", model.Level);
                    command.Parameters.AddWithValue("@Location", model.Location);
                    command.Parameters.AddWithValue("@DepartmentName", model.Department);
                    command.Parameters.AddWithValue("@SupervisorId", model.EmployeeId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Capacity", model.Capacity);

                    int rowsAffected = command.ExecuteNonQuery(); // Executes the insertion and returns the number of rows affected
                    /*
                                        using (var command2 = new MySqlCommand(query2, connection))
                                        {
                                            command2.Parameters.AddWithValue("@DepartmentName", model.Department); // Dynamically set the department
                                            command2.Parameters.AddWithValue("@SupervisorId", model.EmployeeId ?? (object)DBNull.Value); // Pass supervisor ID
                                            command2.ExecuteNonQuery(); // Executes the update
                                        }
                    */

                    if (rowsAffected > 0)
                    {
                        return "Cabinet added successfully!";
                    }
                    else
                    {
                        return "Failed to add cabinet. Please try again.";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
            if (ex.Message.Contains("Duplicate entry") && ex.Message.Contains("key") && ex.Message.Contains("PRIMARY"))
            {
                return "Cabinet Number already exists";
            }
            return "Error adding cabinet: " + ex.Message;

        }
    }
    public async Task<string> AssignCovenant(int supervisorId, string departmentName)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            connection.Open();
            // Verify department exists
            var departmentCheckQuery = "SELECT COUNT(*) FROM Departments WHERE name = @DepartmentName";
            using (var departmentCheckCmd = new MySqlCommand(departmentCheckQuery, connection))
            {
                departmentCheckCmd.Parameters.AddWithValue("@DepartmentName", departmentName);
                var departmentExists = Convert.ToInt32(await departmentCheckCmd.ExecuteScalarAsync()) > 0;

                if (!departmentExists)
                {
                    return "Department does not exist.";
                }
            }

            // Check if supervisor exists
            var supervisorCheckQuery = "SELECT COUNT(*) FROM Supervisors WHERE id = @SupervisorId";
            using (var supervisorCheckCmd = new MySqlCommand(supervisorCheckQuery, connection))
            {
                supervisorCheckCmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                var supervisorExists = Convert.ToInt32(await supervisorCheckCmd.ExecuteScalarAsync()) > 0;

                if (!supervisorExists)
                {
                    return "Supervisor does not exist.";
                }
            }

            // Check if supervisor already has a covenant
            var covenantCheckQuery = "SELECT COUNT(*) FROM Supervisors WHERE id = @SupervisorId AND supervised_department IS NOT NULL";
            using (var covenantCheckCmd = new MySqlCommand(covenantCheckQuery, connection))
            {
                covenantCheckCmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                var hasCovenant = Convert.ToInt32(await covenantCheckCmd.ExecuteScalarAsync()) > 0;

                if (hasCovenant)
                {
                    return "Supervisor already has a covenant assigned.";
                }
            }

            // Assign the covenant
            var updateQuery = "UPDATE Supervisors SET supervised_department = @DepartmentName WHERE id = @SupervisorId";
            using (var updateCmd = new MySqlCommand(updateQuery, connection))
            {
                updateCmd.Parameters.AddWithValue("@DepartmentName", departmentName);
                updateCmd.Parameters.AddWithValue("@SupervisorId", supervisorId);

                int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                return rowsAffected > 0
                    ? "Covenant assigned successfully."
                    : "Failed to assign covenant.";
            }
        }
    }

    public async Task<string> DeleteCovenant(int supervisorId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            // First check if supervisor exists
            connection.Open();
            var checkQuery = "SELECT COUNT(*) FROM Supervisors WHERE id = @SupervisorId";
            using (var checkCommand = new MySqlCommand(checkQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@SupervisorId", supervisorId);
                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                if (!exists)
                {
                    return "Supervisor not found.";
                }
            }

            // Check if supervisor actually has a covenant to delete
            var covenantCheckQuery = "SELECT COUNT(*) FROM Supervisors WHERE id = @SupervisorId AND supervised_department IS NOT NULL";
            using (var covenantCheckCmd = new MySqlCommand(covenantCheckQuery, connection))
            {
                covenantCheckCmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                var hasCovenant = Convert.ToInt32(await covenantCheckCmd.ExecuteScalarAsync()) > 0;

                if (!hasCovenant)
                {
                    return "Supervisor doesn't have a covenant assigned.";
                }
            }

            // Perform the deletion
            var updateQuery = "UPDATE Supervisors SET supervised_department = NULL WHERE id = @SupervisorId";
            using (var updateCmd = new MySqlCommand(updateQuery, connection))
            {
                updateCmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                return rowsAffected > 0
                    ? "Covenant deleted successfully."
                    : "Failed to delete covenant.";
            }
        }
    }

    public async Task<Supervisor> GetSupervisorById(int supervisorId)
    {

        using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        {
            connection.Open();
            var query = @"
                SELECT s.id, s.name, s.email, d.name AS department_name, d.total_wings, d.Location 
                FROM Supervisors s
                LEFT JOIN Departments d ON s.supervised_department = d.name
                WHERE s.id = @SupervisorId";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@SupervisorId", supervisorId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        // Fetch the supervised department (if assigned)
                        Department supervisedDepartment = null;
                        if (!reader.IsDBNull(reader.GetOrdinal("department_name")))
                        {
                            supervisedDepartment = new Department
                            {
                                Name = reader.GetString("department_name"),
                                Total_Wings = reader.GetInt32("total_wings"),
                                Location = reader.GetString("Location")
                            };
                        }

                        // Create and return the Supervisor object
                        return new Supervisor(
                            id: reader.GetInt32("id"),
                            name: reader.GetString("name"),
                            email: reader.GetString("email"),
                            department: supervisedDepartment
                        );
                    }
                }
            }
        }

        return null; // Return null if no supervisor is found
    }


    //emas 
    public void Login()
    {
        throw new NotImplementedException();
    }

    public void NotifyStudents()
    {
        throw new NotImplementedException();
    }

    public void RespondForwardedReport(string respond)
    {
        throw new NotImplementedException();
    }

    public void RespondReallocation(string respond)
    {
        throw new NotImplementedException();
    }

    public void SignCabinetToNewSupervisour()
    {
        throw new NotImplementedException();
    }

    public void ViewAllCabinetsInfo()
    {
        throw new NotImplementedException();
    }
    public async Task<List<Report>> ViewForwardedReports()
    {
        var reports = new List<Report>();

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            connection.Open();
            var query = @"
            SELECT 
                r.Id AS ReportId,
                r.Subject AS ProblemDescription,
                r.Statement AS DetailedDescription,
                r.Type AS ReportType,
                r.Status AS ReportStatus,
                r.ReportDate AS ReportDate,
                r.ResolvedDate AS ResolvedDate,
                l.Id AS LockerNumber,
                l.Status AS LockerStatus,
                u.id AS ReporterId,
                u.name AS ReporterName,
                u.email AS ReporterEmail,
                d.name AS DepartmentName
            FROM 
                Reports r
            JOIN 
                Lockers l ON r.LockerId = l.Id
            JOIN 
                Students u ON r.ReporterId = u.id
            JOIN 
                Departments d ON l.DepartmentName = d.name
            WHERE 
                r.Type = 'THEFT'";

            using (var command = new MySqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    reports.Add(new Report
                    {
                        ReportId = reader.GetInt32("ReportId"),
                        Reporter = new Student(
                            reader.GetInt32("ReporterId"),
                            reader.GetString("ReporterName"),
                            reader.GetString("ReporterEmail"),
                            reader.GetString("DepartmentName") // Department is fetched from Lockers
                        ),
                        Locker = new Locker
                        {
                            LockerId = reader.GetString("LockerNumber"),
                            Status = (LockerStatus)Enum.Parse(typeof(LockerStatus), reader.GetString("LockerStatus")),
                            Department = reader.GetString("DepartmentName"),
                        },
                        Type = (ReportType)Enum.Parse(typeof(ReportType), reader.GetString("ReportType")),
                        Status = (ReportStatus)Enum.Parse(typeof(ReportStatus), reader.GetString("ReportStatus")),
                        Subject = reader.GetString("ProblemDescription"),
                        Statement = reader.GetString("DetailedDescription"),
                        ReportDate = reader.GetDateTime("ReportDate"),
                        ResolvedDate = reader.IsDBNull(reader.GetOrdinal("ResolvedDate")) ? (DateTime?)null : reader.GetDateTime("ResolvedDate")
                    });
                }
            }
        }
        return reports;
    }
    

    public void ViewNotifications()
    {
        throw new NotImplementedException();
    }

    public async Task<List<Supervisor>> ViewAllSupervisorInfo()
    {
        var supervisors = new List<Supervisor>();

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            connection.Open();
            var query = @"
            SELECT DISTINCT
                s.id, 
                s.name, 
                s.email, 
                s.location,
                s.supervised_department,
                d.name AS department_name,
                d.total_wings,
                d.Location AS department_location
            FROM 
                Supervisors s
            LEFT JOIN 
                Departments d ON s.supervised_department = d.name
            GROUP BY s.id";

            using (var command = new MySqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Department supervisedDepartment = null;

                    // Only create department object if supervisor has one assigned
                    if (!reader.IsDBNull(reader.GetOrdinal("supervised_department")))
                    {
                        supervisedDepartment = new Department
                        {
                            Name = reader.GetString("department_name"),
                            Total_Wings = reader.GetInt32("total_wings"),
                            Location = reader.GetString("department_location")
                        };
                    }

                    supervisors.Add(new Supervisor(
                        id: reader.GetInt32("id"),
                        name: reader.GetString("name"),
                        email: reader.GetString("email"),
                        department: supervisedDepartment
                    )
                    {
                        Location = reader.GetString("location")
                    });
                }
            }
        }

        return supervisors;
    }

}