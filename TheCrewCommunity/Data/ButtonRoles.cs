namespace TheCrewCommunity.Data;

public class ButtonRoles
{
    public ButtonRoles(ulong guildId)
    {
        GuildId = guildId;
    }

    public int Id { get; init; }

    public ulong ButtonId
    {
        get => _buttonId;
        init => _buttonId = Convert.ToUInt64(value);
    }

    private readonly ulong _buttonId;

    private readonly ulong _guildId;

    public ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }

    private readonly ulong _channelId;

    public ulong ChannelId
    {
        get => _channelId;
        init => _channelId = Convert.ToUInt64(value);
    }

    public Guild? Guild { get; init; }
}