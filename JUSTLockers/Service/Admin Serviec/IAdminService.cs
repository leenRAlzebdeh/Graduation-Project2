using JUSTLockers.Classes;
namespace JUSTLockers.Services;

public interface IAdminService : IUserActions
{
    void RespondReallocation(string respond);
    void NotifyStudents();
    public Task<string> DeleteCovenant(Supervisor supervisor);
    public string AssignCabinet(Cabinet model);
    void SignCabinetToNewSupervisour();
    public Task<string> AssignCovenant(Supervisor supervisor, string departmentName);
    Task<List<Supervisor>> ViewSupervisorInfo(string filter = "All");
    void ViewAllCabinetsInfo();
    Task<List<Report>> ViewForwardedReports();
    public Task<Supervisor> GetSupervisorById(int supervisorId);
    void RespondForwardedReport(string respond);
}
