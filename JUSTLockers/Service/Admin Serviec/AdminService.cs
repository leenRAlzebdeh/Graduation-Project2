using JUSTLockers.Classes;
using JUSTLockers.DataBase;
using MailKit.Search;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.ComponentModel;
using System.Data;
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
    //public string AddSupervisor(Supervisor supervisor)
    //{

    //    try
    //    {
    //        string query = "INSERT INTO Supervisors (id, name, email, supervised_department, location) VALUES (@Id, @Name, @Email, @DepartmentName, @Location)";

    //        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
    //        {
    //            connection.Open();
    //            using (var command = new MySqlCommand(query, connection))
    //            {
    //                command.Parameters.AddWithValue("@Id", supervisor.Id);
    //                command.Parameters.AddWithValue("@Name", supervisor.Name);
    //                command.Parameters.AddWithValue("@Email", supervisor.Email);
    //                command.Parameters.AddWithValue("@DepartmentName", supervisor.DepartmentName); // Avoid null exception

    //                command.Parameters.AddWithValue("@Location", supervisor.Location);

    //                int rowsAffected = command.ExecuteNonQuery();
    //                if (rowsAffected > 0)
    //                {
    //                    return "Supervisor added successfully!";
    //                }
    //                else
    //                {
    //                    return "Failed to add supervisor. Please try again.";
    //                }
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        if(ex.Message.Contains("Duplicate entry") && ex.Message.Contains("key") && ex.Message.Contains("PRIMARY"))
    //        {
    //            return "Supervisor ID already exists";
    //        }

    //        return $"Error: {ex.Message}";
    //    }
    //}
    public async Task<bool> CheckEmployeeExists(int employeeId)
    {
        try
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM Employees WHERE id = @EmployeeId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EmployeeId", employeeId);

                    var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0;
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error (you should inject ILogger<AdminController> in the constructor)


            // In case of error, return false to prevent false positives
            return false;
        }
    }
    public async Task<(bool Success, string Message)> AddSupervisor(Supervisor supervisor)
    {
        try
        {
            string query = @"INSERT INTO Supervisors 
                        (id, name, email, supervised_department, location) 
                        VALUES (@Id, @Name, @Email, @DepartmentName, @Location)";

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", supervisor.Id);
                    command.Parameters.AddWithValue("@Name", supervisor.Name);
                    command.Parameters.AddWithValue("@Email", supervisor.Email);
                    command.Parameters.AddWithValue("@DepartmentName",
                        string.IsNullOrEmpty(supervisor.DepartmentName) ? DBNull.Value : supervisor.DepartmentName);
                    command.Parameters.AddWithValue("@Location",
                        string.IsNullOrEmpty(supervisor.Location) ? DBNull.Value : supervisor.Location);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    return rowsAffected > 0
                        ? (true, "Supervisor added successfully!")
                        : (false, "Failed to add supervisor");
                }
            }
        }
        catch (MySqlException ex) when (ex.Number == 1062) // Duplicate entry
        {
            return (false, "Supervisor ID already exists");
        }
        catch (Exception ex)
        {
            return (false, $"Error adding supervisor: {ex.Message}");
        }
    }

    public async Task<bool> SupervisorExists(int supervisorId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = "SELECT COUNT(*) FROM Supervisors WHERE id = @Id";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", supervisorId);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
        }
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
           // if (ex.Message.Contains("Duplicate entry") && ex.Message.Contains("key") && ex.Message.Contains("PRIMARY"))
           // {
           //     return "Cabinet Number already exists";
           // }
            return "Error adding cabinet: " + ex.Message;

        }
    }
    public async Task<string> AssignCovenant(int supervisorId, string departmentName, string location)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();

            // 1. Verify department exists at the specified location
            //var departmentQuery = "SELECT name FROM Departments WHERE name = @DepartmentName AND Location = @Location";
            //string departmentNameFromDb = null;

            //using (var departmentCmd = new MySqlCommand(departmentQuery, connection))
            //{
            //    departmentCmd.Parameters.AddWithValue("@DepartmentName", departmentName);
            //    departmentCmd.Parameters.AddWithValue("@Location", location);

            //    using (var reader = await departmentCmd.ExecuteReaderAsync())
            //    {
            //        if (await reader.ReadAsync())
            //        {
            //            departmentNameFromDb = reader.IsDBNull("name") ? null : reader.GetString("name");
            //        }
            //        else
            //        {
            //            return "Department does not exist at the specified location.";
            //        }
            //    }
            //}

            // 2. Get supervisor's current details
            var supervisorQuery = "SELECT id, name FROM Supervisors WHERE id = @SupervisorId";
            string supervisorName = null;

            using (var supervisorCmd = new MySqlCommand(supervisorQuery, connection))
            {
                supervisorCmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                using (var reader = await supervisorCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        supervisorName = reader.IsDBNull("name") ? null : reader.GetString("name");
                    }
                    else
                    {
                        return "Supervisor does not exist.";
                    }
                }
            }

            // 3. Check if this department+location is already assigned to someone else
            //var assignmentCheckQuery = @"SELECT COUNT(*) FROM Supervisors 
            //                      WHERE supervised_department = @DepartmentName 
            //                      AND location = @Location
            //                      AND id != @SupervisorId";

            //using (var checkCmd = new MySqlCommand(assignmentCheckQuery, connection))
            //{
            //    checkCmd.Parameters.AddWithValue("@DepartmentName", departmentName);
            //    checkCmd.Parameters.AddWithValue("@Location", location);
            //    checkCmd.Parameters.AddWithValue("@SupervisorId", supervisorId);

            //    var alreadyAssigned = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            //    if (alreadyAssigned)
            //    {
            //        return "This department at this location is already assigned to another supervisor.";
            //    }
            //}

            // 4. Update supervisor's covenant and location
            var updateQuery = @"UPDATE Supervisors 
                        SET supervised_department = @DepartmentName,
                            location = @Location
                        WHERE id = @SupervisorId";

            using (var updateCmd = new MySqlCommand(updateQuery, connection))
            {
                updateCmd.Parameters.AddWithValue("@DepartmentName", departmentName);
                updateCmd.Parameters.AddWithValue("@Location", location);
                updateCmd.Parameters.AddWithValue("@SupervisorId", supervisorId);

                int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                return rowsAffected > 0
                    ? $"Covenant assigned successfully. {supervisorName} is now responsible for {departmentName} at {location}."
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
            var updateQuery = "UPDATE Supervisors SET supervised_department = NULL ,location = NULL WHERE id = @SupervisorId";
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
                r.ResolvedDetails AS ResolutionDetails,
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
                            reader.GetString("DepartmentName")
                            
                            // Department is fetched from Lockers
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
                        ResolvedDate = reader.IsDBNull(reader.GetOrdinal("ResolvedDate")) ? (DateTime?)null : reader.GetDateTime("ResolvedDate"),
                        ResolutionDetails = reader.IsDBNull(reader.GetOrdinal("ResolutionDetails")) ? null : reader.GetString("ResolutionDetails"),
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



    //public async Task<List<Cabinet>> ViewCabinetInfo()
    //{
    //    var cabinets = new List<Cabinet>();

    //    using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
    //    {
    //        await connection.OpenAsync();

    //        var query = @"SELECT number_cab, wing, level, location, department_name, 
    //                        cabinet_id, Capacity, status 
    //                  FROM Cabinets";

    //        using (var command = new MySqlCommand(query, connection))
    //        using (var reader = await command.ExecuteReaderAsync())
    //        {
    //            while (await reader.ReadAsync())
    //            {
    //                cabinets.Add(new Cabinet
    //                {
    //                    CabinetNumber = reader.GetInt32("number_cab"),
    //                   // Wing = reader.GetString("wing"),
    //                    Level = reader.GetInt32("level"),
    //                    Location = reader.GetString("location"),
    //                    Department = reader.GetString("department_name"),
    //                    //EmployeeId = reader.IsDBNull(reader.GetOrdinal("supervisor_id"))
    //                    //             ? null
    //                    //             : reader.GetInt32("supervisor_id"),
    //                    Cabinet_id = reader.GetString("cabinet_id"),
    //                    Capacity = reader.GetInt32("Capacity"),
    //                    Status = Enum.TryParse<CabinetStatus>(reader.GetString("status"), out var statusEnum)
    //                             ? statusEnum
    //                             : CabinetStatus.IN_SERVICE, // default if unknown
    //                    EmployeeName = "" // Optional: can join with Employees table if needed
    //                });
    //            }
    //        }
    //    }

    //    return cabinets;
    //}

    public async Task<List<Cabinet>> ViewCabinetInfo(string? searchCab=null, string? location = null, int? level = null, string? department = null, string? status = null, string? wing = null)
    {
        var cabinets = new List<Cabinet>();
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = @"SELECT number_cab, wing, level, location, department_name, 
                         cabinet_id, Capacity, status 
                      FROM Cabinets WHERE 1=1";

            var command = new MySqlCommand();
            command.Connection = connection;

            if (!string.IsNullOrEmpty(searchCab) && searchCab != "")
            {
                query += " AND cabinet_id like @searchCab";
                command.Parameters.AddWithValue("@searchCab", "%" + searchCab.Trim() + "%");

                // command.Parameters.AddWithValue("@searchCab", $"%{searchCab}%");
            }



            if (!string.IsNullOrEmpty(location) && location != "All")
            {
                query += " AND location = @location";
                command.Parameters.AddWithValue("@location", location);
            }

            if (level.HasValue)
            {
                query += " AND level = @level";
                command.Parameters.AddWithValue("@level", level.Value);
            }

            if (!string.IsNullOrEmpty(department) && department != "All Dep")
            {
                query += " AND department_name = @department";
                command.Parameters.AddWithValue("@department", department);
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query += " AND status = @status";
                command.Parameters.AddWithValue("@status", status);
            }

            if (!string.IsNullOrEmpty(wing) && wing != "All")
            {
                query += " AND wing = @wing";
                command.Parameters.AddWithValue("@wing", wing);
            }
            query += " order by department_name";
            command.CommandText = query;

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    cabinets.Add(new Cabinet
                    {
                        CabinetNumber = reader.GetInt32("number_cab"),
                        Wing = reader["wing"].ToString(),
                        Level = reader.GetInt32("level"),
                        Location = reader["location"].ToString(),
                        Department = reader["department_name"].ToString(),
                        Cabinet_id = reader["cabinet_id"].ToString(),
                        Capacity = reader.GetInt32("Capacity"),
                        Status = Enum.TryParse<CabinetStatus>(reader["status"].ToString(), out var statusEnum)
                                 ? statusEnum
                                 : CabinetStatus.IN_SERVICE,
                        EmployeeName = ""
                    });






                }
            }


        }

        return cabinets;
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
                            Name = reader.IsDBNull(reader.GetOrdinal("supervised_department"))? null: reader.GetString("supervised_department"),
                            Total_Wings = reader.IsDBNull(reader.GetOrdinal("total_wings"))? 0: reader.GetInt32("total_wings"),
                            Location = reader.IsDBNull(reader.GetOrdinal("location"))? null: reader.GetString("location")
                        };
                    }

                    supervisors.Add(new Supervisor(
                        id: reader.GetInt32("id"),
                        name: reader.GetString("name"),
                        email: reader.GetString("email"),
                        department: supervisedDepartment
                    )
                    {
                        Location = reader.IsDBNull(reader.GetOrdinal("location"))? null : reader.GetString("location")
                    });
                }
            }
        }

        return supervisors;
    }



    public async Task<List<Department>> GetDepartments()
    {
        var departments = new List<Department>();

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            connection.Open();
            var query = "SELECT name, total_wings, Location FROM Departments";

            using (var command = new MySqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    departments.Add(new Department
                    {
                        Name = reader.GetString("name"),
                        Total_Wings = reader.GetInt32("total_wings"),
                        Location = reader.GetString("Location")
                    });
                }
            }
        }

        return departments;
    }
    public async Task<List<Department>> GetDepartmentsByLocation(string location)
    {
        var departments = new List<Department>();

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = "SELECT name, Location FROM Departments WHERE Location = @Location";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Location", location);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        departments.Add(new Department
                        {
                            Name = reader.GetString("name"),
                            Location = reader.GetString("Location")
                        });
                    }
                }
            }
        }

        return departments;
    }


    public async Task<bool> ResolveReport(int reportId, string? resolutionDetails)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();

            var query = @"UPDATE Reports 
                     SET Status = 'RESOLVED', 
                         ResolvedDate = @ResolvedDate,
                         ResolvedDetails = @ResolutionDetails
                     WHERE Id = @ReportId";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ReportId", reportId);
                command.Parameters.AddWithValue("@ResolvedDate", DateTime.Now);
                command.Parameters.AddWithValue("@ResolutionDetails", resolutionDetails);

                return await command.ExecuteNonQueryAsync() > 0;
            }
        }
    }

    public async Task<bool> ReviewReport(int reportId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();

            var query = @"UPDATE Reports 
                     SET Status = 'IN_REVIEW' 
                     WHERE Id = @ReportId";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ReportId", reportId);
                return await command.ExecuteNonQueryAsync() > 0;
            }
        }
    }

    public async Task<bool> RejectReport(int reportId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();

            var query = @"UPDATE Reports 
                     SET Status = 'REJECTED', 
                         ResolvedDate = @ResolvedDate
                     WHERE Id = @ReportId";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ReportId", reportId);
                command.Parameters.AddWithValue("@ResolvedDate", DateTime.Now);
                return await command.ExecuteNonQueryAsync() > 0;
            }
        }
    }

    public async Task<Report> GetReportDetails(int reportId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = @"
            SELECT r.Id AS ReportId, r.LockerId, r.Subject, r.Statement, r.Status, r.ReportDate, r.ResolvedDate, r.ResolvedDetails,
                   l.DepartmentName, s.id AS ReporterId, s.name, s.email, s.department
            FROM Reports r
            JOIN Lockers l ON r.LockerId = l.Id
            JOIN Students s ON r.ReporterId = s.id
            WHERE r.Id = @ReportId";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ReportId", reportId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Report
                        {
                            ReportId = reader.GetInt32("ReportId"),
                            Locker = new Locker
                            {
                                LockerId = reader.GetString("LockerId"),
                                Department = reader.GetString("DepartmentName")
                            },
                            Subject = reader.GetString("Subject"),
                            Statement = reader.GetString("Statement"),
                            Status = Enum.Parse<ReportStatus>(reader.GetString("Status")),
                            ReportDate = reader.GetDateTime("ReportDate"),
                            ResolvedDate = reader.IsDBNull("ResolvedDate") ? null : reader.GetDateTime("ResolvedDate"),
                            ResolutionDetails = reader.IsDBNull("ResolvedDetails") ? null : reader.GetString("ResolvedDetails"),
                            Reporter = new Student
                            (
                                 reader.GetInt32("ReporterId"),
                                 reader.GetString("name"),
                                reader.GetString("email"),
                                reader.GetString("department")
                            )
                        };
                    }
                }
            }
        }
        return null; // Return null if no report is found
    }


    public async Task<bool> IsDepartmentAssigned(string departmentName, string location)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = @"SELECT COUNT(*) FROM Supervisors 
                     WHERE supervised_department = @DepartmentName 
                     AND location = @Location";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@DepartmentName", departmentName);
                command.Parameters.AddWithValue("@Location", location);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
        }
    }
}