using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.Services;

public class ModMailCleanupService : IHostedService
{
    private readonly Timer _timer;
    private readonly IModMailService _modMailService;
    private readonly DiscordClient _client;
    private readonly ILogger<ModMailCleanupService> _logger;
    private readonly IDbContextFactory<LiveBotDbContext> _dbContextFactory;

    public ModMailCleanupService(
        IModMailService modMailService,
        DiscordClient client,
        ILoggerFactory loggerFactory,
        IDbContextFactory<LiveBotDbContext> dbContextFactory)
    {
        _modMailService = modMailService;
        _client = client;
        _logger = loggerFactory.CreateLogger<ModMailCleanupService>();
        _dbContextFactory = dbContextFactory;
        _timer = new Timer(_ => DoWork());
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(5));
        _logger.LogInformation(CustomLogEvents.ModMailCleanup,"Mod Mail Cleanup service started");
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Change(Timeout.Infinite, 0);
        _logger.LogInformation(CustomLogEvents.ModMailCleanup,"Mod Mail Cleanup service has stopped");
        return Task.CompletedTask;
    }
    private async void DoWork()
    {
        try
        {
            _logger.LogDebug(CustomLogEvents.ModMailCleanup, "Mod Mail cleanup started");
            await using LiveBotDbContext liveBotDbContext = await _dbContextFactory.CreateDbContextAsync();
            foreach (ModMail modMail in liveBotDbContext.ModMail.Where(mMail => mMail.IsActive && mMail.LastMessageTime.AddMinutes(_modMailService.TimeoutMinutes) < DateTime.UtcNow).ToList())
            {
                await _modMailService.CloseModMailAsync(_client, modMail, _client.CurrentUser, " Mod Mail timed out.", "**Mod Mail timed out.**\n----------------------------------------------------");
            }

            _logger.LogDebug(CustomLogEvents.ModMailCleanup, "Mod Mail cleanup finished");
        }
        catch (Exception ex)
        {
            _logger.LogError(CustomLogEvents.ModMailCleanup,ex, "An error occured in the ModMail Cleanup Process");
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}