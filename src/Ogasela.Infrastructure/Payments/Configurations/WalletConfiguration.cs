using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Payments;

namespace Ogasela.Infrastructure.Payments.Configurations;

public sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.BalanceKobo).HasPrecision(18, 2).IsRequired();
        builder.Property(w => w.UpdatedAt).IsRequired();

        builder.HasIndex(w => w.SellerId).IsUnique();

        // Guards concurrent debits/credits (a plan purchase racing a webhook credit, or two
        // webhook deliveries) from silently overwriting each other's balance change: Postgres's
        // built-in "xmin" system column changes on every row update, so mapping it as a shadow
        // concurrency token makes EF fail the UPDATE if the row changed since it was read.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
