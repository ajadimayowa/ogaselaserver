using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Accounts;

namespace Ogasela.Infrastructure.Accounts.Configurations;

public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("ExternalLogins");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Provider).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.ProviderUserId).HasMaxLength(256).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.Provider }).IsUnique();
    }
}
