using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Rbac;

namespace Ogasela.Infrastructure.Rbac.Configurations;

public sealed class OrgUnitConfiguration : IEntityTypeConfiguration<OrgUnit>
{
    public void Configure(EntityTypeBuilder<OrgUnit> builder)
    {
        builder.ToTable("Units");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Description).HasMaxLength(1000);
        builder.Property(u => u.CreatedAt).IsRequired();

        builder.HasIndex(u => u.DepartmentId);
        builder.HasIndex(u => new { u.DepartmentId, u.Name }).IsUnique();
    }
}
