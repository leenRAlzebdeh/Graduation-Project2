using System.Threading.Tasks;
using JUSTLockers.Services;
using Microsoft.AspNetCore.Mvc;
using JUSTLockers.Classes;

namespace JUSTLockers.Controllers
{
    public class NotificationController : Controller
    {
        private readonly NotificationService _notificationService;
        private readonly AdminService _adminService;

        public NotificationController(NotificationService notificationService, AdminService adminService)
        {
            _notificationService = notificationService;
            _adminService = adminService;
        }

        // Helper method to send covenant assignment notifications
        public async Task SendCovenantAssignmentNotification(int supervisorId, string departmentName)
        {
            // Get supervisor details
            var supervisor = await _adminService.GetSupervisorById(supervisorId);
            if (supervisor == null) return;

            // Get department details
            var departments = await _adminService.GetDepartments();
            var department = departments.Find(d => d.Name == departmentName);
            if (department == null) return;

            // Prepare notification message
            string subject = "Covenant Assignment Update";
            string message = $"Dear {supervisor.Name},\n\n" +
                            $"Your covenant assignment has been updated.\n" +
                            $"You are now responsible for department: {department.Name}\n" +
                            $"Location: {department.Location}\n\n" +
                            $"Regards,\nJUST Lockers Administration";

            // Send email notification using existing method
            await _notificationService.SendEmailNotificationAsync(
                supervisor.Email,
                subject,
                message
            );

            // Also store the notification in the database using existing method
            //await _notificationService.SendNotificationAsync(
            //    supervisorId.ToString(),
            //    $"Covenant assigned: {department.Name} at {department.Location}"
            //);
        }

        // Helper method to send covenant removal notifications
        public async Task SendCovenantRemovalNotification(int supervisorId)
        {
            // Get supervisor details
            var supervisor = await _adminService.GetSupervisorById(supervisorId);
            if (supervisor == null) return;

            // Prepare notification message
            string subject = "Covenant Removal Notification";
            string message = $"Dear {supervisor.Name},\n\n" +
                            $"Your covenant assignment has been removed.\n" +
                            $"You are no longer responsible for any department.\n\n" +
                            $"Regards,\nJUST Lockers Administration";

            // Send email notification using existing method
            await _notificationService.SendEmailNotificationAsync(
                supervisor.Email,
                subject,
                message
            );

            // Also store the notification in the database using existing method
            //await _notificationService.SendNotificationAsync(
            //    supervisorId.ToString(),
            //    "Covenant assignment removed"
            //);
        }
    }
}