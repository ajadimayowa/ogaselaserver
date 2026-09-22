using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Notifications;

namespace Ogasela.Infrastructure.Notifications.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Channel).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(100).IsRequired();

        builder.HasIndex(p => new { p.UserId, p.Channel, p.Category }).IsUnique();
    }
}
