namespace JUSTLockers.Classes;

public class Notification
{
    public int Id { get; set; }
    public required string UserId { get; set; }  // Receiver of the notification
    public required User User { get; set; }      // Navigation property
    public required string Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
