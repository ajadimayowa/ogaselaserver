using MediatR;
using Ogasela.Application.Messaging.StartConversation;
using Ogasela.Shared;

namespace Ogasela.Application.Messaging.GetConversations;

public sealed record GetConversationsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedConversationsResponse>>;

public sealed record PagedConversationsResponse(IReadOnlyList<ConversationResponse> Items, int Page, int PageSize, int TotalCount);
