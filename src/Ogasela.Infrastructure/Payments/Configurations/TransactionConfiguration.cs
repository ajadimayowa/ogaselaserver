using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Payments;

namespace Ogasela.Infrastructure.Payments.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(t => t.AmountKobo).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.GatewayReference).HasMaxLength(200);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => t.SellerId);
        builder.HasIndex(t => t.GatewayReference).IsUnique();

        // A webhook's first delivery flips Pending -> Success/Failed; the "xmin" shadow
        // concurrency token (see WalletConfiguration) catches a second, concurrent delivery
        // trying to do the same update so it can be treated as a no-op instead of
        // double-crediting the wallet.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
