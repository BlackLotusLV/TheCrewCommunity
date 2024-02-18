using System.Collections.Concurrent;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.Services;

public abstract class BaseQueueService<T>
{
    private protected readonly IDbContextFactory<LiveBotDbContext> DbContextFactory;
    private protected readonly IDatabaseMethodService DatabaseMethodService;
    private protected readonly CancellationTokenSource CancellationTokenSource;
    private Task _backgroundTask;
    private readonly Type _type;
    private protected readonly BlockingCollection<T> Queue = new();
    private protected readonly ILogger<T> Logger;
    private DiscordClient _client;
    
    protected BaseQueueService(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService, ILoggerFactory loggerFactory)
    {
        DbContextFactory = dbContextFactory;
        DatabaseMethodService = databaseMethodService;
        CancellationTokenSource = new CancellationTokenSource();
        _type = GetType();
        Logger = loggerFactory.CreateLogger<T>();
    }
    
    public bool IsRunning { get;private set; }

    public void StartService(DiscordClient client)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException($"{_type.Name} service is already running!");
        }
        
        _client=client;
        Logger.LogInformation(CustomLogEvents.LiveBot,"{Type} service starting!",_type.Name);
        _backgroundTask = Task.Run(async ()=>await QueueProcessor(),CancellationTokenSource.Token);
        IsRunning = true;
        Logger.LogInformation(CustomLogEvents.LiveBot,"{Type} service has started!",_type.Name);
    }
    public void StopService()
    {
        Logger.LogInformation(CustomLogEvents.LiveBot,"{Type} service stopping!",_type.Name);
        CancellationTokenSource.Cancel();
        _backgroundTask.Wait();
        Queue.Dispose();
        IsRunning = false;
        Logger.LogInformation(CustomLogEvents.LiveBot,"{Type} service has stopped!",_type.Name);
    }
    
    private protected abstract Task ProcessQueueItem(T item);
    private async Task QueueProcessor()
    {
        foreach (T item in Queue.GetConsumingEnumerable(CancellationTokenSource.Token))
        {
            try
            {
                await ProcessQueueItem(item);
            }
            catch (Exception e)
            {
                Logger.LogError(CustomLogEvents.ServiceError,e,"{Type} failed to process item in queue",GetType().Name);
            }
        }
    }
    public void AddToQueue(T value)
    {
        Queue.Add(value);
    }
    
    protected DiscordUser GetBotUser()
    {
        return _client.CurrentUser;
    }
    
}