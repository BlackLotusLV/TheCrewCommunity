namespace TheCrewCommunity.Data.WebData;

public class ImageLike
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ulong DiscordId
    {
        get => _discordId;
        init => _discordId = Convert.ToUInt64(value);
    }
    private readonly ulong _discordId;
    public required Guid ImageId { get; init; }
    public DateTime Date { get; init; } = DateTime.UtcNow;
    
    public ApplicationUser? ApplicationUser { get; init; }
    public UserImage? UserImage { get; init; }
}