using System.Collections.ObjectModel;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public class ActiveWarningAutocompleteProvider(IDbContextFactory<LiveBotDbContext> dbContextFactory) : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        var choices = new List<DiscordAutoCompleteChoice>();
        await using LiveBotDbContext databaseContext = await dbContextFactory.CreateDbContextAsync();
        var userId = (ulong?)ctx.Options.First(x => x.Type == DiscordApplicationCommandOptionType.User).Value;
        //var userId = (ulong)ctx.Options.First(x => x.Name == "user").Value;
        if (ctx.Guild is null) return choices;
        foreach (Infraction item in databaseContext.Infractions.Where(w => w.GuildId == ctx.Guild.Id && w.UserId == userId && w.InfractionType == InfractionType.Warning && w.IsActive))
        {
            choices.Add(new DiscordAutoCompleteChoice($"#{item.Id} - {item.Reason}", item.Id.ToString()));
        }

        return choices;
    }
}