using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Geo;

namespace Ogasela.Infrastructure.Geo.Configurations;

public sealed class NigeriaStateConfiguration : IEntityTypeConfiguration<NigeriaState>
{
    public void Configure(EntityTypeBuilder<NigeriaState> builder)
    {
        builder.ToTable("NigeriaStates");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Code).HasMaxLength(10).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasIndex(s => s.Name).IsUnique();
        builder.HasIndex(s => s.Code).IsUnique();
    }
}
