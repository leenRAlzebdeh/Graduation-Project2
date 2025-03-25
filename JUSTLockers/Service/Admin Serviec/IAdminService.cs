using JUSTLockers.Classes;
namespace JUSTLockers.Services;

public interface IAdminService : IUserActions
{
    void RespondReallocation(string respond);
    void NotifyStudents();
    public Task<string> DeleteCovenant(int supervisorId);
    public string AssignCabinet(Cabinet model);
    void SignCabinetToNewSupervisour();
    public Task<string> AssignCovenant(int supervisorId, string departmentName);
    public Task<List<Supervisor>> ViewAllSupervisorInfo();
    void ViewAllCabinetsInfo();
    Task<List<Report>> ViewForwardedReports();
    public Task<Supervisor> GetSupervisorById(int supervisorId);
    void RespondForwardedReport(string respond);
    public Task<List<Department>> GetDepartments();
}
