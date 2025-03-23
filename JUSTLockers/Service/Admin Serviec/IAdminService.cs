using JUSTLockers.Classes;
namespace JUSTLockers.Services;

public interface IAdminService : IUserActions
{
    void RespondReallocation(string respond);
    void NotifyStudents();
    void DeleteCovenant(Supervisor supervisor);
    public string AssignCabinet(Cabinet model);
    void SignCabinet();
    void AssignCovenant(Supervisor supervisor);
    void ViewSupervisorInfo(Supervisor supervisor);
    void ViewAllCabinetsInfo();
    Task<List<Report>> ViewForwardedReports();
    void RespondForwardedReport(string respond);
}
