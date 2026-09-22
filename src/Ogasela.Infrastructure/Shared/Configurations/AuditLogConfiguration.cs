using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Shared;

namespace Ogasela.Infrastructure.Shared.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.TargetType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Timestamp).IsRequired();

        builder.HasIndex(a => new { a.TargetType, a.TargetId });
        builder.HasIndex(a => a.ActorId);
    }
}
