using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Admin.GetAuditLog;

public sealed class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, Result<PagedAuditLogResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetAuditLogQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedAuditLogResponse>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.AuditLogs.AsQueryable();

        if (request.ActorId is { } actorId)
        {
            query = query.Where(a => a.ActorId == actorId);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(a => a.Action == request.Action);
        }

        if (request.From is { } from)
        {
            query = query.Where(a => a.Timestamp >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(a => a.Timestamp <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogEntryResponse(
                a.Id, a.ActorId, a.Action, a.TargetType, a.TargetId, a.BeforeJson, a.AfterJson, a.Timestamp))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedAuditLogResponse(items, page, pageSize, totalCount));
    }
}
