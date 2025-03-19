namespace JUSTLockers.Classes;

public class Reallocation
{
    public required string Id { get; set; }
    public List<Cabinet> Covenant { get; set; } = new();
    public required string Message { get; set; }
    public required Department TargetDepartment { get; set; }
    public ReallocationStatus Status { get; set; }
    public List<Student> Observers { get; set; } = new();

    public void Register(Student student) => Observers.Add(student);
    public void Unregister(Student student) => Observers.Remove(student);
    public event Action<string>? OnReallocationApproved; // Event to notify students

    public void Approve()
    {
        Status = ReallocationStatus.APPROVED;
        OnReallocationApproved?.Invoke("Your locker has been successfully reallocated.");
    }
}

public enum ReallocationStatus
{
    REQUESTED,
    SUBMITTED,
    PENDING,
    APPROVED,
    REJECTED
}
