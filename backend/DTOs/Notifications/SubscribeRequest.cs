using FluentValidation;
using System.Text.Json.Serialization;

namespace ChatApp.DTOs.Notifications;

public class SubscribeRequest
{
    public string Endpoint { get; set; } = string.Empty;
    [JsonPropertyName("p256dh")]
    public string P256DH { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}

public class SubscribeRequestValidator : AbstractValidator<SubscribeRequest>
{
    public SubscribeRequestValidator()
    {
        RuleFor(x => x.Endpoint).NotEmpty();
        RuleFor(x => x.P256DH).NotEmpty();
        RuleFor(x => x.Auth).NotEmpty();
    }
}
