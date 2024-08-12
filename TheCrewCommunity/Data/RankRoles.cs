namespace TheCrewCommunity.Data;

public class RankRoles
{
    public RankRoles(ulong guildId, ulong roleId, long serverRank)
    {
        GuildId = guildId;
        RoleId = roleId;
        ServerRank = serverRank;
    }

    public int Id { get; init; }

    public ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }

    private readonly ulong _guildId;

    public ulong RoleId
    {
        get => _roleId;
        init => _roleId = Convert.ToUInt64(value);
    }

    private readonly ulong _roleId;
    public long ServerRank { get; init; }

    public Guild? Guild { get; init; }
}