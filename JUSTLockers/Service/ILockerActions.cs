
using JUSTLockers.Classes;

namespace JUSTLockers.Services;
public interface ILockerActions
{
    public Task<List<Locker>> ViewAvailableLockers(string departmentName);
    //public Task<bool> ReserveLocker(int studentId, string lockerId);
    public Task<bool> CancelReservation(int studentId);
}