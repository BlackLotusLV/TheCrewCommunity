using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers;

public static class VoiceActivityLog
{
    public static async Task OnVoiceStateUpdated(DiscordClient client, VoiceStateUpdatedEventArgs e)
    {
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        DiscordGuild? discordGuild = await e.GetGuildAsync();
        DiscordUser? discordUser = await e.GetUserAsync();
        if (discordGuild is null || discordUser is null) return;
        Guild guild = await liveBotDbContext.Guilds.FindAsync(discordGuild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(discordGuild.Id));
        if (guild.VoiceActivityLogChannelId == null) return;
        DiscordChannel vcActivityLogChannel = await discordGuild.GetChannelAsync(guild.VoiceActivityLogChannelId.Value);
        DiscordEmbedBuilder embed = new()
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                IconUrl = discordUser.AvatarUrl,
                Name = $"{discordUser.Username} ({discordUser.Id})"
            },
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
            {
                Url = discordUser.AvatarUrl
            }
        };
        DiscordChannel? beforeChannel = await e.Before.GetChannelAsync() ?? null;
        DiscordChannel? afterChannel = await e.After.GetChannelAsync() ?? null;
        
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