using JUSTLockers.Classes;
using JUSTLockers.DataBase;
using MySqlConnector;
using System.ComponentModel;
namespace JUSTLockers.Services;

public class AdminService : IAdminService
{
   
    private readonly IDbConnectionFactory _connectionFactory;
    public AdminService( IDbConnectionFactory db)
    {
        //this.admin = admin;
        _connectionFactory = db;

    }
    //emas
    public void AssignCabinet(Department dept)
    {
        throw new NotImplementedException();
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

        using (var connection = await _connectionFactory.CreateConnectionAsync())
        {
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