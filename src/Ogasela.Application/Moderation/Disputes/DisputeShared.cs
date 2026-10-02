using System.Security.Cryptography;
using Ogasela.Domain.Moderation;
using Ogasela.Shared;

namespace Ogasela.Application.Moderation.Disputes;

/// <summary>A file attached to a dispute (evidence on opening, or an attachment on a reply).</summary>
public sealed record DisputeFile(Stream Content, string FileName, string ContentType);

public static class DisputeRules
{
    public const int MaxEvidenceFiles = 4;

    public static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/heic", "image/webp", "application/pdf"];

    // No 0/O/1/I, so a reference read out over the phone can't be misheard.
    private const string ReferenceAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string NewReference() =>
        "DSP-" + new string(Enumerable.Range(0, 6).Select(_ => ReferenceAlphabet[RandomNumberGenerator.GetInt32(ReferenceAlphabet.Length)]).ToArray());

    public static string ReasonLabel(DisputeReason reason) => reason switch
    {
        DisputeReason.ItemNotAsDescribed => "Item not as described",
        DisputeReason.NotDelivered => "Item not delivered",
        DisputeReason.PaidNoResponse => "Paid, but the seller stopped responding",
        DisputeReason.FakeOrCounterfeit => "Fake or counterfeit item",
        DisputeReason.BuyerDidNotPay => "Buyer didn't pay",
        _ => "Other"
    };

    public static string OutcomeLabel(DisputeOutcome outcome) => outcome switch
    {
        DisputeOutcome.InFavourOfRaiser => "Resolved in favour of the person who opened it",
        DisputeOutcome.InFavourOfRespondent => "Resolved in favour of the other party",
        DisputeOutcome.MutualAgreement => "Resolved by mutual agreement",
        _ => "Dismissed"
    };
}

public static class DisputeErrors
{
    public static readonly Error NotFound = new("Dispute.NotFound", "This dispute could not be found.");
    public static readonly Error ListingNotFound = new("Dispute.ListingNotFound", "That ad could not be found.");
    public static readonly Error ConversationRequired = new(
        "Dispute.ConversationRequired", "Open the dispute from your chat with the buyer, so we know who it's about.");
    public static readonly Error ConversationMismatch = new("Dispute.ConversationMismatch", "That chat isn't about this ad.");
    public static readonly Error AlreadyOpen = new(
        "Dispute.AlreadyOpen", "There's already an open dispute about this deal. Add to it instead of opening a new one.");
    public static readonly Error Resolved = new("Dispute.Resolved", "This dispute has been resolved and is closed for new messages.");
    public static readonly Error NotAParty = new("Dispute.NotAParty", "You can only suspend one of the two people in this dispute.");
}
