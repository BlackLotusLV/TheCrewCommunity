using DSharpPlus;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public class CreateFaqCommand
{
    public static async Task ExecuteAsync(SlashCommandContext ctx)
    {
        var customId = $"FAQ-{ctx.User.Id}";
        DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder().WithTitle("New FAQ entry").WithCustomId(customId)
            .AddComponents(new TextInputComponent("Question", "Question", null, null, true, TextInputStyle.Paragraph))
            .AddComponents(new TextInputComponent("Answer", "Answer", "Answer to the question", null, true, TextInputStyle.Paragraph));
        await ctx.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var response = await interactivity.WaitForModalAsync(customId, ctx.User);
        if (!response.TimedOut)
        {
            await new DiscordMessageBuilder()
                .WithContent($"**Q: {response.Result.Values["Question"]}**\n *A: {response.Result.Values["Answer"].TrimEnd()}*")
                .SendAsync(ctx.Channel);
            await response.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("FAQ message created!").AsEphemeral());
        }
    }
}