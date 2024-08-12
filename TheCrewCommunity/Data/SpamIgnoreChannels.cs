namespace TheCrewCommunity.Data;

public class SpamIgnoreChannels
{
    public SpamIgnoreChannels(ulong guildId, ulong channelId)
    {
        GuildId = guildId;
        ChannelId = channelId;
    }
    private readonly ulong _guildId;
    private readonly ulong _channelId;
    public int Id { get; init; }
    public ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }
    public ulong ChannelId
    {
        get => _channelId;
        init => _channelId = Convert.ToUInt64(value);
    }
    public Guild? Guild { get; init; }
}