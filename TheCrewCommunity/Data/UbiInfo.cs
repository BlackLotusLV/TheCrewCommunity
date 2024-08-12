
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
    private readonly ulong _userDiscordId;

    public int Id { get; init; }

    public ulong UserDiscordId
    {
        get => _userDiscordId;
        init => _userDiscordId = Convert.ToUInt64(value);
    }
    [MaxLength(36)]
    public string ProfileId { get; init; }
    [MaxLength(3)]
    public string Platform { get; init; }

    public User? User { get; init; }
}
