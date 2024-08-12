namespace TheCrewCommunity.Data;

public class WhiteListSettings
{
    public WhiteListSettings(ulong guildId, ulong roleId)
    {
        GuildId = guildId;
        RoleId = roleId;
    }
    private readonly ulong _guildId;
    private readonly ulong _roleId;
    public int Id { get; init; }

    public ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }

    public ulong RoleId
    {
        get => _roleId;
        init => _roleId = Convert.ToUInt64(value);
    }
    public ICollection<WhiteList>? WhitelistedUsers { get; init; }
    public Guild? Guild { get; init; }
}