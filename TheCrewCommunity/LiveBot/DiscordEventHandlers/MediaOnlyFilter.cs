using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers;

public static class MediaOnlyFilter
{
    public static async Task OnMessageCreated(DiscordClient client, MessageCreatedEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Author.IsBot || eventArgs.Message.Attachments.Count!=0 || eventArgs.Message.Content.Split(' ').Any(x=>Uri.TryCreate(x, UriKind.Absolute, out _))) return;
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        Guild? guild = await context.Guilds.Include(x => x.MediaOnlyChannels).FirstOrDefaultAsync(x => x.Id == eventArgs.Guild.Id);
        if (guild is null)
        {
            await context.AddAsync(new Guild(eventArgs.Guild.Id));
            await context.SaveChangesAsync();
            return;
        }
        if (guild.MediaOnlyChannels is null) return;
        if (guild.MediaOnlyChannels.Count == 0) return;
        MediaOnlyChannels? channel = guild.MediaOnlyChannels.FirstOrDefault(x => x.ChannelId == eventArgs.Channel.Id);
        if (channel is null) return;
        await eventArgs.Message.DeleteAsync();
        string response = channel.ResponseMessage ?? "This channel is for sharing media only, please use the appropriate channel for discussions. If this is a mistake please contact a moderator.";
        
        DiscordMessage msg = await eventArgs.Channel.SendMessageAsync(response);
        await Task.Delay(9000);
        await msg.DeleteAsync();
        client.Logger.LogInformation(CustomLogEvents.PhotoCleanup,
            "User {Username}({UserId}) tried to send text in a media only channel. Message deleted", eventArgs.Author.Username, eventArgs.Author.Id);
    }
}