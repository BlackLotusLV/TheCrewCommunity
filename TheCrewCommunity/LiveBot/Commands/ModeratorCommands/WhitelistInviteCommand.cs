using System.ComponentModel;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;
public static class WhitelistInviteCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory, SlashCommandContext ctx, [Description("The invite code to be whitelisted")] string code)
    {
        if (ctx.Guild is null)
        {
            await ctx.RespondAsync("This command can only be used in a guild channel");
            return;
        }
        await ctx.DeferResponseAsync(true);
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guild? guild = await dbContext.Guilds.Include(x=>x.WhitelistedVanities).FirstOrDefaultAsync(x=>x.Id==ctx.Guild.Id);
        if (guild is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This guild is not in the database"));
            return;
        }

        if (guild.WhitelistedVanities is not null && guild.WhitelistedVanities.Any(x=>x.VanityCode==code))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This code is already whitelisted"));
            return;
        }
        await dbContext.VanityWhitelist.AddAsync(new VanityWhitelist
        {
            GuildId = ctx.Guild.Id,
            VanityCode = code
        });
        await dbContext.SaveChangesAsync();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Invite code {code} has been whitelisted"));
        ctx.Client.Logger.LogInformation(CustomLogEvents.InviteLinkFilter, "Invite code {Code} has been whitelisted in {GuildName}({GuildId}) by {Username}({UserId})",
            code, ctx.Guild.Name, ctx.Guild.Id, ctx.User.Username, ctx.User.Id);
    }
}