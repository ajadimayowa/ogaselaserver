using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Marketing;

namespace Ogasela.Infrastructure.Marketing.Configurations;

public sealed class AccountDeletionRequestConfiguration : IEntityTypeConfiguration<AccountDeletionRequest>
{
    public void Configure(EntityTypeBuilder<AccountDeletionRequest> builder)
    {
        builder.ToTable("AccountDeletionRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.RequestType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(5000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.CreatedAt);
    }
}
