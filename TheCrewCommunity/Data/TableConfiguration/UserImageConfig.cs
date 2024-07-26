using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.GameData;
using TheCrewCommunity.Data.WebData;

namespace TheCrewCommunity.Data.TableConfiguration;

public class UserImageConfig : IEntityTypeConfiguration<UserImage>
{
    public void Configure(EntityTypeBuilder<UserImage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne<ApplicationUser>(x => x.ApplicationUser)
            .WithMany(au => au.Images)
            .HasForeignKey(ui => ui.DiscordId)
            .HasPrincipalKey(au => au.DiscordId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Game>(x => x.Game)
            .WithMany(game => game.UserImages)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
    