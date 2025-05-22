using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class AddButtonCommand(InteractivityExtension interactivity)
{
    [Command("AddButton"), SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu), RequireGuild, RequirePermissions(DiscordPermission.ManageMessages)]
    public async Task AddButton(SlashCommandContext ctx, DiscordMessage targetMessage)
    {
        await ctx.RespondAsync("Command broken. Don't use");
        /*
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
        response.AddTextInputComponent(new DiscordTextInputComponent("Custom ID", "customId"));
        response.AddTextInputComponent(new DiscordTextInputComponent("Label", "label"));
        response.AddTextInputComponent(new DiscordTextInputComponent("Emoji", "emoji", required: false));

        await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, response);
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
            modified.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Primary, modalResponse.Result.Values["customId"], modalResponse.Result.Values["label"], emoji: emoji));
        }

        if (targetMessage.Components is not null && targetMessage.Components.Count > 0)
        {
            foreach (DiscordActionRowComponent row in targetMessage.Components)
            {
                if (row.Components.Count == 5)
                {
                    modified.AddActionRowComponent(row);
                }
                else
                {
                    var buttons = row.Components.ToList();
                    buttons.Add(new DiscordButtonComponent(DiscordButtonStyle.Primary, modalResponse.Result.Values["customId"], modalResponse.Result.Values["label"], emoji: emoji));
                    modified.addcompo(buttons.ToArray());
                }
            }
        }

        await targetMessage.ModifyAsync(modified);
        await modalResponse.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent($"Button added to the message. **Custom ID:** {modalResponse.Result.Values["customId"]}").AsEphemeral());
        */
    }
}