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
    public DiscordClient DiscordClient{get;}
}
public class LiveBotService : IHostedService, ILiveBotService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SystemEvents _systemEventsEventHandlers;
    private readonly IModeratorLoggingService _moderatorLoggingService;
    private readonly IModeratorWarningService _moderatorWarningService;
    private readonly IStreamNotificationService _streamNotificationService;
    private readonly IModMailService _modMailService;
    
    public DateTime StartTime { get; private set; } = DateTime.UtcNow;
    public DiscordClient DiscordClient { get; }

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
        DiscordClient = new DiscordClient(new DiscordConfiguration
        {
            Token = configuration.GetSection("Discord")["BotToken"] ?? throw new InvalidOperationException("Bot token not provided!"),
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
        DiscordClient.Logger.LogInformation(CustomLogEvents.LiveBot, "LiveBot is starting! Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");
        InteractivityConfiguration interactivityConfiguration = new();
        ulong guildId = 0;
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            guildId = 282478449539678210;
        }
        CommandsConfiguration commandsConfiguration = new()
        {
            ServiceProvider = _serviceProvider,
            DebugGuildId = guildId
        };
        CommandsExtension commandsExtension = DiscordClient.UseCommands(commandsConfiguration);
        
        DiscordClient.UseInteractivity(interactivityConfiguration);

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

        _moderatorLoggingService.StartService(DiscordClient);
        _moderatorWarningService.StartService(DiscordClient);
        _streamNotificationService.StartService(DiscordClient);

        Timer streamCleanupTimer = new(_ => _streamNotificationService.StreamListCleanup());
        Timer modMailCleanupTimer = new(_ => _modMailService.ModMailCleanupAsync(DiscordClient));
        streamCleanupTimer.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(10));
        modMailCleanupTimer.Change(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(2));

        //handle events
        DiscordClient.SessionCreated += _systemEventsEventHandlers.SessionCreated;
        DiscordClient.GuildAvailable += _systemEventsEventHandlers.GuildAvailable;
        DiscordClient.ClientErrored += _systemEventsEventHandlers.ClientErrored;

        commandsExtension.CommandExecuted += _systemEventsEventHandlers.CommandExecuted;
        commandsExtension.CommandErrored += _systemEventsEventHandlers.CommandErrored;

        DiscordClient.GuildAuditLogCreated += auditLogEvents.OnAuditLogCreated;

        DiscordClient.ComponentInteractionCreated += whiteListButton.OnButtonClick;

        DiscordClient.MessageCreated += userActivityTracker.Add_Points;

        DiscordClient.ComponentInteractionCreated += buttonRoles.OnButtonClick;

        DiscordClient.GuildMemberUpdated += membershipScreening.OnAcceptRules;

        DiscordClient.GuildMemberAdded += memberFlow.OnJoin;
        DiscordClient.GuildMemberRemoved += memberFlow.OnLeave;

        DiscordClient.MessageDeleted += deleteLog.OnMessageDeleted;
        DiscordClient.MessagesBulkDeleted += deleteLog.OnBulkDelete;

        DiscordClient.PresenceUpdated += livestreamNotification.OnPresenceChange;

        DiscordClient.ComponentInteractionCreated += getUserInfoOnButton.OnButtonClick;
        DiscordClient.ComponentInteractionCreated += getInfractionOnButton.OnButtonClick;

        DiscordClient.MessageCreated += mediaOnlyFilter.OnMessageCreated;

        DiscordClient.MessageCreated += floodFilter.OnMessageCreated;

        DiscordClient.VoiceStateUpdated += voiceActivityLog.OnVoiceStateUpdated;

        DiscordClient.MessageCreated += everyoneTagFilter.OnMessageCreated;

        DiscordClient.MessageCreated += discordInviteFilter.OnMessageCreated;

        DiscordClient.GuildMemberAdded += memberFlow.LogJoin;
        DiscordClient.GuildMemberRemoved += memberFlow.LogLeave;

        DiscordClient.ComponentInteractionCreated += _modMailService.OpenButton;
        DiscordClient.ComponentInteractionCreated += _modMailService.CloseButton;
        DiscordClient.MessageCreated += _modMailService.ProcessModMailDm;
        
        await commandsExtension.AddProcessorsAsync(
            new SlashCommandProcessor(),
            new UserCommandProcessor(),
            new MessageCommandProcessor()
            );
        commandsExtension.AddCommands(typeof(LiveBotService).Assembly);

        DiscordActivity botActivity = new("/send-modmail to open chat with moderators", DiscordActivityType.Playing);
        DiscordClient.Logger.LogInformation("LiveBot has started!");
        await DiscordClient.ConnectAsync(botActivity);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        DiscordClient.Logger.LogInformation("LiveBot stopping!, Uptime: {Uptime}", DateTime.UtcNow- StartTime);
        await DiscordClient.DisconnectAsync();
    }
}