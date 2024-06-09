using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers.ComponentInteractionCreated;

public static class GetUserInfoOnButton
{
    public static async Task OnButtonClick(DiscordClient client, ComponentInteractionCreatedEventArgs e)
    {
        var moderatorWarningService = client.ServiceProvider.GetRequiredService<IModeratorWarningService>();
        if (e.Interaction is not { Type: DiscordInteractionType.Component, User.IsBot: false }|| !e.Interaction.Data.CustomId.Contains(moderatorWarningService.UserInfoButtonPrefix) || e.Interaction.Guild is null) return;
        string idString = e.Interaction.Data.CustomId.Replace(moderatorWarningService.UserInfoButtonPrefix, "");
        if(!ulong.TryParse(idString,out ulong userId)) return;
        DiscordUser user = await client.GetUserAsync(userId);
        DiscordEmbed embed = await moderatorWarningService.GetUserInfoAsync(e.Guild, user);
        DiscordInteractionResponseBuilder response = new()
        {
            IsEphemeral = true
        };
        response.AddEmbed(embed);
        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, response);
    }
}