using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheCrewCommunity.Data.WebData;

namespace TheCrewCommunity.Data.TableConfiguration;

public class ImageLikeConfig : IEntityTypeConfiguration<ImageLike>
{
    public void Configure(EntityTypeBuilder<ImageLike> builder)
    {
        builder.HasKey(il => il.Id);
        builder.HasOne(il => il.ApplicationUser).WithMany(au => au.ImageLikes).HasForeignKey(il => il.DiscordId);
        builder.HasOne(il => il.UserImage).WithMany(ui => ui.ImageLikes).HasForeignKey(il => il.ImageId);
    }
}