using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Notifications;

namespace Ogasela.Infrastructure.Notifications.Configurations;

public sealed class InboxNotificationConfiguration : IEntityTypeConfiguration<InboxNotification>
{
    public void Configure(EntityTypeBuilder<InboxNotification> builder)
    {
        builder.ToTable("InboxNotifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(150).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(500).IsRequired();
        builder.Ignore(n => n.IsRead);
        builder.HasIndex(n => new { n.UserId, n.UpdatedAt });
        builder.HasIndex(n => new { n.UserId, n.ReadAt });
    }
}
