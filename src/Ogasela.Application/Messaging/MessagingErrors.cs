using Ogasela.Shared;

namespace Ogasela.Application.Messaging;

public static class MessagingErrors
{
    public static readonly Error ListingNotFound = new(
        "Messaging.ListingNotFound", "The listing could not be found.");

    public static readonly Error CannotMessageOwnListing = new(
        "Messaging.CannotMessageOwnListing", "You cannot start a conversation about your own listing.");

    public static readonly Error ConversationNotFound = new(
        "Messaging.ConversationNotFound", "The conversation could not be found.");

    public static readonly Error NotParticipant = new(
        "Messaging.NotParticipant", "You are not a participant in this conversation.");

    public static readonly Error Blocked = new(
        "Messaging.Blocked", "You cannot message this user.");

    public static readonly Error CannotBlockSelf = new(
        "Messaging.CannotBlockSelf", "You cannot block yourself.");

    public static readonly Error CannotReportSelf = new(
        "Messaging.CannotReportSelf", "You cannot report yourself.");
}
