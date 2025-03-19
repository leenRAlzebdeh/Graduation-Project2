namespace JUSTLockers.Classes;

public class Reservation
{
    public required string Id { get; set; }
    public DateTime Date { get; set; }
    public required string StudentId { get; set; }
    public required Student Student { get; set; }
    public required string LockerId { get; set; }
    public required Locker Locker { get; set; }
    public ReservationStatus Status { get; set; }
}

public enum ReservationStatus
{
    AVAILABLE,
    CANCELED,
    BLOCKED,
    RESERVED
}
