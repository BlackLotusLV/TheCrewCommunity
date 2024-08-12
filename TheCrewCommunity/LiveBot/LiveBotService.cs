using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.UserCommands;
using TheCrewCommunity.LiveBot.DiscordEventHandlers;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot;
public class LiveBotService(
    IModeratorLoggingService moderatorLoggingService,
    IModeratorWarningService moderatorWarningService,
    DiscordClient discordClient,
    IUserActivityService userActivityService)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        discordClient.Logger.LogInformation(CustomLogEvents.LiveBot, "LiveBot is starting! Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");
        // start services

        moderatorLoggingService.StartService(discordClient);
        moderatorWarningService.StartService(discordClient);
        await userActivityService.StartAsync();

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