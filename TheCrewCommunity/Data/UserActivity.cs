﻿namespace TheCrewCommunity.Data;

public class UserActivity
{
    public UserActivity(ulong userDiscordId, ulong guildId, int points, DateTime date)
    {
        UserDiscordId = userDiscordId;
        GuildId = guildId;
        Points = points;
        Date = date;
    }

    public long Id { get; set; }

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
    public int Points { get; set; }

    public DateTime Date
    {
        get => _date;
        set => _date = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private DateTime _date;

    public GuildUser? GuildUser { get; set; }
}