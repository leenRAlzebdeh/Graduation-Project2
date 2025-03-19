namespace JUSTLockers.Services;
public interface ISupervisorService : IUserActions, ILockerActions
{
    void ViewAllStudentReservations();
    void Notify();
    void ReallocateCabinet();
    void ViewCovenantInfo();
    void CancelStudentReservation();
    void ManualReserve();
    void ViewReportList();
    void UpdateReportStatus();
    void EscalateReport();
    void BlockStudent();
    void UnblockStudent();
    void ViewBlockList();
}
