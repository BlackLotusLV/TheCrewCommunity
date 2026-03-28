using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.Entities.GameData.Motorfest;

namespace TheCrewCommunity.Data.TableConfiguration.GameData.Motorfest;

public class MotorfestVehicleTypeConfig : IEntityTypeConfiguration<MotorfestVehicleType>
{
    public void Configure(EntityTypeBuilder<MotorfestVehicleType> builder)
    {
        builder.ToTable("motorfest_vehicle_type");
        builder.HasKey(type => type.Id);
        
        builder.Property(type => type.Name)
            .IsRequired()
            .HasMaxLength(20);
        builder.HasIndex(type => type.Name).IsUnique();
    }
}