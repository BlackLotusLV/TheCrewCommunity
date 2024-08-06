using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using TheCrewCommunity.Data.WebData.ProSettings;

namespace TheCrewCommunity.Data.WebData;

public class ApplicationUser : IdentityUser<Guid>
{
    public ulong DiscordId
    {
        get => _discordId;
        set => _discordId = Convert.ToUInt64(value);
    }
    private ulong _discordId;

    [MaxLength(32)]
    public string? GlobalUsername { get; set; }
    
    [MaxLength(2048)]
    public string? AvatarUrl { get; set; }

    public bool IsModerator { get; set; }
    public User? User { get; set; }
    
    public ICollection<MtfstCarProSettings>? MotorfestCarProSettings { get; set; }
    public ICollection<MtfstCarProSettingsLikes>? MotorfestCarProSettingLikes { get; set; }
    
    public ICollection<UserImage>? Images { get; set; }
    public ICollection<ImageLike>? ImageLikes { get; set; }
}