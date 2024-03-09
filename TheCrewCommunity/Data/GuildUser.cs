
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
        set => _userDiscordId = Convert.ToUInt64(value);
    }

    private ulong _userDiscordId;

    public ulong GuildId
    {
        get => _guildId;
        set => _guildId = Convert.ToUInt64(value);
    }

    private ulong _guildId;
    public int KickCount { get; set; }
    public int BanCount { get; set; }
    public bool IsModMailBlocked { get; set; }

    public User? User { get; set; }
    public Guild? Guild { get; set; }

    public ICollection<ModMail>? ModMails { get; set; }
    public ICollection<UserActivity>? UserActivity { get; set; }
    public ICollection<Infraction>? Infractions { get; set; }
}