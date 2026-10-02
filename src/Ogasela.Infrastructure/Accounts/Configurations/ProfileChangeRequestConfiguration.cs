using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Accounts;

namespace Ogasela.Infrastructure.Accounts.Configurations;

public sealed class ProfileChangeRequestConfiguration : IEntityTypeConfiguration<ProfileChangeRequest>
{
    public void Configure(EntityTypeBuilder<ProfileChangeRequest> builder)
    {
        builder.ToTable("ProfileChangeRequests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(r => r.NewValue).HasMaxLength(256);
        builder.Property(r => r.BusinessName).HasMaxLength(200);
        builder.Property(r => r.StoreAddress).HasMaxLength(300);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(r => r.ReviewNote).HasMaxLength(500);
        builder.HasIndex(r => new { r.UserId, r.Type, r.Status });
        builder.HasIndex(r => new { r.Status, r.CreatedAt });
    }
}
