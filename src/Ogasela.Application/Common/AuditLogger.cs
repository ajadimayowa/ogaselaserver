using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Shared;

namespace Ogasela.Application.Common;

public sealed class AuditLogger : IAuditLogger
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;

    public AuditLogger(IApplicationDbContext dbContext, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
    }

    public Task LogAsync(
        Guid actorId, string action, string targetType, Guid targetId, string? beforeJson, string? afterJson,
        CancellationToken cancellationToken)
    {
        var entry = AuditLog.Create(actorId, action, targetType, targetId, beforeJson, afterJson, _dateTime.UtcNow);
        _dbContext.AuditLogs.Add(entry);
        return Task.CompletedTask;
    }
}
