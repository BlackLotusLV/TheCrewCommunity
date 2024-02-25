using DSharpPlus;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public class EditFaqCommand
{
    public static async Task ExecuteAsync(SlashCommandContext ctx, string messageId)
    {
        DiscordMessage message = await ctx.Channel.GetMessageAsync(Convert.ToUInt64(messageId));
        string ogMessage = message.Content.Replace("*", string.Empty);
        string question = ogMessage.Substring(ogMessage.IndexOf(":", StringComparison.Ordinal) + 1, ogMessage.Length - (ogMessage[ogMessage.IndexOf("\n", StringComparison.Ordinal)..].Length + 2))
            .TrimStart();
        string answer = ogMessage[(ogMessage.IndexOf("\n", StringComparison.Ordinal) + 4)..].TrimStart();

        var customId = $"FAQ-Editor-{ctx.User.Id}";
        DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder().WithTitle("FAQ Editor").WithCustomId(customId)
            .AddComponents(new TextInputComponent("Question", "Question", null, question, true, TextInputStyle.Paragraph))
            .AddComponents(new TextInputComponent("Answer", "Answer", null, answer, true, TextInputStyle.Paragraph));

        await ctx.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var response = await interactivity.WaitForModalAsync(customId, ctx.User);
        if (!response.TimedOut)
        {
            await message.ModifyAsync($"**Q: {response.Result.Values["Question"]}**\n *A: {response.Result.Values["Answer"].TrimEnd()}*");
            await response.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("FAQ message edited").AsEphemeral());
        }
    }
}