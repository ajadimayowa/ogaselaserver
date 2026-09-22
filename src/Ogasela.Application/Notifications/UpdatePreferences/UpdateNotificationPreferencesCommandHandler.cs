using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Notifications;
using Ogasela.Shared;

namespace Ogasela.Application.Notifications.UpdatePreferences;

public sealed class UpdateNotificationPreferencesCommandHandler : IRequestHandler<UpdateNotificationPreferencesCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public UpdateNotificationPreferencesCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;

        foreach (var update in request.Preferences)
        {
            var existing = await _dbContext.NotificationPreferences.FirstOrDefaultAsync(
                p => p.UserId == userId && p.Channel == update.Channel && p.Category == update.Category, cancellationToken);

            if (existing is null)
            {
                _dbContext.NotificationPreferences.Add(
                    NotificationPreference.Create(userId, update.Channel, update.Category, update.Enabled));
            }
            else
            {
                existing.SetEnabled(update.Enabled);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
