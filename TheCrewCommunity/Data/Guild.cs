using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data;

public class Guild
{
    public Guild(ulong id)
    {
        Id = id;
    }

    public ulong Id
    {
        get => _id;
        init => _id = Convert.ToUInt64(value);
    }

    private readonly ulong _id;

    public ulong? DeleteLogChannelId
    {
        get => _deleteLogChannelId;
        init => _deleteLogChannelId = value.HasValue ? Convert.ToUInt64(value) : null;
    }

    private readonly ulong? _deleteLogChannelId;

    public ulong? UserTrafficChannelId
    {
        get => _userTrafficChannelId;
        init => _userTrafficChannelId = value.HasValue ? Convert.ToUInt64(value) : null;
    }

    private readonly ulong? _userTrafficChannelId;

    public ulong? ModerationLogChannelId
    {
        get => _moderationLogChannelId;
        init => _moderationLogChannelId = value.HasValue ? Convert.ToUInt64(value) : null;
    }

    private readonly ulong? _moderationLogChannelId;

    public ulong? ModMailChannelId
    {
        get => _modMailChannelId;
        init => _modMailChannelId = value.HasValue ? Convert.ToUInt64(value) : null;
    }

    private readonly ulong? _modMailChannelId;
    public bool HasLinkProtection { get; init; }

    public ulong? VoiceActivityLogChannelId
    {
        get => _voiceActivityLogChannelId;
        init => _voiceActivityLogChannelId = value.HasValue ? Convert.ToUInt64(value) : null;
    }

    private readonly ulong? _voiceActivityLogChannelId;

    public ulong? EventLogChannelId
    {
        get => _eventLogChannelId;
        init => _eventLogChannelId = value.HasValue ? Convert.ToUInt64(value) : null;
    }

    private readonly ulong? _eventLogChannelId;
    public bool HasEveryoneProtection { get; set; }
    public bool ModMailEnabled { get; init; }

    public ulong? WelcomeChannelId
    {
        get => _welcomeChannelId;
        init => _welcomeChannelId = value.HasValue ? Convert.ToUInt64(value) : null;
    }

    private readonly ulong? _welcomeChannelId;
    [MaxLength(1000)]
    public string? WelcomeMessage { get; init; }
    [MaxLength(1000)]
    public string? GoodbyeMessage { get; init; }
    public bool HasScreening { get; init; }

    public ulong? RoleId
    {
        get => _roleId;
        init => _roleId = value.HasValue ? Convert.ToUInt64(value) : null;
    }

    private readonly ulong? _roleId;

    public ulong? WhiteListRoleId
    {
        get => _whiteListRoleId;
        init => _whiteListRoleId = value.HasValue ? Convert.ToUInt64(value) : null;
    }

    private readonly ulong? _whiteListRoleId;
    
    public ulong? UserReportsChannelId
    {
        get => _userReportsChannelId;
        init => _userReportsChannelId = value.HasValue ? Convert.ToUInt64(value) : null;
    }
    private readonly ulong? _userReportsChannelId;

    public ulong? SupporterRoleId
    {
        get => _supporterRoleId;
        init => _supporterRoleId = value.HasValue ? Convert.ToUInt64(value) : null;
    }

    private readonly ulong? _supporterRoleId;

    public ulong? ThisOrThatDailyChannelId
    {
        get => _thisOrThatDailyChannelId;
        init=> _thisOrThatDailyChannelId = value.HasValue ? Convert.ToUInt64(value) : null;
    }
    private readonly ulong? _thisOrThatDailyChannelId;

    public ICollection<GuildUser>? GuildUsers { get; init; }
    public ICollection<RankRoles>? RankRoles { get; init; }
    public ICollection<ButtonRoles>? ButtonRoles { get; init; }
    public ICollection<RoleTagSettings>? RoleTagSettings { get; init; }
    public ICollection<StreamNotifications>? StreamNotifications { get; init; }
    public ICollection<SpamIgnoreChannels>? SpamIgnoreChannels { get; init; }
    public ICollection<WhiteListSettings>? WhiteListSettings { get; init; }
    public ICollection<MediaOnlyChannels>? MediaOnlyChannels { get; init; }
    public ICollection<PhotoCompSettings>? PhotoCompSettings { get; init; }
    public ICollection<Tag>? Tags { get; init; }
    public ICollection<VanityWhitelist>? WhitelistedVanities { get; init; }
}