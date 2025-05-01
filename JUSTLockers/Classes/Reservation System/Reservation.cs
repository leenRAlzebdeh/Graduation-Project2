namespace JUSTLockers.Classes;

public class Reservation
{
    public required string Id { get; set; }
    public DateTime Date { get; set; }
    public required int StudentId { get; set; }
    public required string LockerId { get; set; }
    public required string StudentName { get; set; }
    public ReservationStatus Status { get; set; }
}

public enum ReservationStatus
{
    AVAILABLE,
    CANCELED,
    BLOCKED,
    RESERVED
}
