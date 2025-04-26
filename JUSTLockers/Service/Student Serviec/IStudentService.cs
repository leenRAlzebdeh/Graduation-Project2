using JUSTLockers.Classes;

namespace JUSTLockers.Services;
public interface IStudentService : IUserActions, ILockerActions
{
    //done
    void Login();
   
    public Task<Reservation> ViewReservationInfo(int studentId);

    //void ReportProblem();
    Task<bool> SaveReportAsync(int ReportID,int reporterId, string LockerId, string problemType,string Subject, string description, IFormFile imageFile);

    void DeleteReport();
    void ViewAllReports();
    void CheckReportStatus();
  
}
