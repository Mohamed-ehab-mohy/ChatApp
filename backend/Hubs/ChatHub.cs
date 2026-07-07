using ChatApp.Data;
using ChatApp.Models;
using ChatApp.Services;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private readonly HtmlSanitizer _sanitizer;
    private readonly PushNotificationService _push;

    public ChatHub(AppDbContext db, HtmlSanitizer sanitizer, PushNotificationService push)
    {
        _db = db;
        _sanitizer = sanitizer;
        _push = push;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "GlobalGroup");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "GlobalGroup");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        if (content.Length > 1000)
            return;

        var cleanContent = _sanitizer.Sanitize(content);

        if (string.IsNullOrWhiteSpace(cleanContent))
            return;

        var userId = Context.UserIdentifier;
        if (userId is null)
            return;

        var email = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = cleanContent,
            SenderId = userId,
            SenderEmail = email,
            SentAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        await Clients.Group("GlobalGroup").SendAsync("ReceiveMessage", new
        {
            message.Id,
            message.Content,
            message.SenderEmail,
            message.SentAt
        });

        var otherUserIds = _db.Users
            .Where(u => u.Id != userId)
            .Select(u => u.Id)
            .ToList();

        foreach (var uid in otherUserIds)
        {
            _ = _push.SendToUserAsync(uid, email, cleanContent, "/chat");
        }
    }
}
