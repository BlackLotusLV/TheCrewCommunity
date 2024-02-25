using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;
public static class UnWarnCommand
{
    public static async Task ExecuteAsync(IModeratorWarningService warningService ,SlashCommandContext ctx,DiscordUser user,long warningId = -1)
    {
        await ctx.DeferResponseAsync(true);
        await warningService.RemoveWarningAsync(user, ctx, (int)warningId);
    }
}