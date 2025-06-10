using JUSTLockers.Services;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using JUSTLockers.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JUSTLockers.Service
{
    public class SemesterEndService: BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly AdminService _adminService;
        private readonly IEmailService _emailService;
        public SemesterEndService(IConfiguration configuration, AdminService adminService, IEmailService emailService)
        {
            _configuration = configuration;
            _adminService = adminService;
            _emailService = emailService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var settings = await _adminService.GetSemesterSettings();
                    if (settings != null)
                    {
                        var endDate = settings.GetType().GetProperty("SemesterEndDate")?.GetValue(settings) as DateTime?;
                        Console.WriteLine(endDate);
                        var users = await _adminService.GetAllStudentsEmails();
                        if (endDate.HasValue)
                        {
                            var now = DateTime.Now;
                            var daysUntilEnd = (endDate.Value - now).TotalDays;

                            // Send notifications 14 days before scheduled end
                            if (daysUntilEnd <= 14 && daysUntilEnd>0)
                            {
                                    await _emailService.SemesterEndNotificationAsync(
                                        users, "StudentSemesterEndNotification",new Dictionary<string, DateTime>
                                        {
                                            { "EndDate", endDate.Value }
                                        }
                                    );
                                
                            }

                            // Handle semester end
                            if (now >= endDate.Value)
                            {
                                // Send final notification

                                await _emailService.SemesterEndNotificationAsync(
                                    users, "StudentSemesterEndFinalNotification", new Dictionary<string, DateTime>
                                    {
                                            { "EndDate", endDate.Value }
                                    }
                                );


                                // Clear reservations and reports
                               var clreaed= await _adminService.ClearReservationsAndReports();
                                if(clreaed)
                                    Console.WriteLine("Reservation is clean now");
                                // Clear semester settings
                                using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                                await connection.OpenAsync();
                                var query = "DELETE FROM SemesterSettings WHERE Id = (SELECT MAX(Id) FROM SemesterSettings)";
                                using var command = new MySqlCommand(query, connection);
                                await command.ExecuteNonQueryAsync();
                                //How shutdown the application ? 
                                // Environment.Exit(0); // Uncomment to stop the application after clearing settings

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error (implement logging as needed)
                    Console.WriteLine($"SemesterEndService error: {ex.Message}");
                }

                // Check every 24 hours
                 await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

            }
        }
    }
}

