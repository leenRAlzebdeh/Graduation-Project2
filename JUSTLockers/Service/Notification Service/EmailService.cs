//using MailKit.Net.Smtp;
//using MailKit.Security;
//using MimeKit;
//using Microsoft.Extensions.Configuration;

//namespace JUSTLockers.Services;

//public interface IEmailService
//{
//    Task SendEmailAsync(string recipient, string subject, string body);
//    Task SendEmailToManyAsync(List<string> recipients, string subject, string body);
//}
//public class EmailService : IEmailService
//{
//    private readonly IConfiguration _config;

//    public EmailService(IConfiguration config)
//    {
//        _config = config;
//    }

//    public async Task SendEmailAsync(string recipient, string subject, string body)
//    {
//        var email = _config["Email:Email"];
//        var password = _config["Email:Password"];
//        var host = _config["Email:Host"];
//        var port = _config.GetValue<int>("Email:Port");

//        var message = new MimeMessage();
//        message.From.Add(MailboxAddress.Parse(email));
//        message.To.Add(MailboxAddress.Parse(recipient));
//        message.Subject = subject;
//        message.Body = new TextPart("plain") { Text = body };

//        using var smtp = new SmtpClient();
//        await smtp.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
//        await smtp.AuthenticateAsync(email, password);
//        await smtp.SendAsync(message);
//        await smtp.DisconnectAsync(true);
//    }

//    public async Task SendEmailToManyAsync(List<string> recipients, string subject, string body)
//    {
//        var email = _config["Email:Email"];
//        var password = _config["Email:Password"];
//        var host = _config["Email:Host"];
//        var port = _config.GetValue<int>("Email:Port");

//        var message = new MimeMessage();
//        message.From.Add(MailboxAddress.Parse(email));
//        message.Subject = subject;
//        message.Body = new TextPart("plain") { Text = body };

//        // 🔹 Add multiple recipients
//        foreach (var recipient in recipients)
//        {
//            message.To.Add(MailboxAddress.Parse(recipient));
//        }

//        using var smtp = new SmtpClient();
//        await smtp.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect); 
//        await smtp.AuthenticateAsync(email, password);
//        await smtp.SendAsync(message);
//        await smtp.DisconnectAsync(true);
//    }



//}


using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
namespace JUSTLockers.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string recipient, string subject, string body);
        Task SendEmailToManyAsync(List<string> recipients, string subject, string body);
        public Task SendStudentNotificationAsync(List<string> recipients, EmailMessageType messageType, Dictionary<string, string>? data = null);
        Task SendSupervisorNotificationAsync(string recipient, EmailMessageType messageType, Dictionary<string, string>? data = null);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string recipient, string subject, string body)
        {
            var email = _config["Email:Email"];
            var password = _config["Email:Password"];
            var host = _config["Email:Host"];
            var port = _config.GetValue<int>("Email:Port");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(email));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync(email, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendEmailToManyAsync(List<string> recipients, string subject, string body)
        {
            var email = _config["Email:Email"];
            var password = _config["Email:Password"];
            var host = _config["Email:Host"];
            var port = _config.GetValue<int>("Email:Port");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(email));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            foreach (var recipient in recipients)
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync(email, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendSupervisorNotificationAsync(string recipient, EmailMessageType messageType, Dictionary<string, string>? data = null)
        {
            (string subject, string body) = GenerateEmailContent(messageType, data);
            Console.WriteLine( subject+body );
            await SendEmailAsync(recipient, subject, body);
        }

        public async Task SendStudentNotificationAsync(List<string> recipients, EmailMessageType messageType, Dictionary<string, string>? data = null)
        {
            (string subject, string body) = GenerateEmailContent(messageType, data);
            Console.WriteLine(subject + body);
            await SendEmailToManyAsync(recipients, subject, body);
        }

        private (string subject, string body) GenerateEmailContent(EmailMessageType messageType, Dictionary<string, string>? data)
        {
            string subject = "";
            string body = "";

            switch (messageType)
            {
                case EmailMessageType.SupervisorAdded:
                    subject = "Welcome to JUSTLockers - Supervisor Role Assigned";
                    body = $"Dear {data?.GetValueOrDefault("Name") ?? "Supervisor"},\n\n" +
                           "You have been successfully added as a supervisor in the JUSTLockers system.\n" +
                           $"Department: {data?.GetValueOrDefault("Department") ?? "N/A"}\n" +
                           $"Location: {data?.GetValueOrDefault("Location") ?? "N/A"}\n\n" +
                           "Please log in to the system to manage your responsibilities.\n\n" +
                           "Best regards,\nJUSTLockers Team";
                    break;

                case EmailMessageType.CovenantSigned:
                    subject = "Covenant Assignment Confirmation";
                    body = $"Dear {data?.GetValueOrDefault("Name") ?? "Supervisor"},\n\n" +
                           "A covenant has been assigned to you in the JUSTLockers system.\n" +
                           $"Department: {data?.GetValueOrDefault("Department") ?? "N/A"}\n" +
                           $"Location: {data?.GetValueOrDefault("Location") ?? "N/A"}\n\n" +
                           "Please review the details in the system.\n\n" +
                           "Best regards,\nJUSTLockers Team";
                    break;

                case EmailMessageType.CovenantDeleted:
                    subject = "Covenant Removal Notification";
                    body = $"Dear {data?.GetValueOrDefault("Name") ?? "Supervisor"},\n\n" +
                           $"Your covenant for Department: {data?.GetValueOrDefault("Department") ?? "N/A"} " +
                           $"Location: {data?.GetValueOrDefault("Location") ?? "N/A"} has been removed from the JUSTLockers system.\n" +
                           "If you have any questions, please contact the admin.\n\n" +
                           "Best regards,\nJUSTLockers Team";
                    break;
                case EmailMessageType.SupervisorDeleted:
                    subject = "Supervisor Role Removal Notification \n\n";
                    body = $"Dear {data?.GetValueOrDefault("Name") ?? "Supervisor"},\n\n" +
                           "Your supervisor role has been removed from the JUSTLockers system.\n" +
                           "If you have any questions, please contact the admin.\n\n" +
                           "Best regards,\nJUSTLockers Team";
                    break;

                case EmailMessageType.ReallocationApproved:
                    subject = "Reallocation Request Approved";
                    body = $"Dear {data?.GetValueOrDefault("Name") ?? "Supervisor"},\n\n" +
                           $"Your reallocation request (ID: {data?.GetValueOrDefault("RequestId") ?? "N/A"}) has been approved.\n" +
                           "Please check the JUSTLockers system for updated details.\n\n" +
                           "Best regards,\nJUSTLockers Team";
                    break;

                case EmailMessageType.ReallocationRejected:
                    subject = "Reallocation Request Rejected";
                    body = $"Dear {data?.GetValueOrDefault("Name") ?? "Supervisor"},\n\n" +
                           $"Your reallocation request (ID: {data?.GetValueOrDefault("RequestId") ?? "N/A"}) has been rejected.\n" +
                           $"Reason: {data?.GetValueOrDefault("Reason") ?? "N/A"}\n" +
                           "Please contact the admin for further details.\n\n" +
                           "Best regards,\nJUSTLockers Team";
                    break;

                case EmailMessageType.ReallocationCabinet:
                    subject = "Reallocation Cabinet Notification";
                    body =
                   $"Dear {data?.GetValueOrDefault("Name") ?? "Supervisor"},\n\n" +
                   $"There is a reallocation for a cabinet with request (ID: {data?.GetValueOrDefault("RequestId") ?? "N/A"}) that has been processed in your Department.\n" +
                   $"New Cabinet ID: {data?.GetValueOrDefault("NewCabinetId") ?? "N/A"}\n" +
                   $"From Department: {data?.GetValueOrDefault("CurrentDepartment") ?? "N/A"}\n" +
                   $"Requested Wing: {data?.GetValueOrDefault("RequestWing") ?? "N/A"}\n" +
                   $"Requested Level: {data?.GetValueOrDefault("RequestLevel") ?? "N/A"}\n\n" +
                   "Please check the JUSTLockers system for updated details.\n\n" +
                   "Best regards,\nJUSTLockers Team";
                    break;

                case EmailMessageType.StudentReallocation:
                    subject = "Locker Reallocation Notification";
                    body = $"Dear Student ,\n\n" +
                           "Your locker has been reallocated as part of a cabinet reallocation in the JUSTLockers system.\n" +
                           $"Old Cabinet ID: {data?.GetValueOrDefault("CurrentCabinetId") ?? "N/A"}\n" +
                           $"New Cabinet ID: {data?.GetValueOrDefault("NewCabinetId") ?? "N/A"}\n" +
                           $"New Department: {data?.GetValueOrDefault("RequestedDepartment") ?? "N/A"}\n" +
                           $"New Location: {data?.GetValueOrDefault("RequestLocation") ?? "N/A"}\n" +
                           $"New Wing: {data?.GetValueOrDefault("RequestWing") ?? "N/A"}\n" +
                           $"New Level: {data?.GetValueOrDefault("RequestLevel") ?? "N/A"}\n\n" +
                           "Please check the JUSTLockers system for updated details or contact your supervisor if you have any questions.\n\n" +
                           "Best regards,\nJUSTLockers Team";
                    break;
                default:
                    throw new ArgumentException("Invalid email message type");
            }

            return (subject, body);
        }

        
    }
    public enum EmailMessageType
    {
        SupervisorAdded,
        CovenantSigned,
        CovenantDeleted,
        SupervisorDeleted,
        ReallocationApproved,
        ReallocationCabinet,
        ReallocationRejected,
        StudentReallocation
    }


}