namespace JUSTLockers.Classes;

public class BlockedStudent
{
    public int? StudentId { get; set; }
    public string? BlockedBy { get; set; }
    public Student? Student { get; set; }
    //public DateTime BlockedUntil { get; set; }
}
