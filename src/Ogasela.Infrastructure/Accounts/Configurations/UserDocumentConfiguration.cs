using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Accounts;

namespace Ogasela.Infrastructure.Accounts.Configurations;

public sealed class UserDocumentConfiguration : IEntityTypeConfiguration<UserDocument>
{
    public void Configure(EntityTypeBuilder<UserDocument> builder)
    {
        builder.ToTable("UserDocuments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(d => d.IdType).HasConversion<string>().HasMaxLength(32);
        builder.Property(d => d.IdNumber).HasMaxLength(40);
        builder.Property(d => d.FileS3Key).HasMaxLength(500).IsRequired();
        builder.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(d => d.ReviewNote).HasMaxLength(500);
        builder.HasIndex(d => new { d.UserId, d.Type, d.CreatedAt });
    }
}
