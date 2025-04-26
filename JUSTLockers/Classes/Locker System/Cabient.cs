namespace JUSTLockers.Classes;
public class Cabinet
{
   // public required string Id { get; set; }
    // public HashSet<Locker> Lockers { get; set; } = new();

    public int CabinetNumber { get; set; }
    public string? Location { get; set; }
    public string? Department { get; set; }
    public string? Wing { get; set; }
    public int Level { get; set; }
    public int? EmployeeId { get; set; } // Nullable for optional data
    public string? EmployeeName { get; set; }
    public int Capacity { get; set; }

    public string? Cabinet_id { get; set; }

    public CabinetStatus? Status { get; set; }

   
}

public enum CabinetStatus
{
    DAMAGED,
    IN_SERVICE,
    IN_MAINTENANCE,
    OUT_OF_SERVICE
}
