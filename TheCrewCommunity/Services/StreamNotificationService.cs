using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.Services;

public interface IStreamNotificationService
{
    void StartService(DiscordClient client);
    void StopService();
    void AddToQueue(StreamNotificationItem value);
    void StreamListCleanup();
}

public class StreamNotificationService(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService, ILoggerFactory loggerFactory)
    : BaseQueueService<StreamNotificationItem>(dbContextFactory, databaseMethodService,
        loggerFactory), IStreamNotificationService
{
    public static List<LiveStreamer> LiveStreamerList { get; set; } = [];
    public static int StreamCheckDelay { get; } = 5;

    private protected override async Task ProcessQueueItem(StreamNotificationItem item)
    {
        DiscordMember streamMember = await item.Guild.GetMemberAsync(item.EventArgs.User.Id);
        DiscordActivity? activity = item.EventArgs.User?.Presence?.Activities?.FirstOrDefault(w => w.Name.Equals("twitch", StringComparison.CurrentCultureIgnoreCase) || w.Name.Equals("youtube", StringComparison.CurrentCultureIgnoreCase));
                if (activity?.RichPresence?.State is null || activity.RichPresence?.Details is null || activity.StreamUrl is null) return;
                string gameTitle = activity.RichPresence.State;
                string streamTitle = activity.RichPresence.Details;
                string streamUrl = activity.StreamUrl;

                var roleIds = new HashSet<ulong>(item.StreamNotification.RoleIds ?? Array.Empty<ulong>());
                var games = new HashSet<string>(item.StreamNotification.Games ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

                bool role = roleIds.Count == 0 || streamMember.Roles.Any(r => roleIds.Contains(r.Id));
                bool game = games.Count == 0 || games.Contains(gameTitle);

                if (!game || !role) return;
                string description = $"**Streamer:**\n {item.EventArgs.User.Mention}\n\n" +
                                     $"**Game:**\n{gameTitle}\n\n" +
                                     $"**Stream title:**\n{streamTitle}\n\n" +
                                     $"**Stream Link:**\n{streamUrl}";
                DiscordEmbedBuilder embed = new()
                {
                    Color = new DiscordColor(0x6441A5),
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        IconUrl = item.EventArgs.User.AvatarUrl,
                        Name = "STREAM",
                        Url = streamUrl
                    },
                    Description = description,
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                    {
                        Url = item.EventArgs.User.AvatarUrl
                    },
                    Title = $"Check out {item.EventArgs.User.Username} is now Streaming!"
                };
                await item.Channel.SendMessageAsync(embed: embed);
                Logger.LogInformation("Stream notification sent for {Username} in {GuildName} in {Channel}",
                    item.EventArgs.User.Username,
                    item.Guild.Name,
                    item.Channel.Name);
                //adds user to list
                LiveStreamerList.Add(item.Streamer);
    }
    
    public void StreamListCleanup()
    {
        try
        {
            foreach (LiveStreamer item in LiveStreamerList.Where(item => item.Time.AddHours(StreamCheckDelay) < DateTime.UtcNow && item.User.Presence.Activity.ActivityType != ActivityType.Streaming))
            {
                Logger.LogDebug(CustomLogEvents.LiveStream, "User {UserName} removed from Live Stream List - {CheckDelay} hours passed", item.User.Username, StreamCheckDelay);
                LiveStreamerList.Remove(item);
            }
        }
        catch (Exception)
        {
            Logger.LogDebug(CustomLogEvents.LiveStream, "Live Stream list is empty. No-one to remove or check");
        }
    }
}
public class StreamNotificationItem(StreamNotifications streamNotification, PresenceUpdateEventArgs eventArgs, DiscordGuild guild, DiscordChannel channel, LiveStreamer streamer)
{
    public StreamNotifications StreamNotification { get; set; } = streamNotification;
    public PresenceUpdateEventArgs EventArgs { get; set; } = eventArgs;
    public DiscordGuild Guild { get; set; } = guild;
    public DiscordChannel Channel { get; set; } = channel;
    public LiveStreamer Streamer { get; set; } = streamer;
}

public class LiveStreamer
{
    public required DiscordUser User { get; init; }
    public required DateTime Time { get; init; }
    public required DiscordGuild Guild { get; init; }
    public required DiscordChannel Channel { get; init; }
}