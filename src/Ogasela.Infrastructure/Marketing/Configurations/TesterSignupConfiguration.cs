using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Marketing;

namespace Ogasela.Infrastructure.Marketing.Configurations;

public sealed class TesterSignupConfiguration : IEntityTypeConfiguration<TesterSignup>
{
    public void Configure(EntityTypeBuilder<TesterSignup> builder)
    {
        builder.ToTable("TesterSignups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
