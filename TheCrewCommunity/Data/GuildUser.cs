
namespace TheCrewCommunity.Data;

public sealed class GuildUser
{
    public GuildUser(ulong userDiscordId, ulong guildId, int kickCount = 0, int banCount = 0, bool isModMailBlocked = false)
    {
        UserDiscordId = userDiscordId;
        GuildId = guildId;
        KickCount = kickCount;
        BanCount = banCount;
        IsModMailBlocked = isModMailBlocked;
    }

    
    public ulong UserDiscordId
    {
        get => _userDiscordId;
        init => _userDiscordId = Convert.ToUInt64(value);
    }

    private readonly ulong _userDiscordId;

    public ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }

    private readonly ulong _guildId;
    public int KickCount { get; set; }
    public int BanCount { get; set; }
    public bool IsModMailBlocked { get; set; }

    public User? User { get; init; }
    public Guild? Guild { get; init; }

    public ICollection<ModMail>? ModMails { get; init; }
    public ICollection<UserActivity>? UserActivity { get; init; }
    public ICollection<Infraction>? Infractions { get; init; }
}