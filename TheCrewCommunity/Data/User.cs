using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data;

public sealed class User
{
    public User(ulong discordId)
    {
        DiscordId = discordId;
    }

    public ulong DiscordId
    {
        get => _discordId;
        init => _discordId = Convert.ToUInt64(value);
    }

    private readonly ulong _discordId;
    public int CookiesGiven { get; set; }
    public int CookiesTaken { get; set; }
    public DateTime CookieDate { get; set; }
    [MaxLength(5)]
    public string? Locale { get; init; }

    public ulong? ParentDiscordId
    {
        get => _parentDiscordId;
        init => _parentDiscordId = value.HasValue ? Convert.ToUInt64(value) : default(ulong?);
    }

    private readonly ulong? _parentDiscordId;

    public User? Parent { get; init; }
    
    public ICollection<UbiInfo>? UbiInfo { get; init; }
    public ICollection<User>? ChildUsers { get; init; }
    public ICollection<GuildUser>? UserGuilds { get; init; }
    public ICollection<PhotoCompEntries>? PhotoCompEntries { get; init; }
    public ICollection<Tag>? Tags { get; init; }
}