using System.Collections.Immutable;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.EventHandlers;

public class FloodFilter(IModeratorWarningService warningService, IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService, GeneralUtils generalUtils)
{
    private const int SpamInterval = 6;
    private const int SpamCount = 5;
    private readonly List<DiscordMessage> _messageList = [];
    public async Task OnMessageCreated(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Author.IsBot || eventArgs.Author.IsCurrent || eventArgs.Guild is null) return;
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild? guild = await liveBotDbContext.Guilds.Include(x => x.SpamIgnoreChannels).FirstOrDefaultAsync(x => x.Id == eventArgs.Guild.Id);
        if (guild is null)
        {
            await databaseMethodService.AddGuildAsync(new Guild(eventArgs.Guild.Id));
            return;
        }
        if (guild.SpamIgnoreChannels is null) return;
        if (guild.SpamIgnoreChannels.Count == 0) return;
        var spamIgnoreChannels = guild.SpamIgnoreChannels.ToImmutableArray();

        if (guild?.ModerationLogChannelId == null || spamIgnoreChannels.Any(x=>x.ChannelId==eventArgs.Channel.Id)) return;
        DiscordMember member = await eventArgs.Guild.GetMemberAsync(eventArgs.Author.Id);

        if (generalUtils.CheckIfMemberAdmin(member)) return;
        _messageList.Add(eventArgs.Message);
        var duplicateMessages = _messageList.Where(w => w.Author == eventArgs.Author && w.Content == eventArgs.Message.Content && eventArgs.Guild == w.Channel.Guild).ToList();
        int i = duplicateMessages.Count;
        if (i < SpamCount) return;

        TimeSpan time = (duplicateMessages[i - 1].CreationTimestamp - duplicateMessages[i - SpamCount].CreationTimestamp) / SpamCount;
        if (time >= TimeSpan.FromSeconds(SpamInterval)) return;
            
        var channelList = duplicateMessages.GetRange(i - SpamCount, SpamCount).Select(s => s.Channel).Distinct().ToList();
        await member.TimeoutAsync(DateTimeOffset.UtcNow + TimeSpan.FromHours(1), "Spam filter triggered - flood");
        foreach (DiscordChannel channel in channelList)
        {
            await channel.DeleteMessagesAsync(duplicateMessages.GetRange(i - SpamCount, SpamCount));
        }

        int infractionLevel = liveBotDbContext.Infractions.Count(w => w.UserId == member.Id && w.GuildId == eventArgs.Guild.Id && w.InfractionType == InfractionType.Warning && w.IsActive);
        if (infractionLevel < 5)
        {
            warningService.AddToQueue(new WarningItem(eventArgs.Author, client.CurrentUser, eventArgs.Guild, eventArgs.Channel, "Spam protection triggered - flood", true));
        }
    }
}