using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers;

// Must re-write to use twitch api and youtube api to check if user is live, and what they are streaming.

public static class LivestreamNotifications
{
    public static async Task OnPresenceChange(DiscordClient client, PresenceUpdatedEventArgs e)
    {
        if (e.User is null || e.User.IsBot || e.User.Presence is null) return;
        DiscordGuild guild = e.User.Presence.Guild;
        if (e.User.Presence.Activities.All(x => x.ActivityType != DiscordActivityType.Streaming)) return;
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var streamNotificationService = client.ServiceProvider.GetRequiredService<IStreamNotificationService>();
        
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        var streamNotifications = liveBotDbContext.StreamNotifications.Where(w => w.GuildId == guild.Id).ToList();
        if (streamNotifications.Count == 0) return;
        foreach (StreamNotifications streamNotification in streamNotifications)
        {
            DiscordChannel channel = await guild.GetChannelAsync(streamNotification.ChannelId);
            LiveStreamer streamer = new()
            {
                User = e.User,
                Time = DateTime.UtcNow,
                Guild = guild,
                Channel = channel
            };
            int itemIndex;
            try
            {
                itemIndex = StreamNotificationService.LiveStreamerList.FindIndex(a =>
                    a.User.Id == e.User.Id
                    && a.Guild.Id == e.User.Presence.Guild.Id
                    && a.Channel.Id == channel.Id);
            }
            catch (Exception)
            {
                itemIndex = -1;
            }

            switch (itemIndex)
            {
                case >= 0
                    when e.User.Presence.Activities.FirstOrDefault(w => w.Name.ToLower() == "twitch" || w.Name.ToLower() == "youtube") == null:
                {
                    //removes user from list
                    if (StreamNotificationService.LiveStreamerList[itemIndex].Time.AddHours(StreamNotificationService.StreamCheckDelay) < DateTime.UtcNow
                        && e.User.Presence.Activities.FirstOrDefault(w => w.Name.ToLower() == "twitch" || w.Name.ToLower() == "youtube") == StreamNotificationService.LiveStreamerList[itemIndex]
                            .User.Presence.Activities.FirstOrDefault(w => w.Name.ToLower() == "twitch" || w.Name.ToLower() == "youtube"))
                    {
                        StreamNotificationService.LiveStreamerList.RemoveAt(itemIndex);
                    }

                    break;
                }
                case -1
                    when e.User.Presence.Activities.FirstOrDefault(w => w.Name.ToLower() == "twitch" || w.Name.ToLower() == "youtube") != null
                         && e.User.Presence.Activities.First(w => w.Name.ToLower() == "twitch" || w.Name.ToLower() == "youtube").ActivityType.Equals(DiscordActivityType.Streaming):
                    streamNotificationService.AddToQueue(new StreamNotificationItem(streamNotification, e, guild, channel, streamer));
                    break;
            }
        }
    }
}