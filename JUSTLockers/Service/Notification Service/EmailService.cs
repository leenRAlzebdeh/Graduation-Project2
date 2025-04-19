using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
namespace JUSTLockers.Services;

public interface IEmailService
{
    Task SendEmailAsync(string recipient, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task SendEmailAsync(string recipient, string subject, string body)
    {
        var email = _config.GetValue<string>("Email:Email");
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Sender email is not configured.");
        }

        var password = _config.GetValue<string>("Email:Password");
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Email password is not configured.");
        }

        var host = _config.GetValue<string>("Email:Host");
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        var port = _config.GetValue<int>("Email:Port");
        if (port <= 0)
        {
            throw new InvalidOperationException("SMTP port is not configured or invalid.");
        }

        using var smtpClient = new SmtpClient(host, port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(email, password)
        };

        using var message = new MailMessage(email, recipient, subject, body);
        await smtpClient.SendMailAsync(message);
    }
}
