namespace Ogasela.Api.Contracts.Messaging;

public sealed record StartConversationRequest(Guid ListingId);

public sealed record SendMessageRequest(string Content, string? ImageUrl);

public sealed record ReportUserRequest(Guid TargetUserId, string Reason);
