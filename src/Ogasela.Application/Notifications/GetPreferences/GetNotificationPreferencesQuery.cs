using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Notifications;
using Ogasela.Shared;

namespace Ogasela.Application.Notifications.GetPreferences;

/// <summary>
/// Every (Channel, Category) pair the app lets a user toggle, with its current state. Pairs the
/// user never touched come back enabled - the opt-out default NotificationDispatcher applies.
/// </summary>
public sealed record GetNotificationPreferencesQuery : IRequest<Result<IReadOnlyList<NotificationPreferenceResponse>>>;

public sealed record NotificationPreferenceResponse(NotificationChannel Channel, string Category, bool Enabled);

public sealed class GetNotificationPreferencesQueryHandler
    : IRequestHandler<GetNotificationPreferencesQuery, Result<IReadOnlyList<NotificationPreferenceResponse>>>
{
    /// <summary>The channels NotificationDispatcher actually delivers on.</summary>
    private static readonly NotificationChannel[] Channels = [NotificationChannel.Push, NotificationChannel.Email];

    private static readonly string[] Categories = [NotificationCategories.ListingLifecycle, NotificationCategories.Verification];

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationPreferencesQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<NotificationPreferenceResponse>>> Handle(
        GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var saved = await _dbContext.NotificationPreferences
            .Where(p => p.UserId == _currentUser.UserId)
            .ToListAsync(cancellationToken);

        var result = (
            from category in Categories
            from channel in Channels
            let row = saved.FirstOrDefault(p => p.Channel == channel && p.Category == category)
            select new NotificationPreferenceResponse(channel, category, row?.Enabled ?? true)).ToList();

        return Result.Success<IReadOnlyList<NotificationPreferenceResponse>>(result);
    }
}
