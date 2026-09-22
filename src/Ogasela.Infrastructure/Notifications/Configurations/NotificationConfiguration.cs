using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Notifications;

namespace Ogasela.Infrastructure.Notifications.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(n => n.Type).HasMaxLength(100).IsRequired();
        builder.Property(n => n.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(n => n.CreatedAt).IsRequired();

        builder.HasIndex(n => new { n.UserId, n.CreatedAt });
    }
}
