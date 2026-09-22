using MediatR;

namespace Ogasela.Application.Listings.Events;

/// <summary>Raised by the hourly expiry job. Phase 8 (Notifications) will add a handler for this.</summary>
public sealed record ListingExpiredEvent(Guid ListingId, Guid SellerId, DateTime ExpiredAt) : INotification;
