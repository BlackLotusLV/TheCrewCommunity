using System.Collections.Immutable;
using System.Collections.ObjectModel;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.TagCommands;

public class TagAutoCompleteProvider(IDbContextFactory<LiveBotDbContext> dbContextFactory, GeneralUtils generalUtils) : IAutoCompleteProvider
{
    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        if (ctx.Guild is null) return ReadOnlyDictionary<string, object>.Empty;
        Guild guild = dbContext.Guilds.Include(x=>x.Tags).First(x=>x.Id == ctx.Guild.Id);
        if (guild.Tags is null) return ReadOnlyDictionary<string, object>.Empty;
        
        var tags = guild.Tags.Where(x=>x.GuildId == ctx.Guild.Id)
            .OrderBy(tag => generalUtils.CalculateLevenshteinDistance(ctx.UserInput.ToString()??"", tag.Name))
            .ToImmutableList();
        return tags.ToDictionary<Tag, string, object>(tag => $"{tag.Name} ({(tag.Content.Length > 50 ? tag.Content[..50] + "..." : tag.Content)})", tag => tag.Id.ToString());
    }
    
}