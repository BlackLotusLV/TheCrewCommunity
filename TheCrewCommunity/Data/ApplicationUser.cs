using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TheCrewCommunity.Data;

public class ApplicationUser : IdentityUser
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

    public bool IsModerator { get; set; } = false;
    public User? User { get; set; }
}