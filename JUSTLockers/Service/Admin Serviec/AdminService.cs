using JUSTLockers.Classes;

namespace JUSTLockers.Services;

public class AdminService : IAdminService
{
    private readonly Admin admin;

    public AdminService(Admin admin)
    {
        this.admin = admin;
    }
    //emas
    public void AssignCabinet(Department dept)
    {
        throw new NotImplementedException();
    }

    public void AssignCovenant(Supervisor supervisor)
    {
        throw new NotImplementedException();
    }

    public void DeleteCovenant(Supervisor supervisor)
    {
        throw new NotImplementedException();
    }
    //emas 
    public void Login()
    {
        throw new NotImplementedException();
    }

    public void NotifyStudents()
    {
        throw new NotImplementedException();
    }

    public void RespondForwardedReport(string respond)
    {
        throw new NotImplementedException();
    }

    public void RespondReallocation(string respond)
    {
        throw new NotImplementedException();
    }

    public void SignCabinet()
    {
        throw new NotImplementedException();
    }

    public void ViewAllCabinetsInfo()
    {
        throw new NotImplementedException();
    }

    public void ViewForwardedReports()
    {
        throw new NotImplementedException();
    }

    public void ViewNotifications()
    {
        throw new NotImplementedException();
    }

    public void ViewSupervisorInfo(Supervisor supervisor)
    {
        throw new NotImplementedException();
    }
}