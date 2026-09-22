using FluentValidation;

namespace Ogasela.Application.Messaging.SendMessage;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Content) || !string.IsNullOrWhiteSpace(x.ImageUrl))
            .WithMessage("A message needs text content, an image, or both.");

        When(x => !string.IsNullOrEmpty(x.Content), () =>
        {
            RuleFor(x => x.Content).MaximumLength(4000);
        });
    }
}
