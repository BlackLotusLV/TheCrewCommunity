using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.EventHandlers;

public class GetInfractionOnButton(IModeratorWarningService moderatorWarningService)
{
    public async Task OnButtonClick(DiscordClient client, ComponentInteractionCreateEventArgs e)
    {
        if (e.Interaction is not { Type: DiscordInteractionType.Component, User.IsBot: false }|| !e.Interaction.Data.CustomId.Contains(moderatorWarningService.InfractionButtonPrefix) || e.Interaction.Guild is null) return;
        string idString = e.Interaction.Data.CustomId.Replace(moderatorWarningService.InfractionButtonPrefix, "");
        if(!ulong.TryParse(idString,out ulong userId)) return;
        await e.Interaction.DeferAsync(true);
        DiscordUser user = await client.GetUserAsync(userId);
        var embeds = await moderatorWarningService.BuildInfractionsEmbedsAsync(e.Guild, user, true);
        var currentPage = 1;
        DiscordWebhookBuilder webhookBuilder = new();
        webhookBuilder.AddEmbed(embeds[0]);
        if (embeds.Count > 1)
        {
            webhookBuilder.AddEmbed(embeds[1]);
        }
        if (embeds.Count <= 2)
        {
            await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
            return;
        }
        
        string leftButtonId = $"left_{e.User.Id}",
            rightButtonId = $"right_{e.User.Id}",
            stopButtonId = $"stop_{e.User.Id}";
        DiscordButtonComponent leftButton = new(DiscordButtonStyle.Primary, leftButtonId, "", true, new DiscordComponentEmoji("⬅️")),
            stopButton = new(DiscordButtonStyle.Danger, stopButtonId, "", false, new DiscordComponentEmoji("⏹️")),
            rightButton = new(DiscordButtonStyle.Primary, rightButtonId, "", false, new DiscordComponentEmoji("➡️"));

        webhookBuilder.AddEmbed(embeds[currentPage])
            .AddComponents(leftButton, stopButton, rightButton);
        DiscordMessage message = await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
        
        while (true)
        {
            var result = await message.WaitForButtonAsync(e.User, TimeSpan.FromSeconds(30));
            if (result.TimedOut || result.Result.Id == stopButtonId)
            {
                webhookBuilder.ClearComponents();
                await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                return;
            }
            webhookBuilder = new DiscordWebhookBuilder();
            if (result.Result.Id == leftButtonId)
            {
                currentPage--;
                if (currentPage == 1)
                {
                    leftButton.Disable();
                }
                if (rightButton.Disabled)
                {
                    rightButton.Enable();
                }

            }else if (result.Result.Id == rightButtonId)
            {
                currentPage++;
                if (currentPage == embeds.Count - 1)
                {
                    rightButton.Disable();
                }
                if (leftButton.Disabled)
                {
                    leftButton.Enable();
                }
            }
            webhookBuilder
                .AddEmbeds(new []{embeds[0],embeds[currentPage]})
                .AddComponents(leftButton, stopButton, rightButton);
            await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
            await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
        }
    }
}