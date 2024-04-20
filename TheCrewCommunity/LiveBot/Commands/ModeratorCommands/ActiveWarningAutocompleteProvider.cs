using System.Collections.ObjectModel;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public class ActiveWarningAutocompleteProvider(IDbContextFactory<LiveBotDbContext> dbContextFactory) : IAutoCompleteProvider
{
    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        await using LiveBotDbContext databaseContext = await dbContextFactory.CreateDbContextAsync();
        Dictionary<string, object> result = [];
        var userId = (ulong)ctx.Options.First(x => x.Type == DiscordApplicationCommandOptionType.User).Value;
        //var userId = (ulong)ctx.Options.First(x => x.Name == "user").Value;
        if (ctx.Guild is null) return ReadOnlyDictionary<string, object>.Empty;
        foreach (Infraction item in databaseContext.Infractions.Where(w => w.GuildId == ctx.Guild.Id && w.UserId == userId && w.InfractionType == InfractionType.Warning && w.IsActive))
        {
            result.Add($"#{item.Id} - {item.Reason}", item.Id.ToString());
        }

        return result;
    }
}