using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Messaging;

namespace Ogasela.Infrastructure.Messaging.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content).HasMaxLength(4000).IsRequired();
        builder.Property(m => m.ImageUrl).HasMaxLength(1000);
        builder.Property(m => m.SentAt).IsRequired();

        builder.HasIndex(m => new { m.ConversationId, m.SentAt });
    }
}
