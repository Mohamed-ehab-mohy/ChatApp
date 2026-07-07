namespace ChatApp.Models;

public class PushSubscription
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string P256DH { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
}
