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
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _backgroundTask;
    private readonly Type _type;
    private readonly BlockingCollection<T> _queue = new();
    private protected readonly ILogger<T> Logger;
    private DiscordClient? _client;
    
    protected BaseQueueService(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService, ILoggerFactory loggerFactory)
    {
        DbContextFactory = dbContextFactory;
        DatabaseMethodService = databaseMethodService;
        _cancellationTokenSource = new CancellationTokenSource();
        _type = GetType();
        Logger = loggerFactory.CreateLogger<T>();
    }
    
    private bool IsRunning { get; set; }

    public void StartService(DiscordClient client)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException($"{_type.Name} service is already running!");
        }
        
        _client=client;
        Logger.LogInformation(CustomLogEvents.LiveBot,"{Type} service starting!",_type.Name);
        _backgroundTask = Task.Run(async ()=>await QueueProcessor(),_cancellationTokenSource.Token);
        IsRunning = true;
        Logger.LogInformation(CustomLogEvents.LiveBot,"{Type} service has started!",_type.Name);
    }
    public void StopService()
    {
        Logger.LogInformation(CustomLogEvents.LiveBot,"{Type} service stopping!",_type.Name);
        _cancellationTokenSource.Cancel();
        _backgroundTask?.Wait();
        _queue.Dispose();
        IsRunning = false;
        Logger.LogInformation(CustomLogEvents.LiveBot,"{Type} service has stopped!",_type.Name);
    }
    
    private protected abstract Task ProcessQueueItem(T item);
    private async Task QueueProcessor()
    {
        foreach (T item in _queue.GetConsumingEnumerable(_cancellationTokenSource.Token))
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
        _queue.Add(value);
    }
    
    protected DiscordUser GetBotUser()
    {
        if(_client is null)
            throw new InvalidOperationException("Client is not set!");
        return _client.CurrentUser;
    }
    
}