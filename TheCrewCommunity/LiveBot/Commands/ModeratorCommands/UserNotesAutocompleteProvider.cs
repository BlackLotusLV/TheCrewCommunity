using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public class UserNotesAutocompleteProvider(IDbContextFactory<LiveBotDbContext> dbContextFactory) : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        var user = (ulong?)ctx.Options.First(x => x.Type == DiscordApplicationCommandOptionType.User).Value;
        var infractions = await dbContext.Infractions
            .Where(x => x.InfractionType == InfractionType.Note && x.AdminDiscordId == ctx.User.Id && x.UserId == user).ToListAsync();
        return infractions.Select(infraction => new DiscordAutoCompleteChoice($"#{infraction.Id} - {infraction.Reason}", infraction.Id.ToString())).ToList();
    }
}