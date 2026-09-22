using MediatR;
using Ogasela.Application.Messaging.SendMessage;
using Ogasela.Shared;

namespace Ogasela.Application.Messaging.GetMessages;

public sealed record GetMessagesQuery(Guid ConversationId, int Page = 1, int PageSize = 30)
    : IRequest<Result<PagedMessagesResponse>>;

/// <summary>Items are ordered oldest-first within the page (newest last), matching how a chat thread reads top-to-bottom.</summary>
public sealed record PagedMessagesResponse(IReadOnlyList<MessageResponse> Items, int Page, int PageSize, int TotalCount);
