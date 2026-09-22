using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Rbac;

namespace Ogasela.Infrastructure.Rbac.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(150).IsRequired();
        builder.Property(r => r.RoleType).HasConversion<string>().HasMaxLength(32).IsRequired();

        // Npgsql maps List<string> to a native Postgres text[] column - simpler than a join
        // table, and consistent with this being an application-validated array (against the
        // Permission catalog) rather than a foreign-key-enforced relationship.
        builder.Property(r => r.PermissionKeys).HasColumnType("text[]").IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasIndex(r => r.Name).IsUnique();
    }
}
