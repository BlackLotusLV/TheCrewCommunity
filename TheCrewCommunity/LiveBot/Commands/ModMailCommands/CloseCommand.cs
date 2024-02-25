using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModMailCommands;
public static class CloseCommand
{
    public static async Task ExecuteAsync(SlashCommandContext ctx, long id,IDbContextFactory<LiveBotDbContext> dbContextFactory, IModMailService modMailService)
    {
        await ctx.DeferResponseAsync(true);
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        ModMail? entry = await dbContext.ModMail.FindAsync(id);
        if (entry is not { IsActive: true })
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Could not find an active entry with this ID."));
            return;
        }

        await modMailService.CloseModMailAsync(ctx.Client, entry, ctx.User, $" Mod Mail closed by {ctx.User.Username}",
            $"**Mod Mail closed by {ctx.User.Username}!\n----------------------------------------------------**");
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"ModMail entry #{id} closed."));
    }
}