namespace Ogasela.Domain.Marketing;

/// <summary>
/// A message the app shows once as a pop-up on the home screen (e.g. a campaign or feature launch),
/// managed from the Control Portal. CtaLabel/CtaUrl are optional and always set together. The app
/// remembers which announcements a device has closed, so each one only pops up once there.
/// </summary>
public class Announcement
{
    private Announcement()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Caption { get; private set; } = string.Empty;

    public string ImageS3Key { get; private set; } = string.Empty;

    public string? CtaLabel { get; private set; }

    public string? CtaUrl { get; private set; }

    /// <summary>Only active announcements are sent to the app.</summary>
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static Announcement Create(
        string title, string caption, string imageS3Key, string? ctaLabel, string? ctaUrl, bool isActive, DateTime now)
    {
        return new Announcement
        {
            Id = Guid.NewGuid(),
            Title = title,
            Caption = caption,
            ImageS3Key = imageS3Key,
            CtaLabel = ctaLabel,
            CtaUrl = ctaUrl,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SetActive(bool isActive, DateTime now)
    {
        IsActive = isActive;
        UpdatedAt = now;
    }
}
