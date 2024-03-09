using System.Collections.Immutable;
using System.Collections.ObjectModel;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.ModMailCommands;

public sealed class ActiveModMailOption(IDbContextFactory<LiveBotDbContext> dbContextFactory) : IAutoCompleteProvider
{
    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        if (ctx.Guild is null) return ReadOnlyDictionary<string, object>.Empty;
        var activeModMails = liveBotDbContext.ModMail.Where(x => x.IsActive);
        Dictionary<string,object> result = [];
        foreach (ModMail modMail in activeModMails)
        {
            DiscordMember member = await ctx.Guild.GetMemberAsync(modMail.UserDiscordId);
            result.Add($"#{modMail.Id} - {member.Username}", modMail.Id.ToString());
        }

        return result;
    }
}