using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public static class CreateFaqCommand
{
    public static async Task ExecuteAsync(InteractivityExtension interactivity, SlashCommandContext ctx)
    {
        var customId = $"FAQ-{ctx.User.Id}";
        DiscordModalBuilder modal = new DiscordModalBuilder().WithTitle("New FAQ entry").WithCustomId(customId)
            .AddTextInput(new DiscordTextInputComponent("Question", "Question", required: true, style: DiscordTextInputStyle.Paragraph), "Question")
            .AddTextInput(new DiscordTextInputComponent("Answer", "Answer", "Answer to the question", required: true, style: DiscordTextInputStyle.Paragraph), "Answer");
        await ctx.RespondWithModalAsync(modal);

        var response = await interactivity.WaitForModalAsync(customId, ctx.User);
        if (response.TimedOut) return;
        
        response.Result.Values.TryGetValue("Question", out IModalSubmission? questionValue);
        response.Result.Values.TryGetValue("Answer", out IModalSubmission? answerValue);
        string question = questionValue is TextInputModalSubmission textInput ? textInput.Value : "";
        string answer = answerValue is TextInputModalSubmission textInput2 ? textInput2.Value : "";
        await new DiscordMessageBuilder()
            .WithContent($"**Q: {question}**\n *A: {answer.TrimEnd()}*")
            .SendAsync(ctx.Channel);
        await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("FAQ message created!").AsEphemeral());
    }
}