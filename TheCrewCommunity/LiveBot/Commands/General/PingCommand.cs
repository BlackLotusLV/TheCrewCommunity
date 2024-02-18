using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees.Attributes;

namespace TheCrewCommunity.LiveBot.Commands.General;

public sealed class PingCommand
{
    [Command("ping")]
    public async Task ExecuteAsync(CommandContext ctx)
    {
        await ctx.RespondAsync("Pong!");
    }
    
}