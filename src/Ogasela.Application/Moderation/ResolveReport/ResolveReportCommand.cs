using MediatR;
using Ogasela.Domain.Moderation;
using Ogasela.Shared;

namespace Ogasela.Application.Moderation.ResolveReport;

public sealed record ResolveReportCommand(Guid ReportId, ReportDecision Decision, string? Notes) : IRequest<Result>;
