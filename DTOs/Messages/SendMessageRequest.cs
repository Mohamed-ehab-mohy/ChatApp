using FluentValidation;

namespace ChatApp.DTOs.Messages;

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
}

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(1000);
    }
}
