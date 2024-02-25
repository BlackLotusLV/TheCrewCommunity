using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public static class PruneCommand
{
    public static async Task ExecuteAsync(SlashCommandContext ctx, long messageCount)
    {
        await ctx.DeferResponseAsync(true);
        if (messageCount > 100)
        {
            messageCount = 100;
        }

        var messageList = ctx.Channel.GetMessagesAsync((int)messageCount);
        await ctx.Channel.DeleteMessagesAsync(messageList);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Selected messages have been pruned"));
    }
}