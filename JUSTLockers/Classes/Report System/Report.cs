namespace JUSTLockers.Classes;

public class Report
{
    public required int ReportId { get; set; }
    public  Student? Reporter { get; set; }
    public int ReporterId { get; set; }

    public  Locker? Locker { get; set; }
    public string LockerId { get; set; }

    public ReportType Type { get; set; }
    public ReportStatus Status { get; set; }
    public List<string> Images { get; set; } = new();
    public  string Subject { get; set; }
    public  string Statement { get; set; }
    public DateTime ReportDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public  string? ResolutionDetails { get; set; }
    public byte[]? ImageData { get; set; } // nullable (BLOB)
    public string? ImageMimeType { get; set; } // nullable
    public bool SentToAdmin { get; set; }
}

public enum ReportType
{
    THEFT,
    MAINTENANCE,
    LOCKED_LOCKER,
    OTHER,
    
}

public enum ReportStatus
{
    REPORTED,
    IN_REVIEW,
    ESCALATED,
    RESOLVED,
    REJECTED
}
