using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Marketing;

namespace Ogasela.Infrastructure.Marketing.Configurations;

public sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("Announcements");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(120).IsRequired();
        builder.Property(a => a.Caption).HasMaxLength(500).IsRequired();
        builder.Property(a => a.ImageS3Key).HasMaxLength(500).IsRequired();
        builder.Property(a => a.CtaLabel).HasMaxLength(40);
        builder.Property(a => a.CtaUrl).HasMaxLength(500);
        builder.HasIndex(a => new { a.IsActive, a.CreatedAt });
    }
}
