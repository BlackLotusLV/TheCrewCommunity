﻿namespace TheCrewCommunity.Data;

public class ButtonRoles
{
    public ButtonRoles(ulong guildId)
    {
        GuildId = guildId;
    }

    public int Id { get; set; }

    public ulong ButtonId
    {
        get => _buttonId;
        set => _buttonId = Convert.ToUInt64(value);
    }

    private ulong _buttonId;

    private ulong _guildId;

    public ulong GuildId
    {
        get => _guildId;
        set => _guildId = Convert.ToUInt64(value);
    }

    private ulong _channelId;

    public ulong ChannelId
    {
        get => _channelId;
        set => _channelId = Convert.ToUInt64(value);
    }

    public Guild Guild { get; set; }
}