namespace TheCrewCommunity.Data.WebData.ProSettings;

public class MtfstCarProSettingsLikes
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ulong DiscordId
    {
        get => _discordId;
        set => _discordId = Convert.ToUInt64(value);
    }
    private ulong _discordId;
    public required Guid ProSettingsId { get; set; }
    
    public ApplicationUser ApplicationUser { get; set; }
    public MtfstCarProSettings MtfstCarProSettings { get; set; }
}