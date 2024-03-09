using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data;

public class VanityWhitelist
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required ulong GuildId
    {
        get => _guildId;
        set => _guildId = Convert.ToUInt64(value);
    }
    private ulong _guildId;
    public Guild? Guild { get; set; }
    [MaxLength(25)]
    public required string VanityCode { get; set; }
}