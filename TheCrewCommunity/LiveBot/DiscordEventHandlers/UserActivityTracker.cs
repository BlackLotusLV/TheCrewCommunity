using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers;

public static class UserActivityTracker
{
    private static List<Cooldown> CoolDowns { get; set; } = [];
    
    public static async Task OnMessageSend(DiscordClient client, MessageCreatedEventArgs e)
    {
        if (e.Guild is null || e.Author.IsBot) return;

        Cooldown? coolDown = CoolDowns.FirstOrDefault(w => w.User == e.Author && w.Guild == e.Guild);
        if (coolDown is not null && coolDown.Time.ToUniversalTime().AddMinutes(2) >= DateTime.UtcNow) return;

        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        UserActivity userActivity =
            liveBotDbContext.UserActivity.FirstOrDefault(activity => activity.UserDiscordId == e.Author.Id && activity.GuildId == e.Guild.Id && activity.Date == DateTime.UtcNow.Date) ??
            await databaseMethodService.AddUserActivityAsync(new UserActivity(e.Author.Id, e.Guild.Id, 0, DateTime.UtcNow.Date));

        await liveBotDbContext.SaveChangesAsync();
        userActivity.Points += new Random().Next(25, 50);
        liveBotDbContext.UserActivity.Update(userActivity);
        await liveBotDbContext.SaveChangesAsync();
        
        if (coolDown is not null)
        {
            CoolDowns.Remove(coolDown);
        }
        CoolDowns.Add(new Cooldown(e.Author, e.Guild, DateTime.UtcNow));

        long userPoints = liveBotDbContext.UserActivity
            .Where(w => w.Date > DateTime.UtcNow.AddDays(-30) && w.GuildId == e.Guild.Id && w.UserDiscordId == e.Author.Id)
            .Sum(w => w.Points);
        var rankRole = liveBotDbContext.RankRoles.Where(w => w.GuildId == e.Guild.Id).ToList();
        var rankRoleUnder = liveBotDbContext.RankRoles.Where(w => w.GuildId == e.Guild.Id && w.ServerRank <= userPoints).OrderByDescending(w => w.ServerRank).ToList();
        var rankRolesOver = rankRole.Except(rankRoleUnder);

        DiscordMember member = await e.Guild.GetMemberAsync(e.Author.Id);

        if (rankRoleUnder.Count == 0) return;
        if (member.Roles.Any(memberRole => memberRole.Id != rankRoleUnder.First().RoleId))
        {
            await member.GrantRoleAsync(e.Guild.Roles.Values.First(role => role.Id == rankRoleUnder.First().RoleId));
        }

        var matchingRoleList = member.Roles.Where(memberRole => rankRoleUnder.Skip(1).Any(under => memberRole.Id == under.RoleId) || rankRolesOver.Any(over => memberRole.Id == over.RoleId));
        foreach (DiscordRole discordRole in matchingRoleList)
        {
            await member.RevokeRoleAsync(discordRole);
        }

    }
    private sealed class Cooldown(DiscordUser user, DiscordGuild guild, DateTime time)
    {
        public DiscordUser User { get; set; } = user;
        public DiscordGuild Guild { get; set; } = guild;
        public DateTime Time { get; set; } = time;
    }
}