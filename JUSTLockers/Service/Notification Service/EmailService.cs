using JUSTLockers.Classes;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Org.BouncyCastle.Cms;
namespace JUSTLockers.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string recipient, string subject, string body);
        Task SendEmailToManyAsync(List<string> recipients, string subject, string body);
        public Task SendStudentsNotificationAsync(List<string> recipients, EmailMessageType messageType, Dictionary<string, string>? data = null);
       // public Task SendStudentNotificationAsync(string recipients, EmailMessageType messageType, Dictionary<string, string>? data = null);

        Task SendForNotificationAsync(string recipient, EmailMessageType messageType, Dictionary<string, string>? data = null);
        Task SendStudentsCabinetNotificationAsync(List<string> recipients, CabinetStatus messageType, Dictionary<string, string>? data = null);
        Task UpdateStudentRepositoryAsync(string recipient, ReportStatus type, Dictionary<string, string>? data = null);
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
            if (recipient==null)
                return;
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
            if (recipients.Count == 1)
            {
                await SendEmailAsync(recipients[0], subject, body);
                return;
            }
            else if (recipients.Count == 0)
                return;
        
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
        
        public async Task SendForNotificationAsync(string recipient, EmailMessageType messageType, Dictionary<string, string>? data = null)
        {
            (string subject, string body) = GenerateEmailContent(messageType, data);
            Console.WriteLine( subject+body );
            await SendEmailAsync(recipient, subject, body);
        }

        public async Task SendStudentsNotificationAsync(List<string> recipients, EmailMessageType messageType, Dictionary<string, string>? data = null)
        {
            (string subject, string body) = GenerateEmailContent(messageType, data);
            Console.WriteLine(subject + body);
            await SendEmailToManyAsync(recipients, subject, body);
        }

        public async Task SendStudentsCabinetNotificationAsync(List<string> recipients, CabinetStatus messageType, Dictionary<string, string>? data = null)
        {
            (string subject, string body) = GenerateEmailContent(messageType, data);
            Console.WriteLine(subject + body);
            await SendEmailToManyAsync(recipients, subject, body);
        }
        public Task UpdateStudentRepositoryAsync(string recipient, ReportStatus type, Dictionary<string, string>? data = null)
        {
           (string subject, string body) = GenerateEmailContent(type, data);
            Console.WriteLine(subject + body);
            return SendEmailAsync(recipient, subject, body);
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
                case EmailMessageType.StudentBlocked:
                    subject =
                        "Locker Reservation Blocked Notification";
                    body =
                        $"Dear Student,\n\n" +
                        "You have been blocked in the JUSTLockers system.\n" +
                        "You should know that you will not be able to reserve a locker until further notice.\n" +
                        "Please contact your supervisor for further details.\n\n" +
                        "Best regards,\nJUSTLockers Team";
                    break;
                case EmailMessageType.StudentUnblocked:
                    subject =
                        "Locker Reservation Unblocked Notification";
                    body =
                        $"Dear Student,\n\n" +
                        "You have been unblocked in the JUSTLockers system.\n" +
                        "You can now reserve a locker now.\n\n" +
                        "Best regards,\nJUSTLockers Team";
                    break;

                default:
                    throw new ArgumentException("Invalid email message type");
            }

            return (subject, body);
        }


        private (string subject, string body) GenerateEmailContent(CabinetStatus messageType, Dictionary<string, string>? data)
        {
            string subject = "";
            string body = "";

            switch (messageType)
            {
                case CabinetStatus.IN_SERVICE:
                    subject =
                        "Cabinet Status Update";
                    body =
                        $"Dear Student ,\n\n" +
                        $"The status of your cabinet has been updated to IN_SERVICE.\n" +
                        $"Cabinet ID: {data?.GetValueOrDefault("CabinetId") ?? "N/A"}\n" +
                        $"Department: {data?.GetValueOrDefault("Department") ?? "N/A"}\n" +
                        $"Location: {data?.GetValueOrDefault("Location") ?? "N/A"}\n\n" +
                        "You can now reserve a locker in this cabinet.\n\n" +
                        "Best regards,\nJUSTLockers Team";
                    break;
                case CabinetStatus.IN_MAINTENANCE:
                    subject =
                        "Cabinet Status Update";
                    body =
                        $"Dear Student ,\n\n" +
                        $"The status of your cabinet has been updated to IN_MAINTENANCE.\n" +
                        $"Cabinet ID: {data?.GetValueOrDefault("CabinetId") ?? "N/A"}\n" +
                        $"Department: {data?.GetValueOrDefault("Department") ?? "N/A"}\n" +
                        $"Location: {data?.GetValueOrDefault("Location") ?? "N/A"}\n\n" +
                        "You will not be able to reserve a locker in this cabinet until further notice.\n\n" +
                        "Please Know that you have 5 days to get your stuff out of the cabinet.\n\n" +
                        "You can reserve a locker in another cabinet.\n\n" +
                        "Best regards,\nJUSTLockers Team";
                    break;
                case CabinetStatus.OUT_OF_SERVICE:
                    subject =
                        "Cabinet Status Update";
                    body =
                        $"Dear Student ,\n\n" +
                        $"The status of your cabinet has been updated to OUT_OF_SERVICE.\n" +
                        $"Cabinet ID: {data?.GetValueOrDefault("CabinetId") ?? "N/A"}\n" +
                        $"Department: {data?.GetValueOrDefault("Department") ?? "N/A"}\n" +
                        $"Location: {data?.GetValueOrDefault("Location") ?? "N/A"}\n\n" +
                        "You will not be able to reserve a locker in this cabinet ,\n\n" +
                        "Please Know that you have 5 days to get your stuff out of the cabinet.\n\n" +
                        "You can reserve a locker in another cabinet.\n\n" +
                        "Best regards,\nJUSTLockers Team";
                    break;
                case CabinetStatus.DAMAGED:
                    subject =
                        "Cabinet Status Update";
                    body =
                        $"Dear Student,\n\n" +
                        $"The status of your cabinet has been updated to DAMAGED.\n" +
                        $"Cabinet ID: {data?.GetValueOrDefault("CabinetId") ?? "N/A"}\n" +
                        $"Department: {data?.GetValueOrDefault("Department") ?? "N/A"}\n" +
                        $"Location: {data?.GetValueOrDefault("Location") ?? "N/A"}\n\n" +
                        "You will not be able to reserve a locker in this cabinet until further notice.\n\n" +
                        "Please Know that you have 5 days to get your stuff out of the cabinet.\n\n" +
                        "You can reserve a locker in another cabinet.\n\n" +
                        "Best regards,\nJUSTLockers Team";
                    break;
                default:
                    throw new ArgumentException("Invalid email message type");
            }
            return (subject, body);
        }

        private (string subject, string body) GenerateEmailContent(ReportStatus messageType, Dictionary<string, string>? data)
        {
            string subject = "";
            string body = "";
            

            switch (messageType)
            {
                case ReportStatus.REPORTED:
                    subject = "Report Submitted";
                    body = $"Dear Student,\n\n" +
                           "Your report has been successfully submitted.\n" +
                           $"Report ID: {data?.GetValueOrDefault("ReportId") ?? "N/A"}\n" +
                           "We will review it and get back to you soon.\n\n" +
                           "Best regards,\nJUSTLockers Team";
                    break;
                case ReportStatus.IN_REVIEW:
                    subject = "Report Under Review";
                    body = $"Dear Student,\n\n" +
                           "Your report is currently under review.\n" +
                           $"Report ID: {data?.GetValueOrDefault("ReportId") ?? "N/A"}\n" +
                           "We will update you once the review is complete.\n\n" +
                           "Best regards,\nJUSTLockers Team";
                    break;
                case ReportStatus.ESCALATED:
                    subject = "Report Escalated ";
                    body = $"Dear Student,\n\n" +
                           "Your report has been escalated to the Admin for further review.\n" +
                           $"Report ID: {data?.GetValueOrDefault("ReportId") ?? "N/A"}\n" +
                           "We will keep you updated on the progress.\n\n" +
                           "Best regards,\nJUSTLockers Team";
                    break;
                case ReportStatus.RESOLVED:
                    subject = "Report Resolved";
                    body = $"Dear Student,\n\n" +
                           "Your report has been resolved.\n" +
                           $"Report ID: {data?.GetValueOrDefault("ReportId") ?? "N/A"}\n" +
                           $"Resolution Details:{ data?.GetValueOrDefault("ResolutionDetails") ?? "N/A"}\n"+
                           "You can check the system for details.\n\n" +
                           "Best regards,\nJUSTLockers Team";
                    break;
                case ReportStatus.REJECTED:
                    subject = "Report Rejected";
                    body = $"Dear Student,\n\n" +
                           "Your report has been rejected.\n" +
                           $"Report ID: {data?.GetValueOrDefault("ReportId") ?? "N/A"}\n" +
                           $"Resolution Details:{data?.GetValueOrDefault("ResolutionDetails") ?? "N/A"}\n" +
                           "You can contact the Supervisor for further details.\n\n" +
                           "Best regards,\nJUSTLockers Team";
                    break;
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
        StudentReallocation,
        StudentBlocked,
        StudentUnblocked,
    }


  


}