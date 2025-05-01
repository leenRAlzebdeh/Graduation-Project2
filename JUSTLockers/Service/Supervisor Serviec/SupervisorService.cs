using JUSTLockers.Classes;
using JUSTLockers.DataBase;
using MailKit.Search;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.ComponentModel;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
namespace JUSTLockers.Services;


public class SupervisorService : ISupervisorService
    {
        private readonly IConfiguration _configuration;

        public SupervisorService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void Login()
        {
            throw new NotImplementedException();
        }

        public Task<List<Locker>> ViewAvailableLockers(string departmentName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ReserveLocker(int studentId, string lockerId)
        {
            throw new NotImplementedException();
        }

        public void ViewReservationInfo()
        {
            throw new NotImplementedException();
        }

        public void ReportProblem()
        {
            throw new NotImplementedException();
        }

        public void DeleteReport()
        {
            throw new NotImplementedException();
        }


    //public async Task<List<Report>> ViewReportedIssues()
    //{
    //    var reports = new List<Report>();
    //    var query = "SELECT * FROM Reports";

    //    using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
    //    {
    //        await connection.OpenAsync();

    //        using (var command = new MySqlCommand(query, connection))
    //        using (var reader = await command.ExecuteReaderAsync())
    //        {
    //            while (await reader.ReadAsync())
    //            {
    //                reports.Add(new Report
    //                {
    //                    Reporter = null,
    //                    Locker = null,

    //                    ReportId = reader.GetInt32("Id"),
    //                    ReporterId = reader.GetInt32("ReporterId"),
    //                    LockerId = reader.GetString("LockerId"),

    //                    // Converting string from DB to enums
    //                    Type = Enum.Parse<ReportType>(reader.GetString("Type")),
    //                    Status = Enum.Parse<ReportStatus>(reader.GetString("Status")),

    //                    Subject = reader.GetString("Subject"),
    //                    Statement = reader.GetString("Statement"),
    //                    ReportDate = reader.GetDateTime("ReportDate"),
    //                    ResolvedDate = reader.IsDBNull("ResolvedDate") ? null : reader.GetDateTime("ResolvedDate"),
    //                    ResolutionDetails = reader.IsDBNull("ResolvedDetails") ? null : reader.GetString("ResolvedDetails"),
    //                    ImageData = reader.IsDBNull("ImageData") ? null : (byte[])reader["ImageData"],
    //                    ImageMimeType = reader.IsDBNull("ImageMimeType") ? null : reader.GetString("ImageMimeType")
    //                });
    //            }
    //        }
    //    }

    //    return reports;
    //}
    public async Task<List<Report>> ViewReportedIssues()
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
                r.ImageData AS ImageData,
                r.ImageMimeType AS ImageMimeType,
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
          ";

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
                        ImageData = reader.IsDBNull(reader.GetOrdinal("ImageData")) ? null : (byte[])reader["ImageData"],
                        ImageMimeType = reader.IsDBNull(reader.GetOrdinal("ImageMimeType")) ? null : reader.GetString("ImageMimeType")
                    });
                }
            }
        }
        return reports;
    }


    public void CheckReportStatus()
        {
            throw new NotImplementedException();
        }

        public void RemoveReservation()
        {
            throw new NotImplementedException();
        }

        public void ViewAllStudentReservations()
        {
            throw new NotImplementedException();
        }

        public void Notify()
        {
            throw new NotImplementedException();
        }
    //here 

    public async Task<string> ReallocationRequest(Reallocation model)
    {
        try
        {
            string query = @"INSERT INTO Reallocation 
                         
                         (SupervisorID, CurrentDepartment, RequestedDepartment, 
                          RequestLocation, CurrentLocation, RequestWing, RequestLevel, 
                          number_cab, CurrentCabinetID) 
                         VALUES 
                         (@SupervisorID, @CurrentDepartment, @RequestedDepartment, 
                          @RequestLocation,@CurrentLocation,@RequestWing, @RequestLevel,
                          @NumberCab, @CurrentCabinetID)";

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SupervisorID", model.SupervisorID);
                    command.Parameters.AddWithValue("@CurrentDepartment", model.CurrentDepartment ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@RequestedDepartment", model.RequestedDepartment ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CurrentLocation", model.CurrentLocation ?? (object)DBNull.Value);
                    //command.Parameters.AddWithValue("@RequestStatus", model.RequestStatus?.ToString() ?? "PENDING");
                    command.Parameters.AddWithValue("@RequestLocation", model.RequestLocation ?? (object)DBNull.Value);
                   // command.Parameters.AddWithValue("@RequestDate", model.RequestDate ?? DateTime.Now);
                    command.Parameters.AddWithValue("@RequestWing", model.RequestWing ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@RequestLevel", model.RequestLevel);
                    command.Parameters.AddWithValue("@NumberCab", model.NumberCab);
                    command.Parameters.AddWithValue("@CurrentCabinetID", model.CurrentCabinetID ?? (object)DBNull.Value);
                   // command.Parameters.AddWithValue("@NewCabinetID", model.NewCabinetID ?? (object)DBNull.Value);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    return rowsAffected > 0 ? "Request sent successfully! Wait Admin Response" : "Failed to send request.";
                }
            }
        }
        catch (Exception ex)
        {
            return $"Error sending request: {ex.Message}";
        }
    }


    public void ViewCovenantInfo()
        {
            throw new NotImplementedException();
        }

        public void CancelStudentReservation()
        {
            throw new NotImplementedException();
        }

        public void ManualReserve()
        {
            throw new NotImplementedException();
        }

        public void ViewReportList()
        {
            throw new NotImplementedException();
        }

        public void UpdateReportStatus()
        {
            throw new NotImplementedException();
        }

        public void EscalateReport()
        {
            throw new NotImplementedException();
        }

        public void BlockStudent()
        {
            throw new NotImplementedException();
        }

        public void UnblockStudent()
        {
            throw new NotImplementedException();
        }

        public void ViewBlockList()
        {
            throw new NotImplementedException();
        }

        public void ViewNotifications()
        {
            throw new NotImplementedException();
        }

    public Task<bool> CancelReservation(int studentId, string reservationId)
        {
            throw new NotImplementedException();
        }

    public Task<bool> CancelReservation(int studentId)
    {
        throw new NotImplementedException();
    }
}
