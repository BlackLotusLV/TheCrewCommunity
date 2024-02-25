using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees.Attributes;

namespace TheCrewCommunity.LiveBot.Commands.General;

public sealed class PingCommand
{
    [Command("ping"), Description("Checks if the bot is alive.")]
    public async Task ExecutePingAsync(CommandContext ctx)
    {
        await ctx.RespondAsync("Pong!");
    }
    
}