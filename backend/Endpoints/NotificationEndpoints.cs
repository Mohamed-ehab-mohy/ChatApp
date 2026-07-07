using ChatApp.DTOs.Notifications;
using ChatApp.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChatApp.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/public-key", (PushNotificationService push) =>
        {
            return Results.Ok(new { publicKey = push.PublicKey });
        }).AllowAnonymous();

        group.MapPost("/subscribe", async (
            SubscribeRequest request,
            HttpContext context,
            PushNotificationService push,
            IValidator<SubscribeRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId is null)
                return Results.Unauthorized();

            await push.SubscribeAsync(userId, request.Endpoint, request.P256DH, request.Auth);
            return Results.Ok(new { message = "Subscribed" });
        });

        group.MapPost("/unsubscribe", async (
            UnsubscribeRequest request,
            HttpContext context,
            PushNotificationService push) =>
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId is null)
                return Results.Unauthorized();

            await push.UnsubscribeAsync(userId, request.Endpoint);
            return Results.Ok(new { message = "Unsubscribed" });
        });
    }
}
