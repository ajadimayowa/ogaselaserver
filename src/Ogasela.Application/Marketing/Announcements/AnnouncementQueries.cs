using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Marketing.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Marketing.Announcements;

/// <summary>Public - active announcements, newest first. The app shows the newest one this device hasn't closed yet.</summary>
public sealed record GetActiveAnnouncementsQuery : IRequest<Result<IReadOnlyList<AnnouncementResponse>>>;

/// <summary>Control Portal - every announcement, newest first.</summary>
public sealed record GetAllAnnouncementsQuery : IRequest<Result<IReadOnlyList<AnnouncementResponse>>>;

public sealed class AnnouncementQueryHandlers :
    IRequestHandler<GetActiveAnnouncementsQuery, Result<IReadOnlyList<AnnouncementResponse>>>,
    IRequestHandler<GetAllAnnouncementsQuery, Result<IReadOnlyList<AnnouncementResponse>>>
{
    private const int MaxActive = 10;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAnnouncementImageStorage _imageStorage;

    public AnnouncementQueryHandlers(IApplicationDbContext dbContext, IAnnouncementImageStorage imageStorage)
    {
        _dbContext = dbContext;
        _imageStorage = imageStorage;
    }

    public async Task<Result<IReadOnlyList<AnnouncementResponse>>> Handle(
        GetActiveAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        var active = await _dbContext.Announcements
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .Take(MaxActive)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AnnouncementResponse>>(
            active.Select(a => AnnouncementResponse.From(a, _imageStorage.GetImageUrl(a.ImageS3Key))).ToList());
    }

    public async Task<Result<IReadOnlyList<AnnouncementResponse>>> Handle(
        GetAllAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        var all = await _dbContext.Announcements
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AnnouncementResponse>>(
            all.Select(a => AnnouncementResponse.From(a, _imageStorage.GetImageUrl(a.ImageS3Key))).ToList());
    }
}
