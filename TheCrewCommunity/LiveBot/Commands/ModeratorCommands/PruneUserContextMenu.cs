using DSharpPlus.Commands;
using DSharpPlus.Entities;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;
public static class PruneUserContextMenu
{
    public static async Task ExecuteAsync(CommandContext ctx, DiscordMessage targetMessage)
    {
        const int messageAgeLimit = 14;
        const int batchSize = 100;
        await ctx.DeferResponseAsync();
        if (targetMessage.Timestamp.UtcDateTime < DateTime.UtcNow.AddDays(-messageAgeLimit))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Message is older than 14 days, cannot prune"));
            return;
        }
        List<DiscordMessage> messages= [targetMessage];
        while (true)
        {
            var messageCount = 0;
            await foreach (DiscordMessage message in ctx.Channel.GetMessagesAfterAsync(messages.Last().Id))
            {
                messageCount++;
                if (message.Author == targetMessage.Author)
                {
                    messages.Add(message);
                }
            }
            if (messageCount < batchSize)
            {
                break;
            }
        }

        await ctx.Channel.DeleteMessagesAsync(messages);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Selected messages have been pruned"));
    }
}