using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.UserCommands;
using Serilog;
using Serilog.Events;
using TheCrewCommunity.LiveBot.DiscordEventHandlers;
using TheCrewCommunity.LiveBot.LogEnrichers;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot;
public interface ILiveBotService
{
    public DateTime StartTime{get;}
}
public class LiveBotService : IHostedService, ILiveBotService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IModeratorLoggingService _moderatorLoggingService;
    private readonly IModeratorWarningService _moderatorWarningService;
    private readonly IStreamNotificationService _streamNotificationService;
    private readonly IModMailService _modMailService;
    private readonly DiscordClient _discordClient;
    
    public DateTime StartTime { get; private set; } = DateTime.UtcNow;

    public LiveBotService(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        IModeratorLoggingService moderatorLoggingService,
        IModeratorWarningService moderatorWarningService,
        IStreamNotificationService streamNotificationService,
        IModMailService modMailService,
        DiscordClient discordClient)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.With(new EventIdEnricher())
            .WriteTo.Console( standardErrorFromLevel: LogEventLevel.Error ,outputTemplate:"[{Timestamp:yyyy:MM:dd HH:mm:ss} {Level:u3}] [{FormattedEventId}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog();
        _discordClient = discordClient;
        _serviceProvider = serviceProvider;
        _moderatorLoggingService = moderatorLoggingService;
        _moderatorWarningService = moderatorWarningService;
        _streamNotificationService = streamNotificationService;
        _modMailService = modMailService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _discordClient.Logger.LogInformation(CustomLogEvents.LiveBot, "LiveBot is starting! Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");
        InteractivityConfiguration interactivityConfiguration = new();
        ulong guildId = 0;
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            //guildId = 282478449539678210;
        }
        CommandsConfiguration commandsConfiguration = new()
        {
            DebugGuildId = guildId
        };
        CommandsExtension commandsExtension = _discordClient.UseCommands(commandsConfiguration);
        
        _discordClient.UseInteractivity(interactivityConfiguration);

        // start services

        _moderatorLoggingService.StartService(_discordClient);
        _moderatorWarningService.StartService(_discordClient);
        _streamNotificationService.StartService(_discordClient);

        Timer streamCleanupTimer = new(_ => _streamNotificationService.StreamListCleanup());
        Timer modMailCleanupTimer = new(_ => _modMailService.ModMailCleanupAsync(_discordClient));
        streamCleanupTimer.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(10));
        modMailCleanupTimer.Change(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(2));

        commandsExtension.CommandExecuted += SystemEvents.CommandExecuted;
        commandsExtension.CommandErrored += SystemEvents.CommandErrored;
        
        await commandsExtension.AddProcessorsAsync(
            new SlashCommandProcessor(),
            new UserCommandProcessor(),
            new MessageCommandProcessor()
            );
        commandsExtension.AddCommands(typeof(LiveBotService).Assembly);
        

        DiscordActivity botActivity = new("/send-modmail to open chat with moderators", DiscordActivityType.Playing);
        _discordClient.Logger.LogInformation("LiveBot has started!");
        await _discordClient.ConnectAsync(botActivity);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _discordClient.Logger.LogInformation("LiveBot stopping!, Uptime: {Uptime}", DateTime.UtcNow- StartTime);
        await _discordClient.DisconnectAsync();
    }
}