namespace Ogasela.Domain.Settings;

/// <summary>
/// One SuperAdmin-editable platform setting (Control Portal → Settings), stored as text under a dotted
/// key, e.g. "listings.requireApproval". A key with no row falls back to its default from appsettings.
/// </summary>
public class PlatformSetting
{
    private PlatformSetting()
    {
    }

    public string Key { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public Guid? UpdatedByUserId { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static PlatformSetting Create(string key, string value, Guid? updatedByUserId, DateTime now) => new()
    {
        Key = key,
        Value = value,
        UpdatedByUserId = updatedByUserId,
        UpdatedAt = now
    };

    public void Set(string value, Guid? updatedByUserId, DateTime now)
    {
        Value = value;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = now;
    }
}
