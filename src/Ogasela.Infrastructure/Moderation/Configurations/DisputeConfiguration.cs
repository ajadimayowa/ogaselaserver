using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Moderation;

namespace Ogasela.Infrastructure.Moderation.Configurations;

public sealed class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.ToTable("Disputes");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Reference).HasMaxLength(20).IsRequired();
        builder.HasIndex(d => d.Reference).IsUnique();
        builder.Property(d => d.Reason).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(2000).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.Outcome).HasConversion<string>().HasMaxLength(40);
        builder.Property(d => d.ResolutionNote).HasMaxLength(2000);
        builder.HasIndex(d => new { d.Status, d.CreatedAt });
        builder.HasIndex(d => d.RaisedByUserId);
        builder.HasIndex(d => d.AgainstUserId);
    }
}

public sealed class DisputeMessageConfiguration : IEntityTypeConfiguration<DisputeMessage>
{
    public void Configure(EntityTypeBuilder<DisputeMessage> builder)
    {
        builder.ToTable("DisputeMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.AuthorRole).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.Body).HasMaxLength(2000).IsRequired();
        builder.Property(m => m.AttachmentKey).HasMaxLength(500);
        builder.HasIndex(m => new { m.DisputeId, m.CreatedAt });
    }
}
