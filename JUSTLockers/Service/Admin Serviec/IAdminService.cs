using JUSTLockers.Classes;
using Microsoft.AspNetCore.Mvc;
namespace JUSTLockers.Services;

public interface IAdminService 
{

    public  Task<bool> RejectRequestReallocation(int requestId);
    Task<bool> ApproveRequestReallocation(int requestId);
   // public Task<(bool Success, string Message)> ApproveRequestReallocation(int requestId);
    //done
    public  Task<List<Cabinet>> ViewCabinetInfo(string? searchCab = null,string ? location = null, int? level = null, string? department = null, string? status = null, string? wing = null);
    //done


    public Task<(bool Success, string Message)> AddSupervisor(Supervisor supervisor);
    public Task<string> DeleteSupervisor(int supervisorId);
    public Task<bool> CheckEmployeeExists(int employeeId);
    public Task<bool> SupervisorExists(int supervisorId);
    Task<List<Reallocation>> ReallocationResponse();
    //done
    public Task<string> DeleteCovenant(int supervisorId);
    //done
    public string AssignCabinet(Cabinet model);

    //done
    public Task<string> AssignCovenant(int supervisorId, string departmentName, string location);

 
    public Task<List<Supervisor>> ViewAllSupervisorInfo();
    //done
    Task<List<Report>> ViewForwardedReports();
    //done
    public Task<Supervisor> GetSupervisorById(int supervisorId);
    
    //done
    public Task<List<Department>> GetDepartments();
    public Task<List<Department>> GetDepartmentsByLocation(string location);
    //done
    public Task<bool> SolveReport(int reportId, string? resolutionDetails);
    //done
    public Task<bool> RejectReport(int reportId);
    //done
    public Task<Report> GetReportDetails(int reportId);
    //done 
    public Task<bool> ReviewReport(int reportId);
    public Task<Reallocation> GetReallocationRequestById(int requestId);
    }
