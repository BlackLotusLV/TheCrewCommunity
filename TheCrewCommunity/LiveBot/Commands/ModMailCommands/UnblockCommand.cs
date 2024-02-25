using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.ModMailCommands;

public static class UnblockCommand
{
    public static async Task ExecuteAsync(SlashCommandContext ctx, DiscordUser user, IDbContextFactory<LiveBotDbContext> dbContextFactory)
    {
        await ctx.DeferResponseAsync(true);
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        if (ctx.Guild is null)
        {
            throw new NullReferenceException("Guild is null, this should not happen");
        }
        GuildUser? guildUser = await dbContext.GuildUsers.FindAsync(user.Id, ctx.Guild.Id);
        if (guildUser == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{user.Username}({user.Id}) is not a member of this server"));
            return;
        }

        if (!guildUser.IsModMailBlocked)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{user.Username}({user.Id}) is not blocked from using ModMail"));
            return;
        }

        guildUser.IsModMailBlocked = false;
        dbContext.GuildUsers.Update(guildUser);
        await dbContext.SaveChangesAsync();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{user.Username}({user.Id}) has been unblocked from using ModMail"));
    }
}