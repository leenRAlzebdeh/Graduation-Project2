namespace JUSTLockers.Services;
public interface IStudentService : IUserActions, ILockerActions
{
    void Login();
    void ViewAvailableLockers();
    void ReserveLocker();
    void ViewReservationInfo();
    void ReportProblem();
    void DeleteReport();
    void ViewAllReports();
    void CheckReportStatus();
    void RemoveReservation();
}
