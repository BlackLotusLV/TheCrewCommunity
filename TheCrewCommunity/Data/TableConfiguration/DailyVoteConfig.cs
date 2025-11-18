using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.WebData.ThisOrThat;

namespace TheCrewCommunity.Data.TableConfiguration;

public class DailyVoteConfig : IEntityTypeConfiguration<DailyVote>
{
    public void Configure(EntityTypeBuilder<DailyVote> builder)
    {
        builder.HasKey(dv => dv.Id);
        builder.HasOne(dv=>dv.VehicleSuggestion1)
            .WithMany()
            .HasForeignKey(dv=>dv.VehicleSuggestion1Id);
        builder.HasOne(dv=>dv.VehicleSuggestion2)
            .WithMany()
            .HasForeignKey(dv=>dv.VehicleSuggestion2Id);

        builder.HasIndex(dv => dv.Date).IsUnique();
        builder.Property(x=>x.IsPostedOnDiscord).HasDefaultValue(false);
    }
}