using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.Services;

public interface IUserActivityService
{
    Task StartAsync();
    Task UpdateRankRolesListAsync();
    Task UpdateUserActivityAsync(DiscordUser user, DiscordGuild guild);
}
public class UserActivityService(IDbContextFactory<LiveBotDbContext> dbContextFactory, IMemoryCache memoryCache, IDatabaseMethodService dbMethodService, ILogger<UserActivityService> logger) : IUserActivityService
{
    private RankRoles[] _rankRolesArray = [];
    private readonly List<Cooldown> _cooldownList = [];
    private const int PointsMinimum = 25;
    private const int PointsMaximum = 50;
    
    public async Task StartAsync()
    {
        logger.LogInformation(CustomLogEvents.UserActivity, "Starting service");
        await UpdateRankRolesListAsync();
        logger.LogInformation(CustomLogEvents.UserActivity, "Service started");
    }

    public async Task UpdateRankRolesListAsync()
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        _rankRolesArray = await dbContext.RankRoles.ToArrayAsync();
    }

    public async Task UpdateUserActivityAsync(DiscordUser user, DiscordGuild guild)
    {
        Cooldown? coolDown = _cooldownList.FirstOrDefault(w => w.User == user && w.Guild == guild);
        DateTime utcNow = DateTime.UtcNow;
        if (coolDown is not null && coolDown.Time.ToUniversalTime().AddMinutes(2) >= utcNow) return;

        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

        var activityKey = $"UserActivity:{user.Id}-{guild.Id}-{utcNow.Date}";
        var pastActivityKey = $"PastActivity:{user.Id}-{guild.Id}-{utcNow.Date}";
        TimeSpan expirationTime = (DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow).Add(TimeSpan.FromMinutes(5));

        UserActivity? userActivity = await memoryCache.GetOrCreateAsync(activityKey, async e =>
        {
            logger.LogDebug(CustomLogEvents.UserActivity,"Adding User activity to the cache. Item to expire after: {Time}",expirationTime.ToString());
            e.SetAbsoluteExpiration(expirationTime);
            return await GetOrCreateUserActivityAsync(user, guild, utcNow.Date);
        });
        if (userActivity is null)
        {
            logger.LogDebug(CustomLogEvents.UserActivity, "UserActivity of {UserName}({UserId}) is null, should not be, ending early", user.GlobalName, user.Id);
            return;
        }

        userActivity.Points += new Random().Next(PointsMinimum, PointsMaximum);
        dbContext.UserActivity.Update(userActivity);
        await dbContext.SaveChangesAsync();
        logger.LogDebug(CustomLogEvents.UserActivity, "Points added to database for {UserName}", user.Username);


        if (coolDown is not null)
        {
            _cooldownList.Remove(coolDown);
            logger.LogDebug(CustomLogEvents.UserActivity, "Removed cooldown entry for {UserName}", user.Username);
        }

        _cooldownList.Add(new Cooldown(user, guild, DateTime.UtcNow));

        long? pastPoints = await memoryCache.GetOrCreateAsync(pastActivityKey, async e =>
        {
            logger.LogDebug(CustomLogEvents.UserActivity,"Adding past points to the cache. Items to expire after: {Time}",expirationTime.ToString());
            e.SetAbsoluteExpiration(expirationTime);
            return await GetPastUserPointsAsync(user, guild, utcNow.Date);
        });
        logger.LogDebug(CustomLogEvents.UserActivity, "Retrieved past points: {PastPoints}", pastPoints);

        long currentPoints = pastPoints.Value + userActivity.Points;
        var rolesUnder = _rankRolesArray
            .Where(x => x.GuildId == guild.Id && x.ServerRank <= currentPoints)
            .OrderByDescending(x => x.ServerRank)
            .ToArray();
        var rolesOver = _rankRolesArray.Except(rolesUnder);

        DiscordMember member = await guild.GetMemberAsync(user.Id);

        var matchingRoles = member.Roles.Where(x =>
            rolesUnder.Skip(1).Any(y => y.RoleId == x.Id) ||
            rolesOver.Any(y => y.RoleId == x.Id)
        ).ToArray();
        logger.LogDebug(CustomLogEvents.UserActivity, "Matching role count: {RoleCount} for user: {UserName}", matchingRoles.Length, user.Username);
        foreach (DiscordRole role in matchingRoles)
        {
            await member.RevokeRoleAsync(role);
            logger.LogDebug(CustomLogEvents.UserActivity, "Remove {RoleName} role from member {MemberName}", role.Name, member.Username);
        }

        if (rolesUnder.Length is 0 || member.Roles.Any(x => x.Id == rolesUnder.First().RoleId)) return;
        logger.LogDebug(CustomLogEvents.UserActivity, "Granting a rank role to {MemberName}", member.Username);
        await member.GrantRoleAsync(guild.Roles.Values.First(role => role.Id == rolesUnder.First().RoleId));
    }

    private async Task<UserActivity> GetOrCreateUserActivityAsync(DiscordUser user, DiscordGuild guild, DateTime date)
    {
        logger.LogDebug(CustomLogEvents.UserActivity,"Getting user: {UserName}({UserId}) activity of today", user.GlobalName, user.Id);
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.UserActivity.FirstOrDefaultAsync(x => x.UserDiscordId == user.Id && x.GuildId == guild.Id && x.Date == date)
               ?? await dbMethodService.AddUserActivityAsync(new UserActivity(user.Id, guild.Id, 0, date));
    }

    private async Task<long> GetPastUserPointsAsync(DiscordUser user, DiscordGuild guild, DateTime date)
    {
        logger.LogDebug(CustomLogEvents.UserActivity, "Getting user past scores from the database");
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return dbContext.UserActivity
            .Where(w => w.Date > date.AddDays(-30) && w.Date != date && w.GuildId == guild.Id && w.UserDiscordId == user.Id)
            .Sum(w => w.Points);
    }
    private sealed class Cooldown(DiscordUser user, DiscordGuild guild, DateTime time)
    {
        public DiscordUser User { get; init; } = user;
        public DiscordGuild Guild { get; init; } = guild;
        public DateTime Time { get; set; } = time;
    }
}