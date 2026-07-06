namespace ChatApp.DTOs.Messages;

public class MessageDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
