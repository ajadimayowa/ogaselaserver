using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Messaging;

namespace Ogasela.Infrastructure.Messaging.Configurations;

public sealed class BlockedUserConfiguration : IEntityTypeConfiguration<BlockedUser>
{
    public void Configure(EntityTypeBuilder<BlockedUser> builder)
    {
        builder.ToTable("BlockedUsers");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.CreatedAt).IsRequired();

        builder.HasIndex(b => new { b.BlockerId, b.BlockedId }).IsUnique();
    }
}
