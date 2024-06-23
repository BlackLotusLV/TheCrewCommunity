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
public class LiveBotService(
    IModeratorLoggingService moderatorLoggingService,
    IModeratorWarningService moderatorWarningService,
    IStreamNotificationService streamNotificationService,
    DiscordClient discordClient)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        discordClient.Logger.LogInformation(CustomLogEvents.LiveBot, "LiveBot is starting! Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");
        InteractivityConfiguration interactivityConfiguration = new();
        ulong guildId = 0;
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            guildId = 282478449539678210;
        }
        CommandsConfiguration commandsConfiguration = new()
        {
            DebugGuildId = guildId
        };
        CommandsExtension commandsExtension = discordClient.UseCommands(commandsConfiguration);
        
        discordClient.UseInteractivity(interactivityConfiguration);

        // start services

        moderatorLoggingService.StartService(discordClient);
        moderatorWarningService.StartService(discordClient);
        streamNotificationService.StartService(discordClient);

        Timer streamCleanupTimer = new(_ => streamNotificationService.StreamListCleanup());
        streamCleanupTimer.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(10));

        commandsExtension.CommandExecuted += SystemEvents.CommandExecuted;
        commandsExtension.CommandErrored += SystemEvents.CommandErrored;
        
        await commandsExtension.AddProcessorsAsync(
            new SlashCommandProcessor(),
            new UserCommandProcessor(),
            new MessageCommandProcessor()
            );
        commandsExtension.AddCommands(typeof(LiveBotService).Assembly);

        DiscordActivity botActivity = new("/send-modmail to open chat with moderators", DiscordActivityType.Playing);
        discordClient.Logger.LogInformation("LiveBot has started!");
        await discordClient.ConnectAsync(botActivity);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        discordClient.Logger.LogInformation("LiveBot is stopping!");
        await discordClient.DisconnectAsync();
    }
}