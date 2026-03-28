using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.Entities.GameData.Motorfest;

namespace TheCrewCommunity.Data.TableConfiguration.GameData.Motorfest;

public class MotorfestVehicleEngineTypeConfig : IEntityTypeConfiguration<MotorfestVehicleEngineType>
{
    public void Configure(EntityTypeBuilder<MotorfestVehicleEngineType> builder)
    {
        builder.HasKey(engineType => engineType.Id);
        builder.Property(engineType => engineType.Name)
            .IsRequired()
            .HasMaxLength(20);
        builder.HasIndex(engineType => engineType.Name).IsUnique();
    }
}