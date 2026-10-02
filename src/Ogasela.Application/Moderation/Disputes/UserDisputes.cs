using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Notifications.Inbox;
using Ogasela.Domain.Listings;
using Ogasela.Domain.Moderation;
using Ogasela.Domain.Notifications;
using Ogasela.Shared;

namespace Ogasela.Application.Moderation.Disputes;

/// <summary>
/// Opens a dispute about a deal on one ad. A buyer raises it against the ad's seller; a seller
/// raises it against a buyer, which needs their chat (ConversationId) to know which buyer.
/// </summary>
public sealed record RaiseDisputeCommand(
    Guid ListingId,
    Guid? ConversationId,
    DisputeReason Reason,
    string Description,
    IReadOnlyList<DisputeFile> Evidence) : IRequest<Result<UserDisputeSummary>>;

public sealed record GetMyDisputesQuery : IRequest<Result<IReadOnlyList<UserDisputeSummary>>>;

public sealed record GetMyDisputeQuery(Guid DisputeId) : IRequest<Result<UserDisputeDetail>>;

/// <summary>A party's reply on the dispute thread, optionally with one image/PDF.</summary>
public sealed record AddDisputeMessageCommand(Guid DisputeId, string Body, DisputeFile? Attachment) : IRequest<Result<DisputeThreadMessage>>;

/// <summary>MyRole is Raiser or Respondent.</summary>
public sealed record UserDisputeSummary(
    Guid Id,
    string Reference,
    Guid ListingId,
    string ListingTitle,
    string? ListingThumbnailUrl,
    DisputeAuthorRole MyRole,
    string OtherPartyName,
    DisputeReason Reason,
    DisputeStatus Status,
    DisputeOutcome? Outcome,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record UserDisputeDetail(
    UserDisputeSummary Summary,
    string Description,
    IReadOnlyList<DisputeEvidence> Evidence,
    string? ResolutionNote,
    DateTime? ResolvedAt,
    IReadOnlyList<DisputeThreadMessage> Messages);

public sealed record DisputeEvidence(string Url, bool IsPdf);

/// <summary>AuthorName is "Ogasela support" for staff entries.</summary>
public sealed record DisputeThreadMessage(
    Guid Id, DisputeAuthorRole AuthorRole, string AuthorName, string Body, DisputeEvidence? Attachment, DateTime CreatedAt);

public sealed class RaiseDisputeCommandValidator : AbstractValidator<RaiseDisputeCommand>
{
    public RaiseDisputeCommandValidator()
    {
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Tell us what happened.")
            .MinimumLength(20).WithMessage("Add a bit more detail (at least 20 characters).")
            .MaximumLength(2000);
        RuleFor(x => x.Evidence.Count).LessThanOrEqualTo(DisputeRules.MaxEvidenceFiles)
            .WithMessage($"Attach at most {DisputeRules.MaxEvidenceFiles} files.");
        RuleForEach(x => x.Evidence)
            .Must(f => DisputeRules.AllowedContentTypes.Contains(f.ContentType))
            .WithMessage("Evidence must be photos (JPEG, PNG, HEIC, WebP) or PDFs.");
    }
}

public sealed class AddDisputeMessageCommandValidator : AbstractValidator<AddDisputeMessageCommand>
{
    public AddDisputeMessageCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
        When(x => x.Attachment is not null, () =>
            RuleFor(x => x.Attachment!.ContentType)
                .Must(t => DisputeRules.AllowedContentTypes.Contains(t))
                .WithMessage("Attach a photo or a PDF."));
    }
}

public sealed class UserDisputeHandlers :
    IRequestHandler<RaiseDisputeCommand, Result<UserDisputeSummary>>,
    IRequestHandler<GetMyDisputesQuery, Result<IReadOnlyList<UserDisputeSummary>>>,
    IRequestHandler<GetMyDisputeQuery, Result<UserDisputeDetail>>,
    IRequestHandler<AddDisputeMessageCommand, Result<DisputeThreadMessage>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDisputeEvidenceStorage _storage;
    private readonly IDateTime _dateTime;

    public UserDisputeHandlers(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IDisputeEvidenceStorage storage, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _storage = storage;
        _dateTime = dateTime;
    }

    private Guid Me => _currentUser.UserId!.Value;

    public async Task<Result<UserDisputeSummary>> Handle(RaiseDisputeCommand request, CancellationToken cancellationToken)
    {
        var listing = await (
            from l in _dbContext.Listings
            join s in _dbContext.SellerProfiles on l.SellerId equals s.Id
            where l.Id == request.ListingId && l.Status != ListingStatus.Draft
            select new { l.Id, l.Title, SellerUserId = s.UserId })
            .FirstOrDefaultAsync(cancellationToken);
        if (listing is null)
        {
            return Result.Failure<UserDisputeSummary>(DisputeErrors.ListingNotFound);
        }

        Guid againstUserId;
        Guid? conversationId = null;
        if (Me == listing.SellerUserId)
        {
            // The seller must say which buyer - via their chat about this ad.
            if (request.ConversationId is not { } chatId)
            {
                return Result.Failure<UserDisputeSummary>(DisputeErrors.ConversationRequired);
            }

            var chat = await _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == chatId, cancellationToken);
            if (chat is null || chat.ListingId != listing.Id || chat.SellerId != Me)
            {
                return Result.Failure<UserDisputeSummary>(DisputeErrors.ConversationMismatch);
            }

            againstUserId = chat.BuyerId;
            conversationId = chat.Id;
        }
        else
        {
            againstUserId = listing.SellerUserId;
            var chat = request.ConversationId is { } chatId
                ? await _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == chatId, cancellationToken)
                : await _dbContext.Conversations.FirstOrDefaultAsync(c => c.ListingId == listing.Id && c.BuyerId == Me, cancellationToken);
            if (request.ConversationId is not null && (chat is null || chat.ListingId != listing.Id || chat.BuyerId != Me))
            {
                return Result.Failure<UserDisputeSummary>(DisputeErrors.ConversationMismatch);
            }

            conversationId = chat?.Id;
        }

        var alreadyOpen = await _dbContext.Disputes.AnyAsync(
            d => d.ListingId == listing.Id && d.Status != DisputeStatus.Resolved
                 && ((d.RaisedByUserId == Me && d.AgainstUserId == againstUserId)
                     || (d.RaisedByUserId == againstUserId && d.AgainstUserId == Me)),
            cancellationToken);
        if (alreadyOpen)
        {
            return Result.Failure<UserDisputeSummary>(DisputeErrors.AlreadyOpen);
        }

        var keys = new List<string>();
        foreach (var file in request.Evidence)
        {
            keys.Add(await _storage.UploadAsync(file.FileName, file.ContentType, file.Content, cancellationToken));
        }

        var reference = DisputeRules.NewReference();
        while (await _dbContext.Disputes.AnyAsync(d => d.Reference == reference, cancellationToken))
        {
            reference = DisputeRules.NewReference();
        }

        var now = _dateTime.UtcNow;
        var dispute = Dispute.Raise(reference, listing.Id, conversationId, Me, againstUserId, request.Reason,
            request.Description.Trim(), keys, now);
        _dbContext.Disputes.Add(dispute);
        _dbContext.InboxNotifications.Add(InboxNotification.Create(
            againstUserId, InboxNotificationTypes.DisputeOpened, $"Dispute {reference} opened about “{listing.Title}”",
            $"{DisputeRules.ReasonLabel(request.Reason)}. Tap to see the details and give your side.",
            listing.Id, null, now, dispute.Id));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success((await SummariesAsync(d => d.Id == dispute.Id, cancellationToken)).Single());
    }

    public async Task<Result<IReadOnlyList<UserDisputeSummary>>> Handle(GetMyDisputesQuery request, CancellationToken cancellationToken) =>
        Result.Success(await SummariesAsync(d => d.RaisedByUserId == Me || d.AgainstUserId == Me, cancellationToken));

    public async Task<Result<UserDisputeDetail>> Handle(GetMyDisputeQuery request, CancellationToken cancellationToken)
    {
        var dispute = await _dbContext.Disputes.FirstOrDefaultAsync(d => d.Id == request.DisputeId, cancellationToken);
        if (dispute is null || !dispute.IsParty(Me))
        {
            return Result.Failure<UserDisputeDetail>(DisputeErrors.NotFound);
        }

        var summary = (await SummariesAsync(d => d.Id == dispute.Id, cancellationToken)).Single();
        var messages = await ThreadAsync(dispute.Id, cancellationToken);

        return Result.Success(new UserDisputeDetail(
            summary,
            dispute.Description,
            dispute.EvidenceKeys.Select(ToEvidence).ToList(),
            dispute.ResolutionNote,
            dispute.ResolvedAt,
            messages));
    }

    public async Task<Result<DisputeThreadMessage>> Handle(AddDisputeMessageCommand request, CancellationToken cancellationToken)
    {
        var dispute = await _dbContext.Disputes.FirstOrDefaultAsync(d => d.Id == request.DisputeId, cancellationToken);
        if (dispute is null || !dispute.IsParty(Me))
        {
            return Result.Failure<DisputeThreadMessage>(DisputeErrors.NotFound);
        }

        if (dispute.Status == DisputeStatus.Resolved)
        {
            return Result.Failure<DisputeThreadMessage>(DisputeErrors.Resolved);
        }

        string? attachmentKey = null;
        if (request.Attachment is { } file)
        {
            attachmentKey = await _storage.UploadAsync(file.FileName, file.ContentType, file.Content, cancellationToken);
        }

        var now = _dateTime.UtcNow;
        var role = Me == dispute.RaisedByUserId ? DisputeAuthorRole.Raiser : DisputeAuthorRole.Respondent;
        var message = DisputeMessage.Create(dispute.Id, Me, role, request.Body.Trim(), attachmentKey, now);
        _dbContext.DisputeMessages.Add(message);
        dispute.Touch(now);

        var other = Me == dispute.RaisedByUserId ? dispute.AgainstUserId : dispute.RaisedByUserId;
        _dbContext.InboxNotifications.Add(InboxNotification.Create(
            other, InboxNotificationTypes.DisputeUpdate, $"New reply on dispute {dispute.Reference}",
            Truncate(request.Body.Trim()), dispute.ListingId, null, now, dispute.Id));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success((await ThreadAsync(dispute.Id, cancellationToken)).Single(m => m.Id == message.Id));
    }

    private async Task<IReadOnlyList<UserDisputeSummary>> SummariesAsync(
        System.Linq.Expressions.Expression<Func<Dispute, bool>> filter, CancellationToken cancellationToken)
    {
        var rows = await (
            from d in _dbContext.Disputes.Where(filter)
            join l in _dbContext.Listings on d.ListingId equals l.Id
            join raiser in _dbContext.Users on d.RaisedByUserId equals raiser.Id
            join against in _dbContext.Users on d.AgainstUserId equals against.Id
            orderby d.CreatedAt descending, d.Id descending
            select new { d, l.Title, l.MediaUrls, RaiserName = raiser.Name, AgainstName = against.Name })
            .ToListAsync(cancellationToken);

        return rows.Select(r =>
        {
            var iRaised = r.d.RaisedByUserId == Me;
            return new UserDisputeSummary(
                r.d.Id,
                r.d.Reference,
                r.d.ListingId,
                r.Title,
                r.MediaUrls.FirstOrDefault(),
                iRaised ? DisputeAuthorRole.Raiser : DisputeAuthorRole.Respondent,
                (iRaised ? r.AgainstName : r.RaiserName) ?? "Ogasela user",
                r.d.Reason,
                r.d.Status,
                r.d.Outcome,
                r.d.CreatedAt,
                r.d.UpdatedAt);
        }).ToList();
    }

    private async Task<IReadOnlyList<DisputeThreadMessage>> ThreadAsync(Guid disputeId, CancellationToken cancellationToken)
    {
        var rows = await (
            from m in _dbContext.DisputeMessages
            where m.DisputeId == disputeId
            join u in _dbContext.Users on m.AuthorUserId equals u.Id
            orderby m.CreatedAt
            select new { m, u.Name })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new DisputeThreadMessage(
                r.m.Id,
                r.m.AuthorRole,
                r.m.AuthorRole == DisputeAuthorRole.Staff ? "Ogasela support" : (r.Name ?? "Ogasela user"),
                r.m.Body,
                r.m.AttachmentKey is null ? null : ToEvidence(r.m.AttachmentKey),
                r.m.CreatedAt))
            .ToList();
    }

    private DisputeEvidence ToEvidence(string key) =>
        new(_storage.GetImageUrl(key), key.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

    private static string Truncate(string text) => text.Length <= 120 ? text : text[..119].TrimEnd() + "…";
}
