using System.Collections.Concurrent;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.Services;

public class PersistentMessageService : BaseQueueService<ulong>, IPersistentMessageService, IHostedService
{
    private readonly ConcurrentDictionary<ulong, DateTime> _lastUpdateAttempt = new();
    private DiscordClient? _client;

    public PersistentMessageService(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService, ILoggerFactory loggerFactory)
        : base(dbContextFactory, databaseMethodService, loggerFactory)
    {
    }

    public void EnqueueMessageUpdate(ulong channelId)
    {
        AddToQueue(channelId);
    }

    private protected override async Task ProcessQueueItem(ulong channelId)
    {
        if (_client == null) return;

        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        PersistentMessage? persistentMessage = await dbContext.PersistentMessages.FindAsync(channelId);

        if (persistentMessage == null) return;

        // Check for 5 minute cooldown
        TimeSpan timeSinceLastPost = DateTime.UtcNow - (persistentMessage.LastPostedAt ?? DateTime.MinValue);
        if (timeSinceLastPost < TimeSpan.FromMinutes(5))
        {
            // If already recently attempted, don't re-queue immediately to avoid spamming the queue
            if (_lastUpdateAttempt.TryGetValue(channelId, out DateTime lastAttempt) && DateTime.UtcNow - lastAttempt < TimeSpan.FromSeconds(30))
            {
                return;
            }
            
            _lastUpdateAttempt[channelId] = DateTime.UtcNow;
            
            // Re-queue after some time
            _ = Task.Delay(TimeSpan.FromMinutes(5) - timeSinceLastPost).ContinueWith(_ => EnqueueMessageUpdate(channelId));
            return;
        }

        try
        {
            DiscordChannel channel = await _client.GetChannelAsync(channelId);
            
            // Delete last message if it exists
            if (persistentMessage.MessageId != 0)
            {
                try
                {
                    DiscordMessage lastMessage = await channel.GetMessageAsync(persistentMessage.MessageId);
                    await lastMessage.DeleteAsync();
                }
                catch
                {
                    // Ignore if message already deleted or not found
                }
            }

            // Post new message
            DiscordMessage newMessage = await channel.SendMessageAsync(persistentMessage.Content);
            persistentMessage.MessageId = newMessage.Id;
            persistentMessage.LastPostedAt = DateTime.UtcNow;

            dbContext.PersistentMessages.Update(persistentMessage);
            await dbContext.SaveChangesAsync();
            _lastUpdateAttempt.TryRemove(channelId, out _);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update persistent message in channel {ChannelId}", channelId);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // StartService is called from LiveBotService usually, but we need the client.
        // In this project, LiveBotService seems to be the one starting these.
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopService();
        return Task.CompletedTask;
    }
    
    // Override StartService to capture client
    public new void StartService(DiscordClient client)
    {
        _client = client;
        base.StartService(client);
    }
}
