using MediatR;

namespace Ogasela.Application.Messaging;

/// <summary>A message was delivered to RecipientId. Preview is the text (or "Photo") shown in their notification.</summary>
public sealed record NewMessageEvent(Guid ConversationId, Guid ListingId, Guid RecipientId, Guid SenderId, string Preview) : INotification;
