using MediatR;

namespace Ogasela.Application.Reviews;

/// <summary>A buyer left RevieweeUserId (the seller) a review on one of their listings.</summary>
public sealed record ReviewReceivedEvent(Guid RevieweeUserId, Guid ReviewerId, Guid ListingId, int Rating, string Comment) : INotification;
