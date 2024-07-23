using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.WebData;

namespace TheCrewCommunity.Services;

public interface IDatabaseMethodService
{
    Task<Guild> AddGuildAsync(Guild guild);
    Task<User> AddUserAsync(User user);
    Task AddUbiInfoAsync(UbiInfo ubiInfo);
    Task<GuildUser> AddGuildUsersAsync(GuildUser guildUser);
    Task<UserActivity> AddUserActivityAsync(UserActivity userActivity);
    Task AddModMailAsync(ModMail modMail);
    Task AddInfractionsAsync(Infraction infraction);
    Task AddRankRolesAsync(RankRoles rankRoles);
    Task AddSpamIgnoreChannelsAsync(SpamIgnoreChannels spamIgnoreChannels);
    Task AddStreamNotificationsAsync(StreamNotifications streamNotifications);
    Task AddButtonRolesAsync(ButtonRoles buttonRoles);
    Task AddWhiteListSettingsAsync(WhiteListSettings whiteListSettings);
    Task AddRoleTagSettings(RoleTagSettings roleTagSettings);
    Task AddPhotoCompEntryAsync(PhotoCompEntries entry);
    Task AddUserImageAsync(ApplicationUser user, Guid imageId, string title, Guid gameId);
    Task ToggleImageLikeAsync(ApplicationUser user, UserImage image);
}

public class DatabaseMethodService(IDbContextFactory<LiveBotDbContext> dbContextFactory, Logger<IDatabaseMethodService> logger) : IDatabaseMethodService
{
    public async Task<Guild> AddGuildAsync(Guild guild)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        Guild guildEntity= (await context.Guilds.AddAsync(guild)).Entity;
        await context.SaveChangesAsync();
        return guildEntity;
    }

    public async Task<User> AddUserAsync(User user)
    {
        if (user.ParentDiscordId!=null)
        {
            await AddUserAsync(new User(user.ParentDiscordId.Value));
        }
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        User userEntity = (await context.Users.AddAsync(user)).Entity;
        await context.SaveChangesAsync();
        return userEntity;
    }

    public async Task AddUbiInfoAsync(UbiInfo ubiInfo)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.Users.FindAsync(ubiInfo.UserDiscordId)==null)
        {
            await AddUserAsync(new User(ubiInfo.UserDiscordId));
        }
        await context.UbiInfo.AddAsync(ubiInfo);
        await context.SaveChangesAsync();
    }

    public async Task<GuildUser> AddGuildUsersAsync(GuildUser guildUser)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.Users.FindAsync(guildUser.UserDiscordId)==null)
        {
            await AddUserAsync(new User(guildUser.UserDiscordId));
        }

        if (await context.Guilds.FindAsync(guildUser.GuildId)==null)
        {
            await AddGuildAsync(new Guild(guildUser.GuildId));
        }

        GuildUser guildUserEntry = (await context.GuildUsers.AddAsync(guildUser)).Entity;
        await context.SaveChangesAsync();
        return guildUserEntry;
    }

    public async Task<UserActivity> AddUserActivityAsync(UserActivity userActivity)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.GuildUsers.FindAsync(userActivity.UserDiscordId, userActivity.GuildId)==null)
        {
            await AddGuildUsersAsync(new GuildUser(userActivity.UserDiscordId, userActivity.GuildId));
        }

        UserActivity entity = (await context.UserActivity.AddAsync(userActivity)).Entity;
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task AddModMailAsync(ModMail modMail)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.GuildUsers.FindAsync(modMail.UserDiscordId, modMail.GuildId)==null)
        {
            await AddGuildUsersAsync(new GuildUser(modMail.UserDiscordId, modMail.GuildId));
        }

        await context.ModMail.AddAsync(modMail);
        await context.SaveChangesAsync();
    }

    public async Task AddInfractionsAsync(Infraction infraction)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.GuildUsers.FindAsync(infraction.UserId, infraction.GuildId)==null)
        {
            await AddGuildUsersAsync(new GuildUser(infraction.UserId, infraction.GuildId));
        }

        await context.Infractions.AddAsync(infraction);
        await context.SaveChangesAsync();
    }

    public async Task AddRankRolesAsync(RankRoles rankRoles)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.Guilds.FindAsync(rankRoles.GuildId)==null)
        {
            await AddGuildAsync(new Guild(rankRoles.GuildId));
        }

        await context.RankRoles.AddAsync(rankRoles);
        await context.SaveChangesAsync();
    }

    public async Task AddSpamIgnoreChannelsAsync(SpamIgnoreChannels spamIgnoreChannels)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.Guilds.FindAsync(spamIgnoreChannels.GuildId)==null)
        {
            await AddGuildAsync(new Guild(spamIgnoreChannels.GuildId));
        }
        await context.SpamIgnoreChannels.AddAsync(spamIgnoreChannels);
        await context.SaveChangesAsync();
    }

    public async Task AddStreamNotificationsAsync(StreamNotifications streamNotifications)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.Guilds.FindAsync(streamNotifications.GuildId)==null)
        {
            await AddGuildAsync(new Guild(streamNotifications.GuildId));
        }
        
        await context.StreamNotifications.AddAsync(streamNotifications);
        await context.SaveChangesAsync();
    }

    public async Task AddButtonRolesAsync(ButtonRoles buttonRoles)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.Guilds.FindAsync(buttonRoles.GuildId)==null)
        {
            await AddGuildAsync(new Guild(buttonRoles.GuildId));
        }
        
        await context.ButtonRoles.AddAsync(buttonRoles);
        await context.SaveChangesAsync();
    }

    public async Task AddWhiteListSettingsAsync(WhiteListSettings whiteListSettings)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.Guilds.FindAsync(whiteListSettings.GuildId)==null)
        {
            await AddGuildAsync(new Guild(whiteListSettings.GuildId));
        }
        
        await context.WhiteListSettings.AddAsync(whiteListSettings);
        await context.SaveChangesAsync();
    }

    public async Task AddRoleTagSettings(RoleTagSettings roleTagSettings)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.Guilds.FindAsync(roleTagSettings.GuildId)==null)
        {
            await AddGuildAsync(new Guild(roleTagSettings.GuildId));
        }
        
        await context.RoleTagSettings.AddAsync(roleTagSettings);
        await context.SaveChangesAsync();
    }

    public async Task AddPhotoCompEntryAsync(PhotoCompEntries entry)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.Users.FindAsync(entry.UserId)==null)
        {
            await AddUserAsync(new User(entry.UserId));
        }
        await context.PhotoCompEntries.AddAsync(entry);
        await context.SaveChangesAsync();
    }

    public async Task AddUserImageAsync(ApplicationUser user, Guid imageId, string title, Guid gameId)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        UserImage userImage = new()
        {
            DiscordId = user.DiscordId,
            Id = imageId,
            Title = title,
            GameId = gameId
        };
        await context.UserImages.AddAsync(userImage);
        await context.SaveChangesAsync();
    }

    public async Task ToggleImageLikeAsync(ApplicationUser user, UserImage image)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        ImageLike? like = await context.ImageLikes.FirstOrDefaultAsync(x => x.DiscordId == user.DiscordId && x.ImageId == image.Id);
        if (like is null)
        {
            ImageLike newEntry = new()
            {
                DiscordId = user.DiscordId,
                ImageId = image.Id
            };
            await context.ImageLikes.AddAsync(newEntry);
        }
        else
        {
            context.Remove(like);
        }

        await context.SaveChangesAsync();
        await UpdateImageLikeCountAsync(image.Id);
    }

    private async Task UpdateImageLikeCountAsync(Guid imageId)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        UserImage? image = await context.UserImages
            .Include(x => x.ImageLikes)
            .FirstOrDefaultAsync(x => x.Id == imageId);
        if (image is null)
        {
            logger.LogInformation(CustomLogEvents.DatabaseMethods, "Tried to update likes of an image but failed. Provided Id: {Id}",imageId);
            return;
        }
        image.LikesCount = image.ImageLikes!.Count;
        context.Update(image);
        await context.SaveChangesAsync();
    }
}