using ChatApp.Data;
using ChatApp.DTOs.Notifications;
using ChatApp.Settings;
using WebPush;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Services;

public class PushNotificationService
{
    private readonly AppDbContext _db;
    private readonly VapidSettings _vapid;
    private readonly WebPushClient _client;

    public PushNotificationService(AppDbContext db, VapidSettings vapid)
    {
        _db = db;
        _vapid = vapid;
        _client = new WebPushClient();
    }

    public string PublicKey => _vapid.PublicKey;

    public async Task SubscribeAsync(string userId, string endpoint, string p256dh, string auth)
    {
        var existing = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

        if (existing is not null)
        {
            existing.P256DH = p256dh;
            existing.Auth = auth;
        }
        else
        {
            _db.PushSubscriptions.Add(new Models.PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Endpoint = endpoint,
                P256DH = p256dh,
                Auth = auth
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task UnsubscribeAsync(string userId, string endpoint)
    {
        var subs = await _db.PushSubscriptions
            .Where(s => s.UserId == userId && s.Endpoint == endpoint)
            .ToListAsync();

        _db.PushSubscriptions.RemoveRange(subs);
        await _db.SaveChangesAsync();
    }

    public async Task SendBatchAsync(List<PushSubDto> subs, string title, string body, string? url = null)
    {
        var payload = new PushNotificationPayload
        {
            Title = title,
            Body = body,
            Url = url ?? "/"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);

        foreach (var sub in subs)
        {
            try
            {
                var webPushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256DH, sub.Auth);
                await _client.SendNotificationAsync(webPushSub, json, new VapidDetails(
                    _vapid.Subject,
                    _vapid.PublicKey,
                    _vapid.PrivateKey
                ));
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone
                || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var dbSub = await _db.PushSubscriptions
                    .FirstOrDefaultAsync(s => s.Endpoint == sub.Endpoint);
                if (dbSub is not null)
                    _db.PushSubscriptions.Remove(dbSub);
            }
        }

        await _db.SaveChangesAsync();
    }

    private class PushNotificationPayload
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Url { get; set; }
    }
}
