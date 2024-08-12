using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data;

public class VanityWhitelist
{
    private readonly ulong _guildId;
    public Guid Id { get; init; } = Guid.NewGuid();
    public required ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }
    public Guild? Guild { get; init; }
    [MaxLength(25)]
    public required string VanityCode { get; init; }
}