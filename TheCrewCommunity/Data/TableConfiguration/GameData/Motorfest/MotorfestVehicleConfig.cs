using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.Entities.GameData.Motorfest;

namespace TheCrewCommunity.Data.TableConfiguration.GameData.Motorfest;

public class MotorfestVehicleConfig : IEntityTypeConfiguration<MotorfestVehicle>
{
    public void Configure(EntityTypeBuilder<MotorfestVehicle> builder)
    {
        builder.ToTable("motorfest_vehicles");
        builder.HasKey(mv => mv.Id);
        builder.HasOne(vehicle=>vehicle.Brand)
            .WithMany()
            .HasForeignKey(vehicle=>vehicle.BrandId);
        builder.HasOne(vehicle=>vehicle.EngineType)
            .WithMany()
            .HasForeignKey(vehicle=>vehicle.EngineTypeId);
        builder.HasOne(vehicle=>vehicle.Category)
            .WithMany()
            .HasForeignKey(vehicle=>vehicle.CategoryId);
        builder.HasOne(vehicle=>vehicle.Country)
            .WithMany()
            .HasForeignKey(vehicle=>vehicle.CountryId);
        builder.HasOne(vehicle=>vehicle.Period)
            .WithMany()
            .HasForeignKey(vehicle=>vehicle.PeriodId);
        builder.HasOne(vehicle=>vehicle.Style)
            .WithMany()
            .HasForeignKey(vehicle=>vehicle.StyleId);
        builder.HasOne(vehicle=>vehicle.Type)
            .WithMany()
            .HasForeignKey(vehicle=>vehicle.TypeId);
        builder.HasOne(vehicle=>vehicle.Tag)
            .WithMany()
            .HasForeignKey(vehicle=>vehicle.TagId);
            
        builder.Property(vehicle=>vehicle.ModelName)
            .IsRequired()
            .HasMaxLength(80);
        builder.Property(vehicle => vehicle.CountryId)
            .HasMaxLength(2);
    }
}