namespace TheCrewCommunity.Data.WebData;

public class UserImage
{
    public required Guid Id { get; init; }
    public ulong DiscordId
    {
        get => _discordId;
        init => _discordId = Convert.ToUInt64(value);
    }
    private readonly ulong _discordId;
    public int LikesCount { get; set; }
    
    public ApplicationUser? ApplicationUser { get; set; }
    
    public ICollection<ImageLike>? ImageLikes { get; set; }
}