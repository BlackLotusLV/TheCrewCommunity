using System.Collections.ObjectModel;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.ModMailCommands;

public sealed class ActiveModMailOption(IDbContextFactory<LiveBotDbContext> dbContextFactory) : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        var choices = new List<DiscordAutoCompleteChoice>();
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        if (ctx.Guild is null) return choices;
        var activeModMails = liveBotDbContext.ModMail.Where(x => x.IsActive);
        foreach (ModMail modMail in activeModMails)
        {
            DiscordMember member = await ctx.Guild.GetMemberAsync(modMail.UserDiscordId);
            choices.Add(new DiscordAutoCompleteChoice($"#{modMail.Id} - {member.Username}", modMail.Id.ToString()));
        }

        return choices;
    }
}