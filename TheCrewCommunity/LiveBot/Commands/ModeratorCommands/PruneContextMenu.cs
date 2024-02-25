using DSharpPlus.Commands;
using DSharpPlus.Entities;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;
public static class PruneContextMenu
{
    public static async Task ExecuteAsync(CommandContext ctx, DiscordMessage targetMessage)
    {
        await ctx.DeferResponseAsync();
        if (targetMessage.Timestamp.UtcDateTime < DateTime.UtcNow.AddDays(-14))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Message is older than 14 days, cannot prune"));
            return;
        }
        List<DiscordMessage> messages= [targetMessage];
        var end = false;
        while (!end)
        {
            var temp = ctx.Channel.GetMessagesAfterAsync(messages.Last().Id)
                .ToBlockingEnumerable()
                .ToList();
            messages.AddRange(temp);
            if (temp.Count < 100)
            {
                end = true;
            }
        }
        
        await ctx.Channel.DeleteMessagesAsync(messages);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Selected messages have been pruned"));
    }
}