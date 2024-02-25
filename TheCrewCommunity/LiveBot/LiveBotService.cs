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
using TheCrewCommunity.LiveBot.EventHandlers;
using TheCrewCommunity.LiveBot.LogEnrichers;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot;
public interface ILiveBotService
{
    public DateTime StartTime{get;}
}
public class LiveBotService : IHostedService, ILiveBotService
{
    private readonly DiscordClient _discordClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly SystemEvents _systemEventsEventHandlers;
    private readonly IModeratorLoggingService _moderatorLoggingService;
    private readonly IModeratorWarningService _moderatorWarningService;
    private readonly IStreamNotificationService _streamNotificationService;
    private readonly IModMailService _modMailService;
    
    public DateTime StartTime { get; private set; } = DateTime.UtcNow;
    
    public LiveBotService(SystemEvents systemEventsEventHandlers,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        IModeratorLoggingService moderatorLoggingService,
        IModeratorWarningService moderatorWarningService,
        IStreamNotificationService streamNotificationService,
        IModMailService modMailService)
    {
        _systemEventsEventHandlers = systemEventsEventHandlers;
        
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.With(new EventIdEnricher())
            .WriteTo.Console( standardErrorFromLevel: LogEventLevel.Error ,outputTemplate:"[{Timestamp:yyyy:MM:dd HH:mm:ss} {Level:u3}] [{FormattedEventId}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog();
        _discordClient = new DiscordClient(new DiscordConfiguration
        {
            Token = configuration.GetSection("Discord")["ClientSecret"] ?? throw new InvalidOperationException("Bot token not provided!"),
            TokenType = TokenType.Bot,
            ReconnectIndefinitely = true,
            Intents = DiscordIntents.All,
            LogUnknownEvents = false,
            LogUnknownAuditlogs = false,
            LoggerFactory = loggerFactory
        });
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
        ulong? guildId = null;
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            guildId = 282478449539678210;
        }
        CommandsConfiguration commandsConfiguration = new()
        {
            ServiceProvider = _serviceProvider,
            DebugGuildId = guildId
        };
        CommandsExtension commandsExtension = _discordClient.UseCommands(commandsConfiguration);
        
        _discordClient.UseInteractivity(interactivityConfiguration);

        var auditLogEvents = ActivatorUtilities.CreateInstance<AuditLogEvents>(_serviceProvider);
        var whiteListButton = ActivatorUtilities.CreateInstance<WhiteListButton>(_serviceProvider);
        var userActivityTracker = ActivatorUtilities.CreateInstance<UserActivityTracker>(_serviceProvider);
        var buttonRoles = ActivatorUtilities.CreateInstance<ButtonRoles>(_serviceProvider);
        var membershipScreening = ActivatorUtilities.CreateInstance<MembershipScreening>(_serviceProvider);
        var memberFlow = ActivatorUtilities.CreateInstance<MemberFlow>(_serviceProvider);
        var deleteLog = ActivatorUtilities.CreateInstance<DeleteLog>(_serviceProvider);
        var livestreamNotification = ActivatorUtilities.CreateInstance<LivestreamNotifications>(_serviceProvider);
        var getUserInfoOnButton = ActivatorUtilities.CreateInstance<GetUserInfoOnButton>(_serviceProvider);
        var getInfractionOnButton = ActivatorUtilities.CreateInstance<GetInfractionOnButton>(_serviceProvider);
        var mediaOnlyFilter = ActivatorUtilities.CreateInstance<MediaOnlyFilter>(_serviceProvider);
        var floodFilter = ActivatorUtilities.CreateInstance<FloodFilter>(_serviceProvider);
        var voiceActivityLog = ActivatorUtilities.CreateInstance<VoiceActivityLog>(_serviceProvider);
        var everyoneTagFilter = ActivatorUtilities.CreateInstance<EveryoneTagFilter>(_serviceProvider);
        var discordInviteFilter = ActivatorUtilities.CreateInstance<DiscordInviteFilter>(_serviceProvider);

        // start services

        _moderatorLoggingService.StartService(_discordClient);
        _moderatorWarningService.StartService(_discordClient);
        _streamNotificationService.StartService(_discordClient);

        Timer streamCleanupTimer = new(_ => _streamNotificationService.StreamListCleanup());
        Timer modMailCleanupTimer = new(_ => _modMailService.ModMailCleanupAsync(_discordClient));
        streamCleanupTimer.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(10));
        modMailCleanupTimer.Change(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(2));

        //handle events
        _discordClient.SessionCreated += _systemEventsEventHandlers.SessionCreated;
        _discordClient.GuildAvailable += _systemEventsEventHandlers.GuildAvailable;
        _discordClient.ClientErrored += _systemEventsEventHandlers.ClientErrored;

        commandsExtension.CommandExecuted += _systemEventsEventHandlers.CommandExecuted;
        commandsExtension.CommandErrored += _systemEventsEventHandlers.CommandErrored;

        _discordClient.GuildAuditLogCreated += auditLogEvents.OnAuditLogCreated;

        _discordClient.ComponentInteractionCreated += whiteListButton.OnButtonClick;

        _discordClient.MessageCreated += userActivityTracker.Add_Points;

        _discordClient.ComponentInteractionCreated += buttonRoles.OnButtonClick;

        _discordClient.GuildMemberUpdated += membershipScreening.OnAcceptRules;

        _discordClient.GuildMemberAdded += memberFlow.OnJoin;
        _discordClient.GuildMemberRemoved += memberFlow.OnLeave;

        _discordClient.MessageDeleted += deleteLog.OnMessageDeleted;
        _discordClient.MessagesBulkDeleted += deleteLog.OnBulkDelete;

        _discordClient.PresenceUpdated += livestreamNotification.OnPresenceChange;

        _discordClient.ComponentInteractionCreated += getUserInfoOnButton.OnButtonClick;
        _discordClient.ComponentInteractionCreated += getInfractionOnButton.OnButtonClick;

        _discordClient.MessageCreated += mediaOnlyFilter.OnMessageCreated;

        _discordClient.MessageCreated += floodFilter.OnMessageCreated;

        _discordClient.VoiceStateUpdated += voiceActivityLog.OnVoiceStateUpdated;

        _discordClient.MessageCreated += everyoneTagFilter.OnMessageCreated;

        _discordClient.MessageCreated += discordInviteFilter.OnMessageCreated;

        _discordClient.GuildMemberAdded += memberFlow.LogJoin;
        _discordClient.GuildMemberRemoved += memberFlow.LogLeave;

        _discordClient.ComponentInteractionCreated += _modMailService.OpenButton;
        _discordClient.ComponentInteractionCreated += _modMailService.CloseButton;
        _discordClient.MessageCreated += _modMailService.ProcessModMailDm;
        
        await commandsExtension.AddProcessorsAsync(
            new SlashCommandProcessor(),
            new UserCommandProcessor(),
            new MessageCommandProcessor()
            );
        commandsExtension.AddCommands(typeof(LiveBotService).Assembly);

        DiscordActivity botActivity = new("/send-modmail to open chat with moderators", ActivityType.Playing);
        _discordClient.Logger.LogInformation("LiveBot has started!");
        await _discordClient.ConnectAsync(botActivity);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _discordClient.Logger.LogInformation("LiveBot stopping!, Uptime: {Uptime}", DateTime.UtcNow- StartTime);
        await _discordClient.DisconnectAsync();
    }
}