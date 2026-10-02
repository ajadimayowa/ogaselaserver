using Ogasela.Shared;

namespace Ogasela.Application.Marketing.Announcements;

public static class AnnouncementErrors
{
    public static readonly Error NotFound = new("Announcement.NotFound", "The announcement could not be found.");
}
