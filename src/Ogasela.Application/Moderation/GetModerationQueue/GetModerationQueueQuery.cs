using MediatR;
using Ogasela.Domain.Moderation;
using Ogasela.Shared;

namespace Ogasela.Application.Moderation.GetModerationQueue;

public sealed record GetModerationQueueQuery : IRequest<Result<IReadOnlyList<ModerationQueueItemResponse>>>;

public enum ModerationQueueItemType
{
    Report,
    BiometricManualReview
}

/// <summary>
/// One unified, priority-sorted queue row. For a Report, Id/TargetId are the Report's own
/// Id/TargetId; for a manual-review item, Id is the BiometricVerification's Id and TargetId is
/// the SellerId it belongs to (there's no separate "report target" for an identity check).
/// </summary>
public sealed record ModerationQueueItemResponse(
    Guid Id,
    ModerationQueueItemType Type,
    Guid TargetId,
    string Summary,
    decimal PriorityScore,
    DateTime CreatedAt);
