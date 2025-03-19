namespace JUSTLockers.Classes;

public class Department
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public HashSet<Student> Students { get; set; } = new();
    public HashSet<Locker> Lockers { get; set; } = new();
    public Supervisor? Supervisor { get; set; }
}
