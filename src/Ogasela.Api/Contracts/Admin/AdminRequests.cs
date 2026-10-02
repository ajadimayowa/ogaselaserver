using Ogasela.Domain.Moderation;

namespace Ogasela.Api.Contracts.Admin;

public sealed record ResolveReportRequest(ReportDecision Decision, string? Notes);

public sealed record RejectListingRequest(string Reason);
