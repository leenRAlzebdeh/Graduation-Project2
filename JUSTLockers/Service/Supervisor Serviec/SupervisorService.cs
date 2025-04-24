using JUSTLockers.Classes;
using JUSTLockers.DataBase;
using MailKit.Search;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.ComponentModel;
using System.Data;
using Dapper;
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

        public void ViewAllReports()
        {
            throw new NotImplementedException();
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
    }
