using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.Entities.GameData.Motorfest;

namespace TheCrewCommunity.Data.TableConfiguration.GameData.Motorfest;

public class MotorfestVehicleCountryConfig : IEntityTypeConfiguration<MotorfestVehicleCountry>
{
    public void Configure(EntityTypeBuilder<MotorfestVehicleCountry> builder)
    {
        builder.ToTable("motorfest_vehicle_country");
        builder.HasKey(country => country.Id);
        builder.Property(country => country.Id)
            .HasMaxLength(2);
        builder.Property(country => country.Name)
            .IsRequired()
            .HasMaxLength(30);
        builder.HasIndex(country => country.Name).IsUnique();
    }
}