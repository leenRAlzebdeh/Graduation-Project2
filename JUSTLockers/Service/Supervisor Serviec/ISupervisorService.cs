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
    public  Task<List<Report>> TheftIssues(string filter);
    public  Task SendToAdmin(int reportId);
    void ManualReserve();
    //void ViewReportList();
    //done
    Task<List<Report>> ViewReportedIssues(int? userId);


    //done
    public string UnblockStudent(int id, int? userId);
    //done
    public string BlockStudent(int id, int? userId);
    Task<List<BlockedStudent>> BlockedStudents();
    public bool IsStudentBlocked(int id);
    public Student GetStudentById(int id);


}
