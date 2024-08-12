using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.Services;

public interface IModeratorLoggingService
{
    public void StartService(DiscordClient client);
    public void StopService();
    public void AddToQueue(ModLogItem value);
}

public class ModeratorLoggingService(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService, ILoggerFactory loggerFactory)
    : BaseQueueService<ModLogItem>(dbContextFactory, databaseMethodService, loggerFactory), IModeratorLoggingService
{
    private protected override async Task ProcessQueueItem(ModLogItem item)
    {
        DiscordColor color = GetLogTypeColor(item.Type);
        string footerText = GetLogTypeFooterText(item.Type);

        DiscordMessageBuilder messageBuilder = new();
        DiscordEmbedBuilder embedBuilder = new()
        {
            Color = color,
            Description = item.Description,
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                IconUrl = item.TargetUser.AvatarUrl,
                Name = $"{item.TargetUser.Username} ({item.TargetUser.Id})"
            },
            Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                IconUrl = item.TargetUser.AvatarUrl,
                Text = footerText
            }
        };

        messageBuilder.Content = item.Content ?? string.Empty;
        var hasAttachment = false;
        MemoryStream memoryStream = new();
        if (item.Attachment is not null)
        {
            if (item.Attachment.FileName is null) throw new InvalidOperationException("Attachment filename is null");
            using HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(item.Attachment.Url);
            if (response.IsSuccessStatusCode)
            {
                await response.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                messageBuilder.AddFile(item.Attachment.FileName, memoryStream);
                embedBuilder.ImageUrl = $"attachment://{item.Attachment.FileName}";
                hasAttachment = true;
            }
        }
        messageBuilder.AddEmbed(embedBuilder);

        DiscordMessage sentMsg = await item.ModLogChannel.SendMessageAsync(messageBuilder);
        if (hasAttachment)
        {
            DiscordMessage renewed = await item.ModLogChannel.GetMessageAsync(sentMsg.Id);
            await DatabaseMethodService.AddInfractionsAsync(
                new Infraction(
                    GetBotUser().Id,
                    item.TargetUser.Id,
                    item.ModLogChannel.Guild.Id,
                    renewed.Embeds[0].Image?.Url.ToString() ?? throw new InvalidOperationException(),
                    true,
                    InfractionType.Note)
            );
        }
        await memoryStream.DisposeAsync();

    }

    private static DiscordColor GetLogTypeColor(ModLogType type)
    {
        var colourMap = new Dictionary<ModLogType, DiscordColor>
        {
            { ModLogType.Kick, new DiscordColor(0xf90707) },
            { ModLogType.Ban, new DiscordColor(0xf90707) },
            { ModLogType.Info, new DiscordColor(0x59bfff) },
            { ModLogType.Warning, new DiscordColor(0xFFBA01) },
            { ModLogType.UnWarn, DiscordColor.NotQuiteBlack },
            { ModLogType.Unban, DiscordColor.NotQuiteBlack },
            { ModLogType.TimedOut, new DiscordColor(0xFFBA01) },
            { ModLogType.TimeOutRemoved, DiscordColor.NotQuiteBlack },
            { ModLogType.TimeOutExtended, new DiscordColor(0xFFBA01) },
            { ModLogType.TimeOutShortened, new DiscordColor(0xFFBA01) }
        };
        return colourMap.TryGetValue(type, out DiscordColor value) ? value : DiscordColor.NotQuiteBlack;
    }
    private static string GetLogTypeFooterText(ModLogType type)
    {
        var footerText = new Dictionary<ModLogType,string>
        {
            {ModLogType.Kick,"User Kicked"},
            {ModLogType.Ban,"User Banned"},
            {ModLogType.Info,"Info"},
            {ModLogType.Warning,"User Warned"},
            {ModLogType.UnWarn,"User Unwarned"},
            {ModLogType.Unban,"User Unbanned"},
            {ModLogType.TimedOut,"User Timed Out"},
            {ModLogType.TimeOutRemoved,"Timeout Removed"},
            {ModLogType.TimeOutExtended,"Timeout Extended"},
            {ModLogType.TimeOutShortened,"Timeout Shortened"}
        };
        return footerText.TryGetValue(type, out string? value) ? value : string.Empty;
    }
}
public enum ModLogType
{
    Kick,
    Ban,
    Info,
    Warning,
    UnWarn,
    Unban,
    TimedOut,
    TimeOutRemoved,
    TimeOutExtended,
    TimeOutShortened
}

public class ModLogItem(DiscordChannel modLogChannel, DiscordUser targetUser, string description, ModLogType type, string? content = null, DiscordAttachment? attachment = null)
{
    public DiscordChannel ModLogChannel { get; set; } = modLogChannel;
    public DiscordUser TargetUser { get; set; } = targetUser;
    public string Description { get; set; } = description;
    public ModLogType Type { get; set; } = type;
    public string? Content { get; set; } = content;
    public DiscordAttachment? Attachment { get; set; } = attachment;
}