using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data;

public class RoleTagSettings
{
    private readonly ulong _guildId;
    private readonly ulong _roleId;
    private readonly ulong? _channelId;
    public long Id { get; init; }

    public required ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }

    public required ulong RoleId
    {
        get => _roleId;
        init => _roleId = Convert.ToUInt64(value);
    }

    public ulong? ChannelId
    {
        get => _channelId;
        init => _channelId = value.HasValue ? Convert.ToUInt64(value) : default(ulong?);
    }

    public required int Cooldown { get; init; }
    public required DateTime LastTimeUsed { get; set; }
    [MaxLength(2000)]
    public required string Message { get; init; }
    [MaxLength(150)]
    public required string Description { get; init; }

    public Guild? Guild { get; init; }
}