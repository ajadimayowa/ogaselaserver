using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Marketing.Interfaces;
using Ogasela.Domain.Marketing;
using Ogasela.Shared;

namespace Ogasela.Application.Marketing.Announcements;

public sealed record CreateAnnouncementCommand(
    string Title,
    string Caption,
    Stream Image,
    string ImageFileName,
    string ImageContentType,
    string? CtaLabel,
    string? CtaUrl,
    bool IsActive) : IRequest<Result<AnnouncementResponse>>;

public sealed record SetAnnouncementActiveCommand(Guid Id, bool IsActive) : IRequest<Result>;

public sealed record DeleteAnnouncementCommand(Guid Id) : IRequest<Result>;

public sealed class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
{
    public CreateAnnouncementCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Caption).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ImageContentType)
            .Must(type => type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            .WithMessage("The image must be an image file.");

        // The button is optional, but its label and link come as a pair.
        RuleFor(x => x)
            .Must(x => string.IsNullOrWhiteSpace(x.CtaLabel) == string.IsNullOrWhiteSpace(x.CtaUrl))
            .WithMessage("Give the button both a label and a link, or leave both empty.");
        When(x => !string.IsNullOrWhiteSpace(x.CtaLabel) && !string.IsNullOrWhiteSpace(x.CtaUrl), () =>
        {
            RuleFor(x => x.CtaLabel!).MaximumLength(40);
            RuleFor(x => x.CtaUrl!)
                .MaximumLength(500)
                .Must(BeAnAllowedLink)
                .WithMessage("The button link must be a web address (https://...) or an app link (ogasela://...).");
        });
    }

    private static bool BeAnAllowedLink(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == "ogasela");
}

public sealed class AnnouncementCommandHandlers :
    IRequestHandler<CreateAnnouncementCommand, Result<AnnouncementResponse>>,
    IRequestHandler<SetAnnouncementActiveCommand, Result>,
    IRequestHandler<DeleteAnnouncementCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAnnouncementImageStorage _imageStorage;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IDateTime _dateTime;

    public AnnouncementCommandHandlers(
        IApplicationDbContext dbContext, IAnnouncementImageStorage imageStorage, ICurrentUserService currentUser,
        IAuditLogger auditLogger, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _imageStorage = imageStorage;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _dateTime = dateTime;
    }

    public async Task<Result<AnnouncementResponse>> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var imageKey = await _imageStorage.UploadAsync(request.ImageFileName, request.ImageContentType, request.Image, cancellationToken);
        var hasCta = !string.IsNullOrWhiteSpace(request.CtaLabel);

        var announcement = Announcement.Create(
            request.Title.Trim(),
            request.Caption.Trim(),
            imageKey,
            hasCta ? request.CtaLabel!.Trim() : null,
            hasCta ? request.CtaUrl!.Trim() : null,
            request.IsActive,
            _dateTime.UtcNow);

        _dbContext.Announcements.Add(announcement);
        await _auditLogger.LogAsync(
            _currentUser.UserId!.Value, "Announcement.Created", nameof(Announcement), announcement.Id, null,
            JsonSerializer.Serialize(new { announcement.Title, announcement.IsActive }), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(AnnouncementResponse.From(announcement, _imageStorage.GetImageUrl(imageKey)));
    }

    public async Task<Result> Handle(SetAnnouncementActiveCommand request, CancellationToken cancellationToken)
    {
        var announcement = await _dbContext.Announcements.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (announcement is null)
        {
            return Result.Failure(AnnouncementErrors.NotFound);
        }

        announcement.SetActive(request.IsActive, _dateTime.UtcNow);
        await _auditLogger.LogAsync(
            _currentUser.UserId!.Value, request.IsActive ? "Announcement.Activated" : "Announcement.Deactivated",
            nameof(Announcement), announcement.Id, null, null, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await _dbContext.Announcements.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (announcement is null)
        {
            return Result.Failure(AnnouncementErrors.NotFound);
        }

        _dbContext.Announcements.Remove(announcement);
        await _auditLogger.LogAsync(
            _currentUser.UserId!.Value, "Announcement.Deleted", nameof(Announcement), announcement.Id,
            JsonSerializer.Serialize(new { announcement.Title }), null, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await _imageStorage.DeleteAsync(announcement.ImageS3Key, cancellationToken);
        }
        catch (Exception)
        {
            // The record is already gone; an orphaned image file is harmless.
        }

        return Result.Success();
    }
}
