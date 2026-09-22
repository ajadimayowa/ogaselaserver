using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Messaging.StartConversation;

public sealed record StartConversationCommand(Guid ListingId) : IRequest<Result<ConversationResponse>>;

public sealed record ConversationResponse(
    Guid Id, Guid ListingId, Guid BuyerId, Guid SellerId, Guid OtherParticipantId, DateTime CreatedAt, DateTime LastMessageAt);
