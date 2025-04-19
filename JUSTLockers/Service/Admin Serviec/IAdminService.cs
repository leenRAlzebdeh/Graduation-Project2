using JUSTLockers.Classes;
using Microsoft.AspNetCore.Mvc;
namespace JUSTLockers.Services;

public interface IAdminService : IUserActions
{
    //done
    public  Task<List<Cabinet>> ViewCabinetInfo(string? searchCab = null,string ? location = null, int? level = null, string? department = null, string? status = null, string? wing = null);
    //done
    public string AddSupervisor(Supervisor supervisor);
    void RespondReallocation(string respond);
    void NotifyStudents();
    //done
    public Task<string> DeleteCovenant(int supervisorId);
    //done
    public string AssignCabinet(Cabinet model);

    void SignCabinetToNewSupervisour();
    //done
    public Task<string> AssignCovenant(int supervisorId, string departmentName, string location);

   // public  Task<List<Cabinet>> ViewCabinetInfo();
   //done
    public Task<List<Supervisor>> ViewAllSupervisorInfo();
    //done
    Task<List<Report>> ViewForwardedReports();
    //done
    public Task<Supervisor> GetSupervisorById(int supervisorId);
    void RespondForwardedReport(string respond);
    //done
    public Task<List<Department>> GetDepartments();
    //done
    public Task<bool> ResolveReport(int reportId, string? resolutionDetails);
    //done
    public Task<bool> RejectReport(int reportId);
    //done
    public Task<Report> GetReportDetails(int reportId);
    //done 
    public Task<bool> ReviewReport(int reportId);

    }
