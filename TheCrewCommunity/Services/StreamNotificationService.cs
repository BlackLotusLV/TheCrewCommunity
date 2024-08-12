using System.Collections.Concurrent;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.Services;

public class StreamNotificationService(ILoggerFactory loggerFactory) : IHostedService
{
    private  BlockingCollection<StreamNotificationItem>? _queue;
    private Timer? _cleanupTimer;
    private readonly ILogger<StreamNotificationService> _logger = loggerFactory.CreateLogger<StreamNotificationService>();
    private Task? _task;
    public static List<LiveStreamer> LiveStreamerList { get; } = [];
    public static int StreamCheckDelay => 5;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(CustomLogEvents.StreamNotification,"Stream notification service starting");
        _queue = new BlockingCollection<StreamNotificationItem>();
        _task = Task.Run(async () => await ProcessQueueAsync(), cancellationToken);
        _cleanupTimer = new Timer(_ => CleanupList());
        _cleanupTimer.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(10));
        _logger.LogInformation(CustomLogEvents.StreamNotification,"Stream notification service started");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(CustomLogEvents.StreamNotification,"Stream notification service is stopping");
        if (_task is null)
        {
            _logger.LogInformation(CustomLogEvents.StreamNotification,"Can't stop task, task is null");
        }
        else
        {
            _task.Dispose();
        }

        if (_queue is null)
        {
            _logger.LogInformation(CustomLogEvents.StreamNotification,"Can't dispose of queue, already null");
        }
        else
        {
            _queue.Dispose();
        }

        if (_cleanupTimer is null)
        {
            _logger.LogInformation(CustomLogEvents.StreamNotification,"Timer already null");
        }
        else
        {
            await _cleanupTimer.DisposeAsync();
        }

        _logger.LogInformation(CustomLogEvents.StreamNotification,"Stream notification service stopping procedure completed");
    }

    public void AddToQueue(StreamNotificationItem item)
    {
        if (_queue is null)
        {
            _logger.LogDebug(CustomLogEvents.StreamNotification,"Tried to add to queue, queue is null");
        }
        else
        {
            _queue.Add(item);
        }
    }

    private async Task ProcessQueueAsync()
    {
        if (_queue is null)
        {
            _logger.LogError(CustomLogEvents.StreamNotification,"Stream notification service can't continue due to queue not being initialised");
            await StopAsync(new CancellationToken());
            return;
        }
        foreach (StreamNotificationItem item in _queue.GetConsumingEnumerable())
        {
            try
            {
                await ProcessItem(item);
            }
            catch (Exception e)
            {
                _logger.LogError(CustomLogEvents.ServiceError, e, "Stream notification service failed to process an item");
            }
        }
    }

    private async Task ProcessItem(StreamNotificationItem item)
    {
        DiscordMember streamMember = await item.Guild.GetMemberAsync(item.EventArgs.User.Id);
        DiscordActivity? activity = item.EventArgs.User?.Presence?.Activities?.FirstOrDefault(w =>
            w.Name.Equals("twitch", StringComparison.CurrentCultureIgnoreCase) || w.Name.Equals("youtube", StringComparison.CurrentCultureIgnoreCase));
        if (activity?.RichPresence?.State is null || activity.RichPresence?.Details is null || activity?.StreamUrl is null) return;
        string gameTitle = activity.RichPresence.State;
        string streamTitle = activity.RichPresence.Details;
        string streamUrl = activity.StreamUrl;

        var roleIds = new HashSet<ulong>(item.StreamNotification.RoleIds ?? []);
        var games = new HashSet<string>(item.StreamNotification.Games ?? [], StringComparer.OrdinalIgnoreCase);

        bool role = roleIds.Count == 0 || streamMember.Roles.Any(r => roleIds.Contains(r.Id));
        bool game = games.Count == 0 || games.Contains(gameTitle);

        if (!game || !role || item.EventArgs.User is null) return;
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
        _logger.LogInformation("Stream notification sent for {Username} in {GuildName} in {Channel}",
            item.EventArgs.User.Username,
            item.Guild.Name,
            item.Channel.Name);
        //adds user to list
        LiveStreamerList.Add(item.Streamer);
    }
    private void CleanupList()
    {
        try
        {
            foreach (LiveStreamer item in LiveStreamerList.Where(item => item.Time.AddHours(StreamCheckDelay) < DateTime.UtcNow && item.User.Presence.Activity.ActivityType != DiscordActivityType.Streaming))
            {
                LiveStreamerList.Remove(item);
                _logger.LogDebug(CustomLogEvents.LiveStream, "User {UserName} removed from Live Stream List - {CheckDelay} hours passed", item.User.Username, StreamCheckDelay);
            }
        }
        catch (Exception)
        {
            _logger.LogDebug(CustomLogEvents.LiveStream, "Live Stream list is empty. No-one to remove or check");
        }
    }
}
public class StreamNotificationItem(StreamNotifications streamNotification, PresenceUpdatedEventArgs eventArgs, DiscordGuild guild, DiscordChannel channel, LiveStreamer streamer)
{
    public StreamNotifications StreamNotification { get; set; } = streamNotification;
    public PresenceUpdatedEventArgs EventArgs { get; set; } = eventArgs;
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