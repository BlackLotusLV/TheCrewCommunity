using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.EventHandlers;

public class VoiceActivityLog(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService)
{
    public async Task OnVoiceStateUpdated(DiscordClient client, VoiceStateUpdateEventArgs e)
    {
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guild = await liveBotDbContext.Guilds.FindAsync(e.Guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(e.Guild.Id));

        if (guild.VoiceActivityLogChannelId == null) return;
        DiscordChannel vcActivityLogChannel = e.Guild.GetChannel(guild.VoiceActivityLogChannelId.Value);
        DiscordEmbedBuilder embed = new()
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                IconUrl = e.User.AvatarUrl,
                Name = $"{e.User.Username} ({e.User.Id})"
            },
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
            {
                Url = e.User.AvatarUrl
            }
        };
        DiscordChannel? beforeChannel = e.Before?.Channel ?? null; //ignore warning that ? is not needed, DSP issue, it needs to be there!
        DiscordChannel? afterChannel = e.After.Channel ?? null;
        
        if (afterChannel is not null && beforeChannel is null)
        {
            embed.Title = "➡ [JOINED] ➡";
            embed.Color = DiscordColor.Green;
            embed.AddField("Channel joined", $"**{afterChannel.Name}** *({afterChannel.Id})*");
            await vcActivityLogChannel.SendMessageAsync(embed);
        }
        else if (afterChannel is null && beforeChannel is not null)
        {
            embed.Title = "⬅ [LEFT] ⬅";
            embed.Color = DiscordColor.Red;
            embed.AddField("Channel left", $"**{beforeChannel.Name}** *({beforeChannel.Id})*");
            await vcActivityLogChannel.SendMessageAsync(embed);
        }
        else if (afterChannel is not null && beforeChannel is not null && afterChannel != beforeChannel)
        {
            embed.Title = "🔄 [SWITCHED] 🔄";
            embed.Color = new DiscordColor(0x87CEFF);
            embed.AddField("Channel left", $"**{beforeChannel.Name}** *({beforeChannel.Id})*");
            embed.AddField("Channel joined", $"**{afterChannel.Name}** *({afterChannel.Id})*");
            await vcActivityLogChannel.SendMessageAsync(embed);
        }
    }
}