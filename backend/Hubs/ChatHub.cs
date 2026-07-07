using ChatApp.Data;
using ChatApp.DTOs.Notifications;
using ChatApp.Models;
using ChatApp.Services;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private readonly HtmlSanitizer _sanitizer;
    private readonly IServiceScopeFactory _scopeFactory;

    public ChatHub(AppDbContext db, HtmlSanitizer sanitizer, IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _sanitizer = sanitizer;
        _scopeFactory = scopeFactory;
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

        var subs = await _db.PushSubscriptions
            .Where(s => s.UserId != userId)
            .Select(s => new PushSubDto
            {
                Endpoint = s.Endpoint,
                P256DH = s.P256DH,
                Auth = s.Auth
            })
            .ToListAsync();

        if (subs.Count == 0)
            return;

        _ = SendPushNotificationsAsync(_scopeFactory, subs, email, cleanContent);
    }

    private static async Task SendPushNotificationsAsync(
        IServiceScopeFactory scopeFactory,
        List<PushSubDto> subs,
        string title,
        string body)
    {
        using var scope = scopeFactory.CreateScope();
        var push = scope.ServiceProvider.GetRequiredService<PushNotificationService>();
        await push.SendBatchAsync(subs, title, body, "/chat");
    }
}
