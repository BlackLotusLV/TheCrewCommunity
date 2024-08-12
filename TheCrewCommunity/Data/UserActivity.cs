namespace TheCrewCommunity.Data;

public class UserActivity
{
    public UserActivity(ulong userDiscordId, ulong guildId, int points, DateTime date)
    {
        UserDiscordId = userDiscordId;
        GuildId = guildId;
        Points = points;
        Date = date;
    }

    public long Id { get; init; }

    public ulong UserDiscordId
    {
        get => _userDiscordId;
        init => _userDiscordId = Convert.ToUInt64(value);
    }

    private readonly ulong _userDiscordId;

    public ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }

    private readonly ulong _guildId;
    public int Points { get; set; }

    public DateTime Date
    {
        get => _date;
        init => _date = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private readonly DateTime _date;

    public GuildUser? GuildUser { get; init; }
}