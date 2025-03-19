namespace JUSTLockers.Classes;
public class Cabinet
{
    public required string Id { get; set; }
    public HashSet<Locker> Lockers { get; set; } = new();
    public CabinetStatus Status { get; set; }
}

public enum CabinetStatus
{
    DAMAGED,
    IN_SERVICE,
    IN_MAINTENANCE,
    OUT_OF_SERVICE
}
