using JUSTLockers.Classes;
using JUSTLockers.Service;

namespace JUSTLockers.Services;
public interface IStudentService : IUserActions, ILockerActions
{
    //done
    void Login();
    public bool HasLocker(int? userId);
    public Task<Reservation> ViewReservationInfo(int studentId);

    //void ReportProblem();
    Task<bool> SaveReportAsync(int ReportID,int reporterId, string LockerId, string problemType,string Subject, string description, IFormFile imageFile);

    public  Task DeleteReport(int reportId);
    public Task<List<Report>> ViewAllReports(int? studentId);
    void CheckReportStatus();
    public Task<List<Locker>> ViewAvailableLockers(string departmentName);
   // public Task<bool> ReserveLocker(int studentId, string lockerId);
    public Task<bool> CancelReservation(int studentId);
    public Task<DepartmentInfo> GetDepartmentInfo(int studentId);
    public Task<List<WingInfo>> GetAvailableWingsAndLevels(string departmentName, string Location);
    public Task<string> ReserveLockerInWingAndLevel(int studentId, string departmentName, string location, string wing, int level);
    public  Task<bool> IsStudentBlocked(int studentId);
   public Task<Reservation> GetCurrentReservationAsync(int studentId);
    public Task<FilterOptions> GetFilterOptions();
    public Task<List<WingInfo>> GetAllAvailableLockerCounts(string location = null, string department = null, string wing = null, int? level = null);

}
