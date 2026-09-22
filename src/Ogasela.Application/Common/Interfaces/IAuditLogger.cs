namespace Ogasela.Application.Common.Interfaces;

/// <summary>
/// Records an administrative action to the shared audit trail. Implementations only stage the
/// <c>AuditLog</c> row on the current <see cref="IApplicationDbContext"/> - they do not call
/// <c>SaveChangesAsync</c> themselves - so the audit entry commits atomically together with
/// whatever entity change it documents, as part of the caller's own SaveChanges call.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(
        Guid actorId,
        string action,
        string targetType,
        Guid targetId,
        string? beforeJson,
        string? afterJson,
        CancellationToken cancellationToken);
}
