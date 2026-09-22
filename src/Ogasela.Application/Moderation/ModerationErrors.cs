using Ogasela.Shared;

namespace Ogasela.Application.Moderation;

public static class ModerationErrors
{
    public static readonly Error ReportNotFound = new(
        "Moderation.ReportNotFound", "The report could not be found.");

    public static readonly Error ReportAlreadyDecided = new(
        "Moderation.ReportAlreadyDecided", "This report has already been resolved or escalated.");
}
