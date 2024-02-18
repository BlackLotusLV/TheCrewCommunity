using System.Collections.Immutable;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity;

public class GeneralUtils
{
    public bool CheckIfMemberAdmin(DiscordMember member)
    {
        return member.Permissions.HasPermission(Permissions.ManageMessages) ||
               member.Permissions.HasPermission(Permissions.KickMembers) ||
               member.Permissions.HasPermission(Permissions.BanMembers) ||
               member.Permissions.HasPermission(Permissions.Administrator);
    }
    
    public sealed class PhotoContestOption : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var databaseContext = ctx.Services.GetService<LiveBotDbContext>();
            Guild guildSettings = await databaseContext.Guilds
                .Include(x => x.PhotoCompSettings)
                .ThenInclude(x => x.Entries)
                .FirstOrDefaultAsync(x => x.Id == ctx.Guild.Id);
            var customParameters = guildSettings.PhotoCompSettings
                .Where(x => x.IsOpen && x.Entries.Any(entry => entry.UserId == ctx.User.Id))
                .Select(x => x.CustomParameter).ToImmutableArray();
            var openCompetitions = guildSettings.PhotoCompSettings
                .Where(x => x.IsOpen && !customParameters.Any(customParameter => customParameter == x.CustomParameter))
                .ToImmutableArray();

            if (openCompetitions.Length == 0)
            {
                return new DiscordAutoCompleteChoice[]
                    { new("No open competitions", -1) };
            }

            return openCompetitions.Select(photoCompSettings => new DiscordAutoCompleteChoice(photoCompSettings.CustomName, photoCompSettings.Id)).ToList();
        }
    }
}