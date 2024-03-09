namespace TheCrewCommunity.Data;

public class PhotoCompSettings
{
    public PhotoCompSettings(ulong guildId, int winnerCount, int maxEntries, int customParameter, string customName, ulong dumpChannelId, bool isOpen)
    {
        GuildId = guildId;
        WinnerCount = winnerCount;
        MaxEntries = maxEntries;
        CustomParameter = customParameter;
        CustomName = customName;
        DumpChannelId = dumpChannelId;
        IsOpen = isOpen;
    }
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public int WinnerCount { get; set; }
    public int MaxEntries { get; set; }
    public int CustomParameter { get; set; }
    public string CustomName { get; set; }
    public ulong DumpChannelId { get; set; }
    public bool IsOpen { get; set; }
    
    public ICollection<PhotoCompEntries>? Entries { get; set; }
    public Guild? Guild { get; set; }
}