namespace JUSTLockers.Classes;

public class Locker
{
    public required string LockerId { get; set; }
    public required string Department { get; set; }
    public Reservation? CurrentReservation { get; set; }
    public Student? AssignedStudent { get; set; }
    public LockerStatus Status { get; set; }
}

public enum LockerStatus
{
    EMPTY,
    RESERVED,
    IN_MAINTENANCE,
    OUT_OF_SERVICE
}
