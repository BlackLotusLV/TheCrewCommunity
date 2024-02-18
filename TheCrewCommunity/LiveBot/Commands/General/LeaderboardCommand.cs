using System.ComponentModel;
using System.Text;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Attributes;
using DSharpPlus.Commands.Trees.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class LeaderboardCommand(IDbContextFactory<LiveBotDbContext> dbContextFactory)
{
    [Command("leaderboard"), Description("Shows the leaderboard of the server"), RequireGuild]
    public async Task ExecuteAsync(SlashCommandContext ctx, [SlashMinMaxValue(MinValue = 1)] long page = 1)
    {
        await ctx.DeferResponseAsync();
        List<DiscordButtonComponent> buttons =
        [
            new DiscordButtonComponent(ButtonStyle.Primary, "left", "", false, new DiscordComponentEmoji("◀️")),
            new DiscordButtonComponent(ButtonStyle.Danger, "end", "", false, new DiscordComponentEmoji("⏹")),
            new DiscordButtonComponent(ButtonStyle.Primary, "right", "", false, new DiscordComponentEmoji("▶️"))
        ];
        string board = await GenerateLeaderboardAsync(ctx, (int)page);
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

            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
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
            .ToListAsync();
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine("```csharp\n📋 Rank | Username");
        for (int i = page * 10 - 10; i < page * 10; i++)
        {
            DiscordUser user = await ctx.Client.GetUserAsync(activityList[i].UserID);
            User? userInfo = await dbContext.Users.FindAsync(user.Id);
            stringBuilder.Append($"[{i + 1}]\t# {user.Username}\n\t\t\tPoints:{activityList[i].Points}");
            if (userInfo != null)
            {
                stringBuilder.AppendLine($"\t\t🍪:{userInfo.CookiesTaken}/{userInfo.CookiesGiven}");
            }

            if (i == activityList.Count - 1)
            {
                i = page * 10;
            }
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
}