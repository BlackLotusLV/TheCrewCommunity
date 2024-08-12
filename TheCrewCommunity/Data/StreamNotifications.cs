namespace TheCrewCommunity.Data;

public class StreamNotifications
{
    public StreamNotifications(ulong guildId)
    {
        GuildId = guildId;
    }

    private readonly ulong _guildId;
    private readonly ulong[]? _roleIds;
    private readonly ulong _channelId;

    public int Id { get; init; }

    public ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }

    public string[]? Games { get; init; }

    public ulong[]? RoleIds
    {
        get => _roleIds;
        init
        {
            if (value != null) _roleIds = value.Select(Convert.ToUInt64).ToArray();
        }
    }

    public ulong ChannelId
    {
        get => _channelId;
        init => _channelId = Convert.ToUInt64(value);
    }

    public Guild? Guild { get; init; }
}