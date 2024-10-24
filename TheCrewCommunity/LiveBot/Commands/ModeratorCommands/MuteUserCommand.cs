using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public static class MuteUserCommand
{
    public static async Task ExecuteAsync(SlashCommandContext ctx, DiscordMember member, string reason)
    {
        await ctx.DeferResponseAsync();
        if (member.IsMuted)
        {
            await ctx.RespondAsync($"{member.Mention} is already muted.", true);
            return;
        }
        await member.SetMuteAsync(true, reason);
        await ctx.RespondAsync($"{member.Mention} has been muted.", true);
        ctx.Client.Logger.LogInformation("User {User}({UserId}) has been muted by {Moderator} in {Guild}.", member.Username, member.Id, ctx.User.Username, member.Guild.Name);
    }
}