using DSharpPlus;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Attributes;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public class ActiveWarningAutocompleteProvider(IDbContextFactory<LiveBotDbContext> dbContextFactory) : IAutoCompleteProvider
{
    public async ValueTask<Dictionary<string, object>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        await using LiveBotDbContext databaseContext = await dbContextFactory.CreateDbContextAsync();
        Dictionary<string, object> result = [];
        var userId = (ulong)ctx.Options.First(x => x.Type == ApplicationCommandOptionType.User).Value;
        //var userId = (ulong)ctx.Options.First(x => x.Name == "user").Value;
        if (ctx.Guild is null) return [];
        foreach (Infraction item in databaseContext.Infractions.Where(w => w.GuildId == ctx.Guild.Id && w.UserId == userId && w.InfractionType == InfractionType.Warning && w.IsActive))
        {
            result.Add($"#{item.Id} - {item.Reason}", item.Id.ToString());
        }

        return result;
    }
}