using System.ComponentModel.DataAnnotations;
using TheCrewCommunity.Data.GameData;

namespace TheCrewCommunity.Data.WebData;

public class UserImage
{
    public required Guid Id { get; init; }
    public required ulong DiscordId
    {
        get => _discordId;
        init => _discordId = Convert.ToUInt64(value);
    }
    private readonly ulong _discordId;
    public int LikesCount { get; set; }
    public DateTime UploadDateTime { get; init; } = DateTime.UtcNow;
    [MaxLength(40)]
    public required string Title { get; init; }
    public ApplicationUser? ApplicationUser { get; init; }
    public required Guid GameId { get; init; }
    public Game? Game { get; init; }
    public ICollection<ImageLike>? ImageLikes { get; init; }
}