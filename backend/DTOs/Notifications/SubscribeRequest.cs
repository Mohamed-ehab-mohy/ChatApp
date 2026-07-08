using FluentValidation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatApp.DTOs.Notifications;

[JsonConverter(typeof(SubscribeRequestConverter))]
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

public class SubscribeRequestConverter : JsonConverter<SubscribeRequest>
{
    public override SubscribeRequest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var request = new SubscribeRequest();

        if (root.TryGetProperty("endpoint", out var endpoint))
            request.Endpoint = endpoint.GetString() ?? string.Empty;

        if (root.TryGetProperty("p256dh", out var p256dh))
            request.P256DH = p256dh.GetString() ?? string.Empty;

        if (root.TryGetProperty("auth", out var auth))
            request.Auth = auth.GetString() ?? string.Empty;

        if ((string.IsNullOrEmpty(request.P256DH) || string.IsNullOrEmpty(request.Auth))
            && root.TryGetProperty("keys", out var keys) && keys.ValueKind == JsonValueKind.Object)
        {
            if (keys.TryGetProperty("p256dh", out var kp256dh))
                request.P256DH = kp256dh.GetString() ?? string.Empty;
            if (keys.TryGetProperty("auth", out var kauth))
                request.Auth = kauth.GetString() ?? string.Empty;
        }

        return request;
    }

    public override void Write(Utf8JsonWriter writer, SubscribeRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("endpoint", value.Endpoint);
        writer.WriteString("p256dh", value.P256DH);
        writer.WriteString("auth", value.Auth);
        writer.WriteEndObject();
    }
}
