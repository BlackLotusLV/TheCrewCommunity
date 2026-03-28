using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.Entities.GameData.Motorfest;

namespace TheCrewCommunity.Data.TableConfiguration.GameData.Motorfest;

public class MotorfestVehiclePeriodConfig : IEntityTypeConfiguration<MotorfestVehiclePeriod>
{
    public void Configure(EntityTypeBuilder<MotorfestVehiclePeriod> builder)
    {
        builder.ToTable("motorfest_vehicle_period");
        builder.HasKey(period => period.Id);
        builder.Property(period => period.Name)
            .IsRequired()
            .HasMaxLength(20);
        builder.HasIndex(period => period.Name).IsUnique();
    }
}