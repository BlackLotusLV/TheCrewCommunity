using System.Collections.Immutable;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.ModMailCommands;

public sealed class ActiveModMailOption(IDbContextFactory<LiveBotDbContext> dbContextFactory, GeneralUtils generalUtils) : IAutoCompleteProvider
{
    public async ValueTask<Dictionary<string, object>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        if (ctx.Guild is null) return [];
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