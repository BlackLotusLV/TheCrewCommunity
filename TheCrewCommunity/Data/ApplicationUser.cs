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
    
    public User? User { get; set; }
}