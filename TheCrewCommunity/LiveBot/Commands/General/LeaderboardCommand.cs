using System.ComponentModel;
using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class LeaderboardCommand(IDbContextFactory<LiveBotDbContext> dbContextFactory)
{
    [Command("leaderboard"), Description("Shows the leaderboard of the server"), RequireGuild]
    public async Task ExecuteAsync(SlashCommandContext ctx, [MinMaxValue(1)] int page = 1)
    {
        await ctx.DeferResponseAsync();
        List<DiscordButtonComponent> buttons =
        [
            new DiscordButtonComponent(DiscordButtonStyle.Primary, "left", "", false, new DiscordComponentEmoji("◀️")),
            new DiscordButtonComponent(DiscordButtonStyle.Danger, "end", "", false, new DiscordComponentEmoji("⏹")),
            new DiscordButtonComponent(DiscordButtonStyle.Primary, "right", "", false, new DiscordComponentEmoji("▶️"))
        ];
        string board = await GenerateLeaderboardAsync(ctx, page);
        DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(board).AddComponents(buttons));

        var end = false;
        do
        {
            var result = await message.WaitForButtonAsync(ctx.User, TimeSpan.FromSeconds(30));
            if (result.TimedOut)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(message.Content));
                return;
            }

            switch (result.Result.Id)
            {
                case "left":
                    if (page > 1)
                    {
                        page--;
                        board = await GenerateLeaderboardAsync(ctx, (int)page);
                        await message.ModifyAsync(board);
                    }

                    break;

                case "right":
                    page++;
                    try
                    {
                        board = await GenerateLeaderboardAsync(ctx, (int)page);
                        await message.ModifyAsync(board);
                    }
                    catch (Exception)
                    {
                        page--;
                    }

                    break;
                case "end":
                    end = true;
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(message.Content));
                    break;
            }

            await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
        } while (!end);
    }
    private async Task<string> GenerateLeaderboardAsync(AbstractContext ctx, int page)
    {
        if (ctx.Guild is null)
        {
            throw new NullReferenceException("Guild is null");
        }
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var activityList = await dbContext.UserActivity
            .Where(x => x.Date > DateTime.UtcNow.AddDays(-30) && x.GuildId == ctx.Guild.Id)
            .GroupBy(x => x.UserDiscordId)
            .Select(g => new { UserID = g.Key, Points = g.Sum(x => x.Points) })
            .OrderByDescending(x => x.Points)
            .Skip((page - 1) * 10)
            .Take(10)
            .ToListAsync();
        
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine("```csharp\n📋 Rank | Username");
        for (var i = 0; i < activityList.Count; i++)
        {
            DiscordUser user = await ctx.Client.GetUserAsync(activityList[i].UserID) ?? throw new Exception($"User with ID {activityList[i].UserID} not found");
            User? userInfo = await dbContext.Users.FindAsync(user.Id);
            stringBuilder.Append(BuildLeaderboardString(i,user,userInfo,activityList[i].Points));
        }

        var rank = 0;
        StringBuilder personalScore = new();
        foreach (var item in activityList)
        {
            rank++;
            if (item.UserID != ctx.User.Id) continue;
            User? userInfo = await dbContext.Users.FirstOrDefaultAsync(w => w.DiscordId == ctx.User.Id);
            personalScore.Append($"⭐Rank: {rank}\t Points: {item.Points}");
            if (userInfo == null) continue;
            personalScore.AppendLine($"\t🍪:{userInfo.CookiesTaken}/{userInfo.CookiesGiven}");
            break;
        }

        stringBuilder.AppendLine($"\n# Your Ranking\n{personalScore.ToString()}\n```");
        return stringBuilder.ToString();
    }
    
    private static string BuildLeaderboardString(int rank, DiscordUser user, User? userInfo, int points)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append($"[{rank}]\t# {user.Username}\n\t\t\tPoints:{points}");
        if (userInfo != null)
        {
            stringBuilder.AppendLine($"\t\t🍪:{userInfo.CookiesTaken}/{userInfo.CookiesGiven}");
        }
        return stringBuilder.ToString();
    }
}