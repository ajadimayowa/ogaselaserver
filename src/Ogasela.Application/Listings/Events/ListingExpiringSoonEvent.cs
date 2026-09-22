using MediatR;

namespace Ogasela.Application.Listings.Events;

/// <summary>Raised by the hourly expiring-soon job. Phase 8 (Notifications) will add a handler for this.</summary>
public sealed record ListingExpiringSoonEvent(Guid ListingId, Guid SellerId, DateTime ExpiresAt) : INotification;
