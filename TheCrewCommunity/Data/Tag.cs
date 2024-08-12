using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data;

public class Tag
{
    private readonly ulong _ownerId;
    private readonly ulong _guildId;
    
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [MaxLength(30)]
    public required string Name { get; set; }
    
    [MaxLength(1900)]
    public required string Content { get; set; }
    public required ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }
    public Guild? Guild { get; init; }
    public required ulong OwnerId
    {
        get => _ownerId;
        init => _ownerId = Convert.ToUInt64(value);
    }
    public User? Owner { get; init; }
}