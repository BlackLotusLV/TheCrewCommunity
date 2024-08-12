using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data;

public class Infraction
{
    public Infraction(ulong adminDiscordId, ulong userId, ulong guildId, string reason, bool isActive, InfractionType infractionType)
    {
        UserId = userId;
        GuildId = guildId;
        Reason = reason;
        IsActive = isActive;
        AdminDiscordId = adminDiscordId;
        InfractionType = infractionType;
    }

    private readonly ulong _guildId;
    private readonly ulong _userId;
    private readonly ulong _adminDiscordId;

    public long Id { get; init; }

    public ulong UserId
    {
        get => _userId;
        init => _userId = Convert.ToUInt64(value);
    }

    public ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }

    [MaxLength(2000)]
    public string? Reason { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset TimeCreated { get; init; } = DateTimeOffset.UtcNow;

    public ulong AdminDiscordId
    {
        get => _adminDiscordId;
        init => _adminDiscordId = Convert.ToUInt64(value);
    }

    public InfractionType InfractionType { get; init; }
    public GuildUser? GuildUser { get; init; }
}

public enum InfractionType
{
    Warning,
    Kick,
    Ban,
    Note,
    TimeoutAdded,
    TimeoutRemoved,
    TimeoutExtended,
    TimeoutReduced
}