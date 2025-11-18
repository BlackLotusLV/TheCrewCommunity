using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers.ComponentInteractionCreated;

public static class HandleEvent
{
    public const string ButtonRolePrefix = "ButtonRole-";
    private const string WhiteListPrefix = "Activate";
    public static async Task OnButtonPress(DiscordClient client, ComponentInteractionCreatedEventArgs eventArgs)
    {
        if (eventArgs.Interaction is not { Type: DiscordInteractionType.Component, User.IsBot: false }) return;
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        var moderatorLoggingService = client.ServiceProvider.GetRequiredService<IModeratorLoggingService>();
        var modMailService = client.ServiceProvider.GetRequiredService<IModMailService>();
        var warningService = client.ServiceProvider.GetRequiredService<IModeratorWarningService>();
        var thisOrThatDailyVoteService = client.ServiceProvider.GetRequiredService<IThisOrThatDailyVoteService>();
        string customId = eventArgs.Interaction.Data.CustomId;
        Task task = customId switch
        {
            _ when customId.Contains(modMailService.OpenButtonPrefix) => modMailService.OpenButton(client, eventArgs),
            _ when customId.Contains(modMailService.CloseButtonPrefix) => modMailService.CloseButton(client, eventArgs),
            _ when customId.Contains(warningService.InfractionButtonPrefix) => GetInfractionOnButton.OnButtonClick(client, eventArgs),
            _ when customId.Contains(warningService.UserInfoButtonPrefix) => GetUserInfoOnButton.OnButtonClick(client, eventArgs),
            _ when customId.Contains(ButtonRolePrefix) => GetRole.OnButtonClickAsync(client,eventArgs),
            _ when customId.Contains(WhiteListPrefix) => WhiteListCheck.OnButtonClick(client,eventArgs),
            _ when customId.Contains("DailyVote")=>thisOrThatDailyVoteService.Vote(client, eventArgs),
            _ => Task.CompletedTask
        };
        await task;
    }
}