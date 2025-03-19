namespace JUSTLockers.Classes;

public class BlockedStudent
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public DateTime BlockedUntil { get; set; }
}
