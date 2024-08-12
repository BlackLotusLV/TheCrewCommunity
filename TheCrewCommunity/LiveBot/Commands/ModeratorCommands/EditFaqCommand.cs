using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
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
        DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder().WithTitle("FAQ Editor").WithCustomId(customId)
            .AddComponents(new DiscordTextInputComponent("Question", "Question", null, question, true, DiscordTextInputStyle.Paragraph))
            .AddComponents(new DiscordTextInputComponent("Answer", "Answer", null, answer, true, DiscordTextInputStyle.Paragraph));

        await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);

        var response = await interactivity.WaitForModalAsync(customId, ctx.User);
        if (!response.TimedOut)
        {
            await message.ModifyAsync($"**Q: {response.Result.Values["Question"]}**\n *A: {response.Result.Values["Answer"].TrimEnd()}*");
            await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("FAQ message edited").AsEphemeral());
        }
    }
}