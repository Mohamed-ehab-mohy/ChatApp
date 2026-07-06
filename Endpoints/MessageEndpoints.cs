using ChatApp.Data;
using ChatApp.DTOs.Messages;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Endpoints;

public static class MessageEndpoints
{
    public static void MapMessageEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (AppDbContext db, int limit = 50) =>
        {
            var messages = await db.Messages
                .OrderByDescending(m => m.SentAt)
                .Take(limit)
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    SenderEmail = m.SenderEmail,
                    SentAt = m.SentAt
                })
                .ToListAsync();

            return Results.Ok(messages);
        });
    }
}
