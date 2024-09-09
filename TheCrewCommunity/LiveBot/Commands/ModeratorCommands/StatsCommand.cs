using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public static class StatsCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory,CommandContext ctx)
    {
        await ctx.DeferResponseAsync();
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        if (ctx.Guild is null)
        {
            await ctx.EditResponseAsync("An error occured, check logs");
            throw new NullReferenceException("Guild is null. This should not happen.");
        }
        var leaderboard = await dbContext.Infractions.Where(x => x.GuildId == ctx.Guild.Id).Select(x => new { UserId = x.AdminDiscordId, Type = x.InfractionType }).ToListAsync();
        var groupedLeaderboard = leaderboard
            .GroupBy(x => x.UserId)
            .Select(x => new
            {
                UserId = x.Key,
                Kicks = x.Count(y => y.Type == InfractionType.Kick),
                Bans = x.Count(y => y.Type == InfractionType.Ban),
                Warnings = x.Count(y => y.Type == InfractionType.Warning)
            })
            .OrderByDescending(x => x.Warnings)
            .ThenByDescending(x => x.Kicks)
            .ThenByDescending(x => x.Bans)
            .ToList();
        StringBuilder leaderboardBuilder = new();
        leaderboardBuilder.AppendLine("```");
        leaderboardBuilder.AppendLine("User".PadRight(30) + "Warnings".PadRight(10) + "Kicks".PadRight(10) + "Bans".PadRight(10));
        foreach (var user in groupedLeaderboard)
        {
            DiscordUser discordUser;
            try
            {
                discordUser = await ctx.Client.GetUserAsync(user.UserId);
            }
            catch (Exception e)
            {
                ctx.Client.Logger.LogError(e, "Failed to get user from Discord API. User ID: {ID}", user.UserId);
                continue;
            }
            leaderboardBuilder.AppendLine($"{discordUser.Username}#{discordUser.Discriminator}".PadRight(30) + $"{user.Warnings}".PadRight(10) + $"{user.Kicks}".PadRight(10) +
                                          $"{user.Bans}".PadRight(10));
        }

        leaderboardBuilder.AppendLine("```");
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(leaderboardBuilder.ToString()));
    }
}