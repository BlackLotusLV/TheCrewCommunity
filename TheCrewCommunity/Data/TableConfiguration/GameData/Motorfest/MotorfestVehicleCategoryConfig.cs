using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.Entities.GameData.Motorfest;

namespace TheCrewCommunity.Data.TableConfiguration.GameData.Motorfest;

public class MotorfestVehicleCategoryConfig : IEntityTypeConfiguration<MotorfestVehicleCategory>
{
    public void Configure(EntityTypeBuilder<MotorfestVehicleCategory> builder)
    {
        builder.ToTable("motorfest_vehicle_category");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(30);
        builder.HasIndex(category => category.Name).IsUnique();
    }
}