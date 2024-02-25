using System.Collections.Immutable;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Attributes;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.TagCommands;

public class TagAutoCompleteProvider(IDbContextFactory<LiveBotDbContext> dbContextFactory, GeneralUtils generalUtils) : IAutoCompleteProvider
{
    public async ValueTask<Dictionary<string, object>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        if (ctx.Guild is null) return [];
        Guild guild = dbContext.Guilds.Include(x=>x.Tags).First(x=>x.Id == ctx.Guild.Id);
        if (guild.Tags is null) return [];
        
        var tags = guild.Tags.Where(x=>x.GuildId == ctx.Guild.Id)
            .OrderBy(tag => generalUtils.CalculateLevenshteinDistance(ctx.UserInput.ToString()??"", tag.Name))
            .ToImmutableList();
        return tags.ToDictionary<Tag, string, object>(tag => $"{tag.Name} ({(tag.Content.Length > 50 ? tag.Content[..50] + "..." : tag.Content)})", tag => tag.Id.ToString());
    }
    
}