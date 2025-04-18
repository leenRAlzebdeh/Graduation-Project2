using JUSTLockers.Classes;
using Microsoft.AspNetCore.Mvc;
namespace JUSTLockers.Services;

public interface IAdminService : IUserActions
{
    public  Task<List<Cabinet>> ViewCabinetInfo(string? searchCab = null,string ? location = null, int? level = null, string? department = null, string? status = null, string? wing = null);

    public string AddSupervisor(Supervisor supervisor);
    void RespondReallocation(string respond);
    void NotifyStudents();
    public Task<string> DeleteCovenant(int supervisorId);
    public string AssignCabinet(Cabinet model);
    void SignCabinetToNewSupervisour();
    public Task<string> AssignCovenant(int supervisorId, string departmentName);

   // public  Task<List<Cabinet>> ViewCabinetInfo();
    public Task<List<Supervisor>> ViewAllSupervisorInfo();
    void ViewAllCabinetsInfo();
    Task<List<Report>> ViewForwardedReports();
    public Task<Supervisor> GetSupervisorById(int supervisorId);
    void RespondForwardedReport(string respond);
    public Task<List<Department>> GetDepartments();
    public  Task<bool> ResolveReport(int reportId, string? resolutionDetails);
    public Task<bool> RejectReport(int reportId);
    public Task<Report> GetReportDetails(int reportId);


    }
