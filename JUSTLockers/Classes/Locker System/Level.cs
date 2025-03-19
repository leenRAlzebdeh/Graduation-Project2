namespace JUSTLockers.Classes;

public class Level
{
    public required string Id { get; set; }
    public HashSet<Cabinet> Cabinets { get; set; } = new();
}


