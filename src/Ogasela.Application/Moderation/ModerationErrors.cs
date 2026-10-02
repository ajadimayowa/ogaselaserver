using Ogasela.Shared;

namespace Ogasela.Application.Moderation;

public static class ModerationErrors
{
    public static readonly Error ReportNotFound = new(
        "Moderation.ReportNotFound", "The report could not be found.");

    public static readonly Error ReportAlreadyDecided = new(
        "Moderation.ReportAlreadyDecided", "This report has already been resolved or escalated.");

    public static readonly Error ListingNotPendingReview = new(
        "Moderation.ListingNotPendingReview", "This ad isn't waiting for approval any more.");

    public static readonly Error CannotReportOwnListing = new(
        "Moderation.CannotReportOwnListing", "You can't report your own ad.");
}
