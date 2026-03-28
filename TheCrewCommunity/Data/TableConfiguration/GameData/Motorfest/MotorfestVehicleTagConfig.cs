using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.Entities.GameData.Motorfest;

namespace TheCrewCommunity.Data.TableConfiguration.GameData.Motorfest;

public class MotorfestVehicleTagConfig : IEntityTypeConfiguration<MotorfestVehicleTag>
{
    public void Configure(EntityTypeBuilder<MotorfestVehicleTag> builder)
    {
        builder.ToTable("motorfest_vehicle_tag");
        builder.HasKey(tag => tag.Id);
        builder.Property(tag => tag.Name)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(tag => tag.Name).IsUnique();
    }
}