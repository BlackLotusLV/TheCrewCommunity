using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public static class UnMuteUserCommand
{
    public static async Task ExecuteAsync(SlashCommandContext ctx, DiscordMember member, string reason)
    {
        await ctx.DeferResponseAsync();
        if (!member.IsMuted)
        {
            await ctx.RespondAsync($"{member.Mention} is not muted. No action taken", true);
            return;
        }
        await member.SetMuteAsync(false, reason);
        await ctx.RespondAsync($"{member.Mention} has been un-muted.", true);
        ctx.Client.Logger.LogInformation("User {User}({UserId}) has been un-muted by {Moderator} in {Guild}.", member.Username, member.Id, ctx.User.Username, member.Guild.Name);
    }
}