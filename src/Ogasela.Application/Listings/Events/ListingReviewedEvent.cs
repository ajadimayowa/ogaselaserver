using MediatR;

namespace Ogasela.Application.Listings.Events;

/// <summary>A moderator approved (now live) or rejected (back to Draft, with Reason) a listing awaiting review.</summary>
public sealed record ListingReviewedEvent(Guid ListingId, Guid SellerId, string Title, bool Approved, string? Reason) : INotification;
