using DSharpPlus;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Attributes;
using DSharpPlus.Commands.Trees.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class AddButtonCommand
{
    [Command("AddButton"), SlashCommandTypes(ApplicationCommandType.MessageContextMenu), RequireGuild, RequirePermissions(Permissions.ManageMessages)]
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
        response.AddComponents(new TextInputComponent("Custom ID", "customId"));
        response.AddComponents(new TextInputComponent("Label", "label"));
        response.AddComponents(new TextInputComponent("Emoji", "emoji", required: false));

        await ctx.Interaction.CreateResponseAsync(InteractionResponseType.Modal, response);
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
            modified.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, modalResponse.Result.Values["customId"], modalResponse.Result.Values["label"], emoji: emoji));
        }

        foreach (DiscordActionRowComponent row in targetMessage.Components)
        {
            if (row.Components.Count == 5)
            {
                modified.AddComponents(row);
            }
            else
            {
                var buttons = row.Components.ToList();
                buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, modalResponse.Result.Values["customId"], modalResponse.Result.Values["label"], emoji: emoji));
                modified.AddComponents(buttons);
            }
        }

        await targetMessage.ModifyAsync(modified);
        await modalResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent($"Button added to the message. **Custom ID:** {modalResponse.Result.Values["customId"]}").AsEphemeral());
    }
}