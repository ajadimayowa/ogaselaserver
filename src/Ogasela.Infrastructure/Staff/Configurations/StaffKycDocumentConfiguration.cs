using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Staff;

namespace Ogasela.Infrastructure.Staff.Configurations;

public sealed class StaffKycDocumentConfiguration : IEntityTypeConfiguration<StaffKycDocument>
{
    public void Configure(EntityTypeBuilder<StaffKycDocument> builder)
    {
        builder.ToTable("StaffKycDocuments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DocumentType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(d => d.StorageKey).IsRequired();
        builder.Property(d => d.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.UploadedAt).IsRequired();

        builder.HasIndex(d => d.StaffProfileId);
    }
}
