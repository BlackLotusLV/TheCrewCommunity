using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.WebData.ThisOrThat;

namespace TheCrewCommunity.Data.TableConfiguration;

public class VehicleSuggestionConfig : IEntityTypeConfiguration<VehicleSuggestion>
{
    public void Configure(EntityTypeBuilder<VehicleSuggestion> builder)
    {
        builder.HasKey(vs => vs.Id);
    }
}