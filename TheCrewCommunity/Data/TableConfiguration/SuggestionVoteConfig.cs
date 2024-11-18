using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.WebData.ThisOrThat;

namespace TheCrewCommunity.Data.TableConfiguration;

public class SuggestionVoteConfig : IEntityTypeConfiguration<SuggestionVote>
{
    public void Configure(EntityTypeBuilder<SuggestionVote> builder)
    {
        builder.HasKey(sv => sv.Id);
        builder.HasOne(sv=>sv.VotedForVehicle)
            .WithMany(vs=>vs.SuggestionVotes)
            .HasForeignKey(sv=>sv.VotedForVehicleId);
        builder.HasOne(sv=>sv.VehicleSuggestion1)
            .WithMany()
            .HasForeignKey(sv=>sv.VehicleSuggestion1Id);
        builder.HasOne(sv=>sv.VehicleSuggestion2)
            .WithMany()
            .HasForeignKey(sv=>sv.VehicleSuggestion2Id);
        builder.HasOne(sv => sv.User)
            .WithMany(au => au.SuggestionVotes)
            .HasForeignKey(sv => sv.UserId);
    }
}