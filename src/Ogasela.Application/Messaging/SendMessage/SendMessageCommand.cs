using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Messaging.SendMessage;

public sealed record SendMessageCommand(Guid ConversationId, string Content, string? ImageUrl)
    : IRequest<Result<MessageResponse>>;

public sealed record MessageResponse(
    Guid Id, Guid ConversationId, Guid SenderId, string Content, string? ImageUrl, DateTime SentAt, DateTime? ReadAt);
