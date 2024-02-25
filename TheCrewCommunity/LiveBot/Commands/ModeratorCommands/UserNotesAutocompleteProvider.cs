using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Attributes;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public class UserNotesAutocompleteProvider(IDbContextFactory<LiveBotDbContext> dbContextFactory) : IAutoCompleteProvider
{
    public async ValueTask<Dictionary<string,object>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        var user = (ulong)ctx.Options.First(x => x.Name == "user").Value;
        var infractions = await dbContext.Infractions
            .Where(x => x.InfractionType == InfractionType.Note && x.AdminDiscordId == ctx.User.Id && x.UserId == user).ToListAsync();
        Dictionary<string, object> result = [];
        foreach (Infraction infraction in infractions)
        {
            result.Add($"#{infraction.Id} - {infraction.Reason}", infraction.Id);
        }
        return result;
    }
}