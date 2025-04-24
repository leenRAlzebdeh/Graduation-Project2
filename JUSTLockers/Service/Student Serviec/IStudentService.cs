using JUSTLockers.Classes;

namespace JUSTLockers.Services;
public interface IStudentService : IUserActions, ILockerActions
{
    //done
    void Login();
   
    public Task<Reservation> ViewReservationInfo(int studentId);

    void ReportProblem();
    void DeleteReport();
    void ViewAllReports();
    void CheckReportStatus();
  
}
