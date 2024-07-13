using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
    }
}
    