using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Staff;

namespace Ogasela.Infrastructure.Staff.Configurations;

public sealed class StaffProfileConfiguration : IEntityTypeConfiguration<StaffProfile>
{
    public void Configure(EntityTypeBuilder<StaffProfile> builder)
    {
        builder.ToTable("StaffProfiles");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(s => s.RejectionReason).HasMaxLength(1000);
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasIndex(s => s.UserId).IsUnique();
        builder.HasIndex(s => s.DepartmentId);
        builder.HasIndex(s => s.UnitId);
        builder.HasIndex(s => s.RoleId);
        builder.HasIndex(s => s.Status);
    }
}
