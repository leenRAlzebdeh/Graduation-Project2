
namespace JUSTLockers.Services;
public interface ILockerActions
{
    void ViewAvailableLockers();
    void ReserveLocker();
    void CancelReservation();
}