using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using TheCrewCommunity.Data.ThisOrThat;
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
    public User? User { get; init; }
    
    public ICollection<MtfstCarProSettings>? MotorfestCarProSettings { get; init; }
    public ICollection<MtfstCarProSettingsLikes>? MotorfestCarProSettingLikes { get; init; }
    
    public ICollection<UserImage>? Images { get; init; }
    public ICollection<ImageLike>? ImageLikes { get; init; }
    public ICollection<SuggestionVote> SuggestionVotes { get; init; }
}