using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Admin.GetAuditLog;

public sealed record GetAuditLogQuery(
    Guid? ActorId, string? Action, DateTime? From, DateTime? To, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedAuditLogResponse>>;

public sealed record AuditLogEntryResponse(
    Guid Id, Guid ActorId, string Action, string TargetType, Guid TargetId, string? BeforeJson, string? AfterJson, DateTime Timestamp);

public sealed record PagedAuditLogResponse(IReadOnlyList<AuditLogEntryResponse> Items, int Page, int PageSize, int TotalCount);
