using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data.Entities.Discord;

public class PersistentMessage
{
    public ulong ChannelId
    {
        get => _channelId;
        init => _channelId = Convert.ToUInt64(value);
    }
    private readonly ulong _channelId;

    public ulong MessageId
    {
        get => _messageId;
        set => _messageId = Convert.ToUInt64(value);
    }
    private ulong _messageId;

    [MaxLength(2000)]
    public required string Content { get; set; }
    public DateTime? LastPostedAt { get; set; }
}
