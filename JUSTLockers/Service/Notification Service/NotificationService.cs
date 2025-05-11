namespace JUSTLockers.Services;
using JUSTLockers.DataBase;
public class NotificationService
{
    private readonly DbConnectionFactory _context;
    private readonly IEmailService _emailService;

    public NotificationService(DbConnectionFactory context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // Store notification in the database
    public async Task SendNotificationAsync(string userId, string message)
    {
        // var notification = new Notification
        // {
        //     UserId = userId,
        //     Message = message
        // };

        // _context.Notifications.Add(notification);
       // await _context.SaveChangesAsync();
    }

    // Send email notification
    public async Task SendEmailNotificationAsync(string email, string subject, string message)
    {
        await _emailService.SendEmailAsync(email, subject, message);
    }
}
