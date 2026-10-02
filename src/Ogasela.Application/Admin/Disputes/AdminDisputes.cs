using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Admin.Listings;
using Ogasela.Application.Admin.Users;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Moderation.Disputes;
using Ogasela.Application.Notifications.Inbox;
using Ogasela.Domain.Listings;
using Ogasela.Domain.Moderation;
using Ogasela.Domain.Notifications;
using Ogasela.Shared;

namespace Ogasela.Application.Admin.Disputes;

/// <summary>Disputes for the Control Portal, newest first. Search matches the reference, the ad title or either party's name.</summary>
public sealed record GetAdminDisputesQuery(DisputeStatus? Status, string? Search, bool AssignedToMe, int Page, int PageSize)
    : IRequest<Result<AdminDisputePage>>;

public sealed record AdminDisputeListItem(
    Guid Id,
    string Reference,
    string ListingTitle,
    string RaisedByName,
    string AgainstName,
    DisputeReason Reason,
    DisputeStatus Status,
    string? AssignedToName,
    int MessageCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record AdminDisputePage(IReadOnlyList<AdminDisputeListItem> Items, int Page, int PageSize, int TotalCount);

public sealed record GetAdminDisputeQuery(Guid DisputeId) : IRequest<Result<AdminDisputeDetail>>;

public sealed record AdminDisputeParty(
    Guid UserId, string? Name, string? Email, string? Phone, bool IsSeller, bool IsSuspended, string? BusinessName);

public sealed record AdminDisputeListing(Guid Id, string Title, decimal? Price, ListingStatus Status, string? ThumbnailUrl);

/// <summary>The parties' own chat about the ad, for context. Role is "Buyer" or "Seller".</summary>
public sealed record AdminChatMessage(string Role, string Content, string? ImageUrl, DateTime SentAt);

public sealed record AdminDisputeDetail(
    Guid Id,
    string Reference,
    DisputeReason Reason,
    string Description,
    DisputeStatus Status,
    DisputeOutcome? Outcome,
    string? ResolutionNote,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt,
    Guid? AssignedToUserId,
    string? AssignedToName,
    AdminDisputeParty RaisedBy,
    AdminDisputeParty Against,
    AdminDisputeListing Listing,
    IReadOnlyList<DisputeEvidence> Evidence,
    IReadOnlyList<AdminDisputeThreadMessage> Messages,
    IReadOnlyList<AdminChatMessage> Chat);

public sealed record AdminDisputeThreadMessage(
    Guid Id, DisputeAuthorRole AuthorRole, string AuthorName, string Body, DisputeEvidence? Attachment, DateTime CreatedAt);

public sealed record AssignDisputeToMeCommand(Guid DisputeId) : IRequest<Result>;

/// <summary>Staff message on the thread - both parties see it and are notified.</summary>
public sealed record AddStaffDisputeMessageCommand(Guid DisputeId, string Body) : IRequest<Result>;

/// <summary>Closes the dispute with an outcome both parties are told about. Optionally takes the ad down and/or suspends one party.</summary>
public sealed record ResolveDisputeCommand(
    Guid DisputeId, DisputeOutcome Outcome, string Note, bool TakeDownListing, Guid? SuspendUserId) : IRequest<Result>;

public sealed class ResolveDisputeCommandValidator : AbstractValidator<ResolveDisputeCommand>
{
    public ResolveDisputeCommandValidator()
    {
        RuleFor(x => x.Outcome).IsInEnum();
        RuleFor(x => x.Note).NotEmpty().WithMessage("Explain the decision - both parties see it.").MaximumLength(2000);
    }
}

public sealed class AddStaffDisputeMessageCommandValidator : AbstractValidator<AddStaffDisputeMessageCommand>
{
    public AddStaffDisputeMessageCommandValidator() => RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
}

public sealed class AdminDisputeHandlers :
    IRequestHandler<GetAdminDisputesQuery, Result<AdminDisputePage>>,
    IRequestHandler<GetAdminDisputeQuery, Result<AdminDisputeDetail>>,
    IRequestHandler<AssignDisputeToMeCommand, Result>,
    IRequestHandler<AddStaffDisputeMessageCommand, Result>,
    IRequestHandler<ResolveDisputeCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDisputeEvidenceStorage _storage;
    private readonly IAuditLogger _auditLogger;
    private readonly IDateTime _dateTime;
    private readonly ISender _sender;

    public AdminDisputeHandlers(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IDisputeEvidenceStorage storage,
        IAuditLogger auditLogger, IDateTime dateTime, ISender sender)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _storage = storage;
        _auditLogger = auditLogger;
        _dateTime = dateTime;
        _sender = sender;
    }

    private Guid Me => _currentUser.UserId!.Value;

    public async Task<Result<AdminDisputePage>> Handle(GetAdminDisputesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query =
            from d in _dbContext.Disputes
            join l in _dbContext.Listings on d.ListingId equals l.Id
            join raiser in _dbContext.Users on d.RaisedByUserId equals raiser.Id
            join against in _dbContext.Users on d.AgainstUserId equals against.Id
            join staff in _dbContext.Users on d.AssignedToUserId equals staff.Id into staffJoin
            from staff in staffJoin.DefaultIfEmpty()
            select new { d, l.Title, RaiserName = raiser.Name, AgainstName = against.Name, StaffName = staff == null ? null : staff.Name };

        if (request.Status is { } status)
        {
            query = query.Where(x => x.d.Status == status);
        }

        if (request.AssignedToMe)
        {
            query = query.Where(x => x.d.AssignedToUserId == Me);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.d.Reference.ToLower().Contains(term)
                || x.Title.ToLower().Contains(term)
                || (x.RaiserName != null && x.RaiserName.ToLower().Contains(term))
                || (x.AgainstName != null && x.AgainstName.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.d.CreatedAt).ThenByDescending(x => x.d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var ids = rows.Select(r => r.d.Id).ToList();
        var counts = await _dbContext.DisputeMessages
            .Where(m => ids.Contains(m.DisputeId))
            .GroupBy(m => m.DisputeId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var items = rows.Select(r => new AdminDisputeListItem(
                r.d.Id, r.d.Reference, r.Title, r.RaiserName ?? "Ogasela user", r.AgainstName ?? "Ogasela user",
                r.d.Reason, r.d.Status, r.StaffName, counts.GetValueOrDefault(r.d.Id), r.d.CreatedAt, r.d.UpdatedAt))
            .ToList();

        return Result.Success(new AdminDisputePage(items, page, pageSize, total));
    }

    public async Task<Result<AdminDisputeDetail>> Handle(GetAdminDisputeQuery request, CancellationToken cancellationToken)
    {
        var dispute = await _dbContext.Disputes.FirstOrDefaultAsync(d => d.Id == request.DisputeId, cancellationToken);
        if (dispute is null)
        {
            return Result.Failure<AdminDisputeDetail>(DisputeErrors.NotFound);
        }

        var listing = await _dbContext.Listings.FirstAsync(l => l.Id == dispute.ListingId, cancellationToken);
        var raisedBy = await PartyAsync(dispute.RaisedByUserId, cancellationToken);
        var against = await PartyAsync(dispute.AgainstUserId, cancellationToken);
        var assignedName = dispute.AssignedToUserId is { } staffId
            ? await _dbContext.Users.Where(u => u.Id == staffId).Select(u => u.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        var thread = await (
            from m in _dbContext.DisputeMessages
            where m.DisputeId == dispute.Id
            join u in _dbContext.Users on m.AuthorUserId equals u.Id
            orderby m.CreatedAt
            select new { m, u.Name })
            .ToListAsync(cancellationToken);

        var chat = new List<AdminChatMessage>();
        if (dispute.ConversationId is { } conversationId)
        {
            var conversation = await _dbContext.Conversations.FirstAsync(c => c.Id == conversationId, cancellationToken);
            chat = (await _dbContext.Messages
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.SentAt)
                    .Take(100)
                    .ToListAsync(cancellationToken))
                .OrderBy(m => m.SentAt)
                .Select(m => new AdminChatMessage(m.SenderId == conversation.SellerId ? "Seller" : "Buyer", m.Content, m.ImageUrl, m.SentAt))
                .ToList();
        }

        return Result.Success(new AdminDisputeDetail(
            dispute.Id,
            dispute.Reference,
            dispute.Reason,
            dispute.Description,
            dispute.Status,
            dispute.Outcome,
            dispute.ResolutionNote,
            dispute.CreatedAt,
            dispute.UpdatedAt,
            dispute.ResolvedAt,
            dispute.AssignedToUserId,
            assignedName,
            raisedBy,
            against,
            new AdminDisputeListing(listing.Id, listing.Title, listing.Price, listing.Status, listing.MediaUrls.FirstOrDefault()),
            dispute.EvidenceKeys.Select(ToEvidence).ToList(),
            thread.Select(r => new AdminDisputeThreadMessage(
                    r.m.Id, r.m.AuthorRole, r.Name ?? "Ogasela user", r.m.Body,
                    r.m.AttachmentKey is null ? null : ToEvidence(r.m.AttachmentKey), r.m.CreatedAt))
                .ToList(),
            chat));
    }

    public async Task<Result> Handle(AssignDisputeToMeCommand request, CancellationToken cancellationToken)
    {
        var dispute = await _dbContext.Disputes.FirstOrDefaultAsync(d => d.Id == request.DisputeId, cancellationToken);
        if (dispute is null)
        {
            return Result.Failure(DisputeErrors.NotFound);
        }

        if (dispute.Status == DisputeStatus.Resolved)
        {
            return Result.Failure(DisputeErrors.Resolved);
        }

        dispute.AssignTo(Me, _dateTime.UtcNow);
        await _auditLogger.LogAsync(Me, "Dispute.Assigned", nameof(Dispute), dispute.Id, null, null, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(AddStaffDisputeMessageCommand request, CancellationToken cancellationToken)
    {
        var dispute = await _dbContext.Disputes.FirstOrDefaultAsync(d => d.Id == request.DisputeId, cancellationToken);
        if (dispute is null)
        {
            return Result.Failure(DisputeErrors.NotFound);
        }

        if (dispute.Status == DisputeStatus.Resolved)
        {
            return Result.Failure(DisputeErrors.Resolved);
        }

        var now = _dateTime.UtcNow;
        var body = request.Body.Trim();
        _dbContext.DisputeMessages.Add(DisputeMessage.Create(dispute.Id, Me, DisputeAuthorRole.Staff, body, null, now));
        // Messaging a case means someone is on it.
        if (dispute.AssignedToUserId is null)
        {
            dispute.AssignTo(Me, now);
        }

        dispute.Touch(now);
        foreach (var party in new[] { dispute.RaisedByUserId, dispute.AgainstUserId })
        {
            _dbContext.InboxNotifications.Add(InboxNotification.Create(
                party, InboxNotificationTypes.DisputeUpdate, $"Ogasela support replied on {dispute.Reference}",
                body.Length <= 120 ? body : body[..119].TrimEnd() + "…", dispute.ListingId, null, now, dispute.Id));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(ResolveDisputeCommand request, CancellationToken cancellationToken)
    {
        var dispute = await _dbContext.Disputes.FirstOrDefaultAsync(d => d.Id == request.DisputeId, cancellationToken);
        if (dispute is null)
        {
            return Result.Failure(DisputeErrors.NotFound);
        }

        if (dispute.Status == DisputeStatus.Resolved)
        {
            return Result.Failure(DisputeErrors.Resolved);
        }

        if (request.SuspendUserId is { } suspendId && !dispute.IsParty(suspendId))
        {
            return Result.Failure(DisputeErrors.NotAParty);
        }

        var note = request.Note.Trim();
        var now = _dateTime.UtcNow;
        dispute.Resolve(request.Outcome, note, Me, now);
        _dbContext.DisputeMessages.Add(DisputeMessage.Create(
            dispute.Id, Me, DisputeAuthorRole.Staff, $"{DisputeRules.OutcomeLabel(request.Outcome)}. {note}", null, now));

        foreach (var party in new[] { dispute.RaisedByUserId, dispute.AgainstUserId })
        {
            _dbContext.InboxNotifications.Add(InboxNotification.Create(
                party, InboxNotificationTypes.DisputeResolved, $"Dispute {dispute.Reference} resolved",
                note.Length <= 120 ? note : note[..119].TrimEnd() + "…", dispute.ListingId, null, now, dispute.Id));
        }

        await _auditLogger.LogAsync(Me, "Dispute.Resolved", nameof(Dispute), dispute.Id, null,
            JsonSerializer.Serialize(new { Outcome = request.Outcome.ToString(), Note = note, request.TakeDownListing, request.SuspendUserId }),
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Follow-up actions reuse the normal admin commands (with their own checks, notifications and audit entries).
        var followUpErrors = new List<string>();
        if (request.TakeDownListing)
        {
            var result = await _sender.Send(
                new TakeDownListingCommand(dispute.ListingId, $"Following dispute {dispute.Reference}: {note}"), cancellationToken);
            if (result.IsFailure && result.Error != AdminListingErrors.NotLive)
            {
                followUpErrors.Add(result.Error.Message);
            }
        }

        if (request.SuspendUserId is { } userId)
        {
            var result = await _sender.Send(
                new SuspendUserCommand(userId, $"Dispute {dispute.Reference}: {note}"), cancellationToken);
            if (result.IsFailure && result.Error != AdminUserErrors.AlreadySuspended)
            {
                followUpErrors.Add(result.Error.Message);
            }
        }

        return followUpErrors.Count == 0
            ? Result.Success()
            : Result.Failure(new Error("Dispute.FollowUpFailed",
                $"The dispute was resolved, but: {string.Join(" ", followUpErrors)}"));
    }

    private async Task<AdminDisputeParty> PartyAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstAsync(u => u.Id == userId, cancellationToken);
        var business = await _dbContext.SellerProfiles
            .Where(s => s.UserId == userId)
            .Select(s => s.BusinessName)
            .FirstOrDefaultAsync(cancellationToken);
        return new AdminDisputeParty(user.Id, user.Name, user.Email, user.Phone, business is not null, user.IsSuspended, business);
    }

    private DisputeEvidence ToEvidence(string key) =>
        new(_storage.GetImageUrl(key), key.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
}
