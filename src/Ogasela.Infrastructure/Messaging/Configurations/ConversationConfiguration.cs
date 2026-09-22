using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Messaging;

namespace Ogasela.Infrastructure.Messaging.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.LastMessageAt).IsRequired();

        // Enforces "no duplicate conversations for the same buyer/seller/listing triple" at the
        // data layer too, not just in StartConversationCommandHandler's own check.
        builder.HasIndex(c => new { c.ListingId, c.BuyerId, c.SellerId }).IsUnique();

        builder.HasIndex(c => c.BuyerId);
        builder.HasIndex(c => c.SellerId);
    }
}
