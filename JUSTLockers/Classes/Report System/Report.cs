namespace JUSTLockers.Classes;

public class Report
{
    public required int ReportId { get; set; }
    public required Student Reporter { get; set; }
    public required Locker Locker { get; set; }
    public ReportType Type { get; set; }
    public ReportStatus Status { get; set; }
    public List<string> Images { get; set; } = new();
    public required string Subject { get; set; }
    public required string Statement { get; set; }
    public DateTime ReportDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public  string? ResolutionDetails { get; set; }
}

public enum ReportType
{
    THEFT,
    MAINTENANCE,
    LOCKED_LOCKER
}

public enum ReportStatus
{
    REPORTED,
    IN_REVIEW,
    ESCALATED,
    RESOLVED,
    REJECTED
}
