using JUSTLockers.Classes;
using JUSTLockers.Classes.Helper;
using JUSTLockers.Service;

namespace JUSTLockers.Services;
public interface IStudentService 
{

    public bool HasLocker(int? userId);

    Task<bool> SaveReportAsync(int ReportID,int reporterId, string LockerId, string problemType,string Subject, string description, IFormFile imageFile);

    public  Task DeleteReport(int reportId);
    public Task<List<Report>> ViewAllReports(int? studentId);

   // public Task<bool> ReserveLocker(int studentId, string lockerId);
    public Task<bool> CancelReservation(int studentId, string status);
    public Task<DepartmentInfo> GetDepartmentInfo(int studentId);
    public Task<List<WingInfo>> GetAvailableWingsAndLevels(string departmentName, string Location);
    public Task<string> ReserveLockerInWingAndLevel(int studentId, string departmentName, string location, string wing, int level);
    public  Task<bool> IsStudentBlocked(int studentId);
   public Task<Reservation> GetCurrentReservationAsync(int studentId);
    public Task<FilterOptions> GetFilterOptions();
    public  Task<(string email, Report report)> GetReportByAsync(int reportId);
    public Task<List<WingInfo>> GetAllAvailableLockerCounts(string location = null, string department = null, string wing = null, int? level = null);
    public string GetSemesterSettings();
}
