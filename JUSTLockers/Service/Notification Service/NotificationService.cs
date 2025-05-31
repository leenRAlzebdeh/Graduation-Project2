namespace JUSTLockers.Services;

using JUSTLockers.Classes;

public class NotificationService
{
    
    private readonly IEmailService _emailService;
    private readonly AdminService _adminService;
    public NotificationService( IEmailService emailService,AdminService admin)
    {
        _emailService = emailService;
        _adminService = admin;
        
    }

    public async void SendSupervisorEmail(int supervisorId, Supervisor? supervisor, EmailMessageType type, int? requestId)
    {
        if (supervisor == null)
        {
            supervisor = await _adminService.GetSupervisorById(supervisorId);
            if (supervisor == null)
            {
                return; // Supervisor not found, exit early
            }
        }
        
        var emailData = new Dictionary<string, string>
            {
                { "Name", supervisor.Name },
                { "Department", supervisor.SupervisedDepartment.Name },
                { "Location", supervisor.SupervisedDepartment.Location },
                { "RequestId", requestId?.ToString() ?? "" },
                { "Reason" ,"Contact with the Admin for more information" }

            };
        await _emailService.SendForNotificationAsync(supervisor.Email, type, emailData);
    }
    public async void SendSupervisorReallocationEmail(int supervisorId, Supervisor? supervisor, EmailMessageType type, int? requestId, Reallocation reallocation)
    {
        if (supervisor == null)
        {
            supervisor = await _adminService.GetSupervisorById(supervisorId);
            if (supervisor == null)
            {
                return; // Supervisor not found, exit early
            }
        }

        var emailData = new Dictionary<string, string>
            {
                { "Name", supervisor.Name },
                { "Department", supervisor.SupervisedDepartment.Name },
                { "Location", supervisor.SupervisedDepartment.Location },
                { "RequestId", requestId?.ToString() ?? "" },
                { "Reason" ,"There is some data that have to be solved first" },
                { "NewCabinetId", reallocation.NewCabinetID ?? "N/A" },
                { "CurrentDepartment", reallocation.CurrentDepartment ?? "N/A" },
                { "RequestWing", reallocation.RequestWing ?? "N/A" },
                { "RequestLevel", reallocation.RequestLevel?.ToString() ?? "N/A" },


            };
        await _emailService.SendForNotificationAsync(supervisor.Email, type, emailData);
    }

    public async void SendStudentReallocationEmail(List<string> student, EmailMessageType type, Reallocation reallocation)
    {
        var emailData = new Dictionary<string, string>
            {
                { "NewCabinetId", reallocation.NewCabinetID ?? "N/A" },
                { "RequestedDepartment", reallocation.RequestedDepartment ?? "N/A" },
                { "RequestLocation", reallocation.RequestLocation ?? "N/A" },
                { "CurrentCabinetId", reallocation.CurrentCabinetID ?? "N/A" },
                { "RequestWing", reallocation.RequestWing ?? "N/A" },
                { "RequestLevel", reallocation.RequestLevel?.ToString() ?? "N/A" },
            };
        await _emailService.SendStudentsNotificationAsync(student, type, emailData);
    }

    public async void SendStudentEmail(string studentEmail, EmailMessageType type, string message)
    {
        var emailData = new Dictionary<string, string>
            {
                { "Name", "" },
                { "Message", message }
            };
        await _emailService.SendForNotificationAsync(studentEmail, type, emailData);
    }

    public async void SendStudentsEmail(List<string> student, CabinetStatus type, dynamic cabinet)
    {
        var emailData = new Dictionary<string, string>
            {
                { "CabinetId", cabinet.cabinet_id ??"Your cabinet" },
                {"Department",cabinet.department_name?? "Your cabinet Department"},
                { "Location", cabinet.location ??"Your cabinet location"},          
            };

        
        await _emailService.SendStudentsCabinetNotificationAsync(student, type, emailData);
    }

    public async void SendUpdatedReportStudentEmail(string studentEmail, ReportStatus type,Report report)
    {
        var emailData = new Dictionary<string, string>
            {
                { "ReportId", report.ReportId.ToString() },
                { "ResolutionDetails", report.ResolutionDetails??"You can see your problem details in the system" }
            };
        await _emailService.UpdateStudentRepositoryAsync(studentEmail, type, emailData);
    }
}
