namespace JUSTLockers.Classes;

public class Reservation
{
    public required string Id { get; set; }
    public DateTime Date { get; set; }
    public required int StudentId { get; set; }
    public required string LockerId { get; set; }
    public required string StudentName { get; set; }
    public ReservationStatus Status { get; set; }
    public string? Wing { get; set; }
    public int? Level { get; set; }
    public string? Department { get; set; }
    public string? Location { get; set; }

}

public enum ReservationStatus
{
    AVAILABLE,
    CANCELED,
    BLOCKED,
    RESERVED
}
