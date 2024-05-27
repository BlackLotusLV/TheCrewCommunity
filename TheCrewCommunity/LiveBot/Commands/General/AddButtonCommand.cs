using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class AddButtonCommand
{
    [Command("AddButton"), SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu), RequireGuild, RequirePermissions(DiscordPermissions.ManageMessages)]
    public async Task AddButton(SlashCommandContext ctx, DiscordMessage targetMessage)
    {
        if (targetMessage.Author is null || targetMessage.Author != ctx.Client.CurrentUser)
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("To add a button, the bot must be the author of the message. Try again").AsEphemeral());
            return;
        }

        var customId = $"AddButton-{targetMessage.Id}-{ctx.User.Id}";
        DiscordInteractionResponseBuilder response = new()
        {
            Title = "Button Parameters",
            CustomId = customId
        };
        response.AddComponents(new DiscordTextInputComponent("Custom ID", "customId"));
        response.AddComponents(new DiscordTextInputComponent("Label", "label"));
        response.AddComponents(new DiscordTextInputComponent("Emoji", "emoji", required: false));

        await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, response);
        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var modalResponse = await interactivity.WaitForModalAsync(customId, ctx.User);

        if (modalResponse.TimedOut) return;

        DiscordMessageBuilder modified = new DiscordMessageBuilder()
            .WithContent(targetMessage.Content)
            .AddEmbeds(targetMessage.Embeds);

        DiscordComponentEmoji? emoji = null;
        if (modalResponse.Result.Values["emoji"] != string.Empty)
        {
            emoji = ulong.TryParse(modalResponse.Result.Values["emoji"], out ulong emojiId) ? new DiscordComponentEmoji(emojiId) : new DiscordComponentEmoji(modalResponse.Result.Values["emoji"]);
        }

        if (targetMessage.Components is null || targetMessage.Components.Count == 0)
        {
            modified.AddComponents(new DiscordButtonComponent(DiscordButtonStyle.Primary, modalResponse.Result.Values["customId"], modalResponse.Result.Values["label"], emoji: emoji));
        }

        if (targetMessage.Components is not null && targetMessage.Components.Count > 0)
        {
            foreach (DiscordActionRowComponent row in targetMessage.Components)
            {
                if (row.Components.Count == 5)
                {
                    modified.AddComponents(row);
                }
                else
                {
                    var buttons = row.Components.ToList();
                    buttons.Add(new DiscordButtonComponent(DiscordButtonStyle.Primary, modalResponse.Result.Values["customId"], modalResponse.Result.Values["label"], emoji: emoji));
                    modified.AddComponents(buttons);
                }
            }
        }

        await targetMessage.ModifyAsync(modified);
        await modalResponse.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent($"Button added to the message. **Custom ID:** {modalResponse.Result.Values["customId"]}").AsEphemeral());
    }
}