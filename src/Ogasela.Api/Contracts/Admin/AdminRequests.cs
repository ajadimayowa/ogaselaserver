using Ogasela.Domain.Moderation;

namespace Ogasela.Api.Contracts.Admin;

public sealed record ResolveReportRequest(ReportDecision Decision, string? Notes);
