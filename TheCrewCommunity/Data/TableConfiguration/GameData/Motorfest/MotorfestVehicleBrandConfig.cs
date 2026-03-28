using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.Entities.GameData.Motorfest;

namespace TheCrewCommunity.Data.TableConfiguration.GameData.Motorfest;

public class MotorfestVehicleBrandConfig: IEntityTypeConfiguration<MotorfestVehicleBrand>
{
    public void Configure(EntityTypeBuilder<MotorfestVehicleBrand> builder)
    {
        builder.ToTable("motorfest_vehicle_brand");
        builder.HasKey(brand => brand.Id);
        builder.Property(brand => brand.Name)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(brand => brand.Name).IsUnique();
    }
}