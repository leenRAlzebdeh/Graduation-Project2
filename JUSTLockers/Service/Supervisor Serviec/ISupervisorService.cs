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
    void ManualReserve();
    //void ViewReportList();
    Task<List<Report>> ViewReportedIssues();
    void UpdateReportStatus();
    void EscalateReport();
    void BlockStudent();
    void UnblockStudent();
    void ViewBlockList();
}
