
using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data;

public class UbiInfo
{
    public UbiInfo(ulong userDiscordId, string profileId, string platform)
    {
        UserDiscordId = userDiscordId;
        ProfileId = profileId;
        Platform = platform;
    }

    public int Id { get; set; }

    public ulong UserDiscordId
    {
        get => _userDiscordId;
        init => _userDiscordId = Convert.ToUInt64(value);
    }

    private readonly ulong _userDiscordId;
    [MaxLength(36)]
    public string ProfileId { get; set; }
    [MaxLength(3)]
    public string Platform { get; set; }

    public User? User { get; set; }
}
