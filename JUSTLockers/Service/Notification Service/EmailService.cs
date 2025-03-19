using System.Net;
using System.Net.Mail;
namespace JUSTLockers.Services;
public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task SendEmailAsync(string recipient, string subject, string body)
    {
        var smtpClient = new SmtpClient(_config["Email:SMTP"])
        {
            Port = 587,
            Credentials = new NetworkCredential(_config["Email:Username"], _config["Email:Password"]),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_config["Email:Username"] ?? throw new InvalidOperationException("Email username is not configured.")),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };

        mailMessage.To.Add(recipient);

        await smtpClient.SendMailAsync(mailMessage);
    }
}
