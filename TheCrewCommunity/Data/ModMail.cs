using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data;

public class ModMail
{
    public ModMail(ulong guildId, ulong userDiscordId, DateTime lastMessageTime, string colorHex, bool isActive =true)
    {
        GuildId = guildId;
        UserDiscordId = userDiscordId;
        LastMessageTime = lastMessageTime;
        ColorHex = colorHex;
        IsActive = isActive;
    }

    private readonly ulong _guildId;
    private readonly ulong _userDiscordId;

    public long Id { get; init; }

    public ulong GuildId
    {
        get => _guildId;
        init => _guildId = Convert.ToUInt64(value);
    }

    public ulong UserDiscordId
    {
        get => _userDiscordId;
        init => _userDiscordId = Convert.ToUInt64(value);
    }

    public DateTime LastMessageTime { get; set; }
    public bool HasChatted { get; set; }
    public bool IsActive { get; set; }
    
    [MaxLength(7)]
    public string ColorHex { get; init; }

    public GuildUser? GuildUser { get; init; }
}