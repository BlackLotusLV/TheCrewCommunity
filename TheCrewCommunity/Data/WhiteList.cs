namespace TheCrewCommunity.Data;

public class WhiteList
{
    public long Id { get; init; }
    
    public string? UbisoftName { get; init; }
    private ulong? _discordId;

    public ulong? DiscordId
    {
        get=>_discordId; 
        set=> _discordId=Convert.ToUInt64(value);
    }
    public int WhiteListSettingsId { get; init; }
    
    public WhiteListSettings? Settings { get; init; }
}