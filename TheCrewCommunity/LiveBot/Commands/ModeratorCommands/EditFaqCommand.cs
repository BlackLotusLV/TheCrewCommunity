using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public static class EditFaqCommand
{
    public static async Task ExecuteAsync(InteractivityExtension interactivity, SlashCommandContext ctx, string messageId)
    {
        DiscordMessage message = await ctx.Channel.GetMessageAsync(Convert.ToUInt64(messageId));
        string ogMessage = message.Content.Replace("*", string.Empty);
        string question = ogMessage.Substring(ogMessage.IndexOf(':') + 1, ogMessage.Length - (ogMessage[ogMessage.IndexOf('\n')..].Length + 2))
            .TrimStart();
        string answer = ogMessage[(ogMessage.IndexOf('\n') + 4)..].TrimStart();

        var customId = $"FAQ-Editor-{ctx.User.Id}";
        DiscordModalBuilder modal = new DiscordModalBuilder().WithTitle("FAQ Editor").WithCustomId(customId)
            .AddTextInput(new DiscordTextInputComponent("Question", "Question", question, true, DiscordTextInputStyle.Paragraph), "Question")
            .AddTextInput(new DiscordTextInputComponent("Answer", "Answer", answer, true, DiscordTextInputStyle.Paragraph), "Answer");

        await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);

        var response = await interactivity.WaitForModalAsync(customId, ctx.User);
        if (!response.TimedOut)
        {
            response.Result.Values.TryGetValue("Question", out IModalSubmission? questionValue);
            response.Result.Values.TryGetValue("Answer", out IModalSubmission? answerValue);
            string newQuestion = questionValue is TextInputModalSubmission textInput ? textInput.Value : "";
            string newAnswer = answerValue is TextInputModalSubmission textInput2 ? textInput2.Value : "";
            await message.ModifyAsync($"**Q: {newQuestion}**\n *A: {newAnswer.TrimEnd()}*");
            await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("FAQ message edited").AsEphemeral());
        }
    }
}