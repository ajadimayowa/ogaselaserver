using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Marketing;

namespace Ogasela.Infrastructure.Marketing.Configurations;

public sealed class ContactFormSubmissionConfiguration : IEntityTypeConfiguration<ContactFormSubmission>
{
    public void Configure(EntityTypeBuilder<ContactFormSubmission> builder)
    {
        builder.ToTable("ContactFormSubmissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(5000).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.CreatedAt);
    }
}
