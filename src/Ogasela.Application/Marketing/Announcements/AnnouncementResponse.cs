using Ogasela.Domain.Marketing;

namespace Ogasela.Application.Marketing.Announcements;

/// <summary>What the app's pop-up renders. Cta is null when the announcement has no button.</summary>
public sealed record AnnouncementResponse(
    Guid Id,
    string Title,
    string Caption,
    string Image,
    AnnouncementCta? Cta,
    bool IsActive,
    DateTime CreatedAt)
{
    public static AnnouncementResponse From(Announcement announcement, string imageUrl) => new(
        announcement.Id,
        announcement.Title,
        announcement.Caption,
        imageUrl,
        announcement.CtaLabel is null || announcement.CtaUrl is null
            ? null
            : new AnnouncementCta(announcement.CtaLabel, announcement.CtaUrl),
        announcement.IsActive,
        announcement.CreatedAt);
}

public sealed record AnnouncementCta(string Label, string Url);
