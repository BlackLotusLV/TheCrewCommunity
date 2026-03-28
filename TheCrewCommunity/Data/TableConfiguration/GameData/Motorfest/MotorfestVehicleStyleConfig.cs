using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.Entities.GameData.Motorfest;

namespace TheCrewCommunity.Data.TableConfiguration.GameData.Motorfest;

public class MotorfestVehicleStyleConfig : IEntityTypeConfiguration<MotorfestVehicleStyle>
{
    public void Configure(EntityTypeBuilder<MotorfestVehicleStyle> builder)
    {
        builder.ToTable("motorfest_vehicle_style");
        builder.HasKey(style => style.Id);
        builder.Property(style => style.Name)
            .IsRequired()
            .HasMaxLength(20);
        builder.HasIndex(style => style.Name).IsUnique();
    }
}