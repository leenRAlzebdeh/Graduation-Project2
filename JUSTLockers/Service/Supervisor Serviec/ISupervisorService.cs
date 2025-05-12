using JUSTLockers.Classes;
using JUSTLockers.Service;
using Microsoft.AspNetCore.Mvc;

namespace JUSTLockers.Services;
public interface ISupervisorService : IUserActions, ILockerActions
{
        Task<List<Student>> ViewAllStudentReservations(int? userId, int? searchstu = null);
        Task<string> ReallocationRequest(Reallocation model);
        public  Task<List<Report>> TheftIssues(string filter);
        public  Task SendToAdmin(int reportId);

        Task<List<Report>> ViewReportedIssues(int? userId);

        public Task<List<Cabinet>> ViewCabinetInfo(int? userId, string? searchCab = null, string? location = null, int? level = null, string? department = null, string? status = null, string? wing = null);

        public string UnblockStudent(int id, int? userId);

        public string BlockStudent(int id, int? userId);
        Task<List<BlockedStudent>> BlockedStudents();
        public bool IsStudentBlocked(int id);
        public Student GetStudentById(int id);

    public  Task<(string message, int requestId)> ReallocationRequestFormSameDep(Reallocation model);

    public  Task<DepartmentInfo> GetDepartmentInfo(int userId);

        void Notify();
        void ManualReserve();
 
    


}
