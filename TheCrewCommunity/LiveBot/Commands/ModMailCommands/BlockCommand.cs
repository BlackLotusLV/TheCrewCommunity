using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.ModMailCommands;

public static class BlockCommand
{
    public static async Task ExecuteAsync(SlashCommandContext ctx, DiscordUser user, IDbContextFactory<LiveBotDbContext> dbContextFactory)
    {
        if (ctx.Guild is null)
        {
            await ctx.RespondAsync("This command can only be used in a guild channel");
            return;
        }
        await ctx.DeferResponseAsync(true);
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        GuildUser? guildUser = await dbContext.GuildUsers.FindAsync(user.Id, ctx.Guild.Id);
        if (guildUser == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{user.Username}({user.Id}) is not a member of this server"));
            return;
        }

        if (guildUser.IsModMailBlocked)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{user.Username}({user.Id}) is already blocked from using ModMail"));
            return;
        }

        guildUser.IsModMailBlocked = true;
        dbContext.GuildUsers.Update(guildUser);
        await dbContext.SaveChangesAsync();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{user.Username}({user.Id}) has been blocked from using ModMail"));
    }
}