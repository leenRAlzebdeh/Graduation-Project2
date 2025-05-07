using JUSTLockers.Classes;
using Microsoft.AspNetCore.Mvc;

namespace JUSTLockers.Services;
public interface ISupervisorService : IUserActions, ILockerActions
{
    void ViewAllStudentReservations();
    void Notify();
    //done
    Task<string> ReallocationRequest(Reallocation model);
    void ViewCovenantInfo();
    void CancelStudentReservation();
    public  Task<List<Report>> TheftIssues(string filter);
    public  Task SendToAdmin(int reportId);
    void ManualReserve();
    //void ViewReportList();
    Task<List<Report>> ViewReportedIssues(int? userId);
    void UpdateReportStatus();
    void EscalateReport();
    public string UnblockStudent(int id, int? userId);
    public string BlockStudent(int id, int? userId);
    void ViewBlockList();
}
