using JUSTLockers.Classes;
using JUSTLockers.DataBase;
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

/*
    public AdminService( IDbConnectionFactory db)
    {
        //this.admin = admin;
        _connectionFactory = db;

    }*/
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
    public void AssignCovenant(Supervisor supervisor)
    {
        throw new NotImplementedException();
    }

    public void DeleteCovenant(Supervisor supervisor)
    {
        throw new NotImplementedException();
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

    public void SignCabinet()
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

    public void ViewSupervisorInfo(Supervisor supervisor)
    {
        throw new NotImplementedException();
    }
}