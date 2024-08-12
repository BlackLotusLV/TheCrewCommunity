using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data;

public class MediaOnlyChannels
{
    public required ulong ChannelId
    {
        get => _channelId;
        init => _channelId = Convert.ToUInt64(value);
    }
    private readonly ulong _channelId;
    public required ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }
    private readonly ulong _guildId;
    
    [MaxLength(1000)]
    public required string ResponseMessage { get; init; }
    
    public Guild? Guild { get; init; }
}