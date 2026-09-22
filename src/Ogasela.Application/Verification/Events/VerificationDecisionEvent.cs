using MediatR;
using Ogasela.Domain.Verification;

namespace Ogasela.Application.Verification.Events;

/// <summary>
/// Raised whenever a seller's verification reaches a final decision (automatic or manual).
/// Added retroactively in Phase 8 so NotificationDispatcher has something to subscribe to -
/// Phase 2 didn't raise any domain event itself. SellerId is SellerProfile.Id; subscribers that
/// need the underlying User (e.g. to send a notification) resolve it themselves.
/// </summary>
public sealed record VerificationDecisionEvent(Guid SellerId, VerificationDecision Decision, DateTime DecidedAt) : INotification;
