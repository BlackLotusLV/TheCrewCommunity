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
    public Guid ImageId { get; init; }
    
    public ApplicationUser? ApplicationUser { get; init; }
    public UserImage? UserImage { get; init; }
}