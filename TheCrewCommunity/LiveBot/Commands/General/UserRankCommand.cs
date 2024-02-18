using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Trees.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.General;

public sealed class UserRankCommand(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService)
{
    [Command("rank"), Description("Shows your rank based on past 30 days of activity"), RequireGuild]
    public async Task ExecuteAsync(CommandContext ctx, DiscordMember? member = null)
    {
        await ctx.DeferResponseAsync();
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var activityList = await dbContext.UserActivity
            .Where(x => x.Date > DateTime.UtcNow.AddDays(-30) && x.GuildId == ctx.Guild!.Id)
            .GroupBy(x => x.UserDiscordId)
            .Select(g => new { UserID = g.Key, Points = g.Sum(x => x.Points) })
            .OrderByDescending(x => x.Points)
            .ToListAsync();
        member ??= ctx.Member;
        if (member is null)
        {
            await ctx.RespondAsync("Could not find your rank in the database");
            throw new NullReferenceException("Member is null");
        }
        User? userInfo = await dbContext.Users.FindAsync(member.Id);
        if (userInfo == null)
        {
            await ctx.RespondAsync("Could not find your rank in the database");
            await databaseMethodService.AddUserAsync(new User(member.Id));
            return;
        }

        var rank = 0;
        foreach (var item in activityList)
        {
            rank++;
            if (item.UserID != member.Id) continue;
            await ctx.RespondAsync(
                $"You are ranked **#{rank}** in {ctx.Guild!.Name} server with **{item.Points}** points. Your cookie stats are: {userInfo.CookiesTaken} Received /  {userInfo.CookiesGiven} Given");
            break;
        }
    }
}