using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.TagCommands;

public class TagAutoCompleteProvider(IDbContextFactory<LiveBotDbContext> dbContextFactory, GeneralUtils generalUtils) : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        if (ctx.Guild is null) return new List<DiscordAutoCompleteChoice>();
        Guild guild = dbContext.Guilds.Include(x=>x.Tags).First(x=>x.Id == ctx.Guild.Id);
        if (guild.Tags is null) return new List<DiscordAutoCompleteChoice>();
        string userInput = ctx.UserInput ?? string.Empty;
        var tags = guild.Tags.Select(x =>
        {
            string preview = x.Content.Length > 50 ? x.Content[..50] + "..." : x.Content;
            var combined = $"{x.Name} ({preview})";
            double score = generalUtils.CalculateStringSimilarity(userInput, combined);
            return (score, Result: x);
        });
        var orderedTag = tags
            .OrderByDescending(x => x.score)
            .Select(x => x.Result);
        return orderedTag.Select(tag => new DiscordAutoCompleteChoice($"{tag.Name} ({(tag.Content.Length > 50 ? tag.Content[..50] + "..." : tag.Content)})", tag.Id.ToString()));
    }
    
}