using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Notifications.Inbox;
using Ogasela.Domain.Listings;
using Ogasela.Domain.Notifications;
using Ogasela.Shared;

namespace Ogasela.Application.Admin.Listings;

/// <summary>Takes a live ad off public view (paused) with a reason the seller sees in their notifications.</summary>
public sealed record TakeDownListingCommand(Guid ListingId, string Reason) : IRequest<Result>;

/// <summary>Puts a paused ad back on public view (or Expired, if its time ran out meanwhile).</summary>
public sealed record ReinstateListingCommand(Guid ListingId) : IRequest<Result>;

public sealed record ChangeListingCategoryCommand(Guid ListingId, Guid CategoryId) : IRequest<Result>;

public sealed class TakeDownListingCommandValidator : AbstractValidator<TakeDownListingCommand>
{
    public TakeDownListingCommandValidator() =>
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Tell the seller why the ad was taken down.").MaximumLength(1000);
}

public sealed class AdminListingCommandHandlers :
    IRequestHandler<TakeDownListingCommand, Result>,
    IRequestHandler<ReinstateListingCommand, Result>,
    IRequestHandler<ChangeListingCategoryCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IDateTime _dateTime;

    public AdminListingCommandHandlers(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IAuditLogger auditLogger, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _dateTime = dateTime;
    }

    private async Task NotifySellerAsync(Listing listing, string type, string title, string body, CancellationToken cancellationToken)
    {
        var userId = await _dbContext.SellerProfiles
            .Where(s => s.Id == listing.SellerId)
            .Select(s => (Guid?)s.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId is { } id)
        {
            _dbContext.InboxNotifications.Add(InboxNotification.Create(id, type, title, body, listing.Id, null, _dateTime.UtcNow));
        }
    }

    public async Task<Result> Handle(TakeDownListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await _dbContext.Listings.FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken);
        if (listing is null)
        {
            return Result.Failure(AdminListingErrors.NotFound);
        }

        if (listing.Status is not (ListingStatus.Active or ListingStatus.ExpiringSoon))
        {
            return Result.Failure(AdminListingErrors.NotLive);
        }

        var reason = request.Reason.Trim();
        listing.TakeDown(reason, _dateTime.UtcNow);
        await NotifySellerAsync(listing, InboxNotificationTypes.AdTakenDown, "Your ad was taken down",
            $"“{listing.Title}”: {reason}", cancellationToken);
        await _auditLogger.LogAsync(_currentUser.UserId!.Value, "Listing.TakenDown", nameof(Listing), listing.Id, null,
            JsonSerializer.Serialize(new { Reason = reason }), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(ReinstateListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await _dbContext.Listings.FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken);
        if (listing is null)
        {
            return Result.Failure(AdminListingErrors.NotFound);
        }

        if (listing.Status != ListingStatus.Paused)
        {
            return Result.Failure(AdminListingErrors.NotPaused);
        }

        listing.Reinstate(_dateTime.UtcNow);
        if (listing.Status == ListingStatus.Active)
        {
            await NotifySellerAsync(listing, InboxNotificationTypes.AdReinstated, "Your ad is live again",
                $"“{listing.Title}” is back on Ogasela.", cancellationToken);
        }

        await _auditLogger.LogAsync(_currentUser.UserId!.Value, "Listing.Reinstated", nameof(Listing), listing.Id, null,
            JsonSerializer.Serialize(new { Status = listing.Status.ToString() }), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(ChangeListingCategoryCommand request, CancellationToken cancellationToken)
    {
        var listing = await _dbContext.Listings.FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken);
        if (listing is null)
        {
            return Result.Failure(AdminListingErrors.NotFound);
        }

        var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(AdminListingErrors.CategoryNotFound);
        }

        var previous = listing.CategoryId;
        listing.ChangeCategory(category.Id, _dateTime.UtcNow);
        await _auditLogger.LogAsync(_currentUser.UserId!.Value, "Listing.CategoryChanged", nameof(Listing), listing.Id,
            JsonSerializer.Serialize(new { CategoryId = previous }),
            JsonSerializer.Serialize(new { CategoryId = category.Id, Category = category.Name }), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
