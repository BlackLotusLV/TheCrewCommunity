using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.Entities;
using TheCrewCommunity.Data.Entities.Discord;
using TheCrewCommunity.Data.Entities.WebData;
using TheCrewCommunity.Data.Entities.WebData.ThisOrThat;
using TheCrewCommunity.Data.Entities.GameData.Motorfest;

namespace TheCrewCommunity.Services;

public interface IDatabaseMethodService
{
    Task<List<VehicleSuggestion>> GetVehicleSuggestionsAsync();
    Task LinkSuggestionToVehicleAsync(Guid suggestionId, Guid vehicleId);
    Task UnlinkSuggestionFromVehicleAsync(Guid vehicleId);
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
    Task<int> GetImageLikesCountAsync(Guid imageId);
    Task DeleteImageAsync(Guid imageId);
    Task AddVehicleSuggestionAsync(Guid imageId, string brand, string model, string year, string? description = null);
    Task<ApplicationUser> AddApplicationUserAsync(ApplicationUser applicationUser);
    Task<SuggestionVote> AddSuggestionVoteAsync(SuggestionVote vote);
    Task AddMotorfestVehicleAsync(MotorfestVehicle vehicle);
    Task<List<MotorfestVehicleBrand>> GetMotorfestVehicleBrandsAsync();
    Task<List<MotorfestVehicleCategory>> GetMotorfestVehicleCategoriesAsync();
    Task<List<MotorfestVehicleCountry>> GetMotorfestVehicleCountriesAsync();
    Task<List<MotorfestVehicleEngineType>> GetMotorfestVehicleEngineTypesAsync();
    Task<List<MotorfestVehiclePeriod>> GetMotorfestVehiclePeriodsAsync();
    Task<List<MotorfestVehicleStyle>> GetMotorfestVehicleStylesAsync();
    Task<List<MotorfestVehicleTag>> GetMotorfestVehicleTagsAsync();
    Task<List<MotorfestVehicleType>> GetMotorfestVehicleTypesAsync();
    Task<List<MotorfestVehicle>> GetMotorfestVehiclesAsync();
    Task AddMotorfestVehicleBrandAsync(MotorfestVehicleBrand brand);
    Task AddMotorfestVehicleCategoryAsync(MotorfestVehicleCategory category);
    Task AddMotorfestVehicleCountryAsync(MotorfestVehicleCountry country);
    Task AddMotorfestVehicleEngineTypeAsync(MotorfestVehicleEngineType engineType);
    Task AddMotorfestVehiclePeriodAsync(MotorfestVehiclePeriod period);
    Task AddMotorfestVehicleStyleAsync(MotorfestVehicleStyle style);
    Task AddMotorfestVehicleTagAsync(MotorfestVehicleTag tag);
    Task AddMotorfestVehicleTypeAsync(MotorfestVehicleType type);
}

public class DatabaseMethodService(IDbContextFactory<LiveBotDbContext> dbContextFactory, ILogger<IDatabaseMethodService> logger) : IDatabaseMethodService
{
    public async Task<List<VehicleSuggestion>> GetVehicleSuggestionsAsync()
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        return await context.VehicleSuggestions
            .Include(vs => vs.Implementations)
            .OrderBy(vs => vs.Brand)
            .ThenBy(vs => vs.Model)
            .ToListAsync();
    }
    public async Task LinkSuggestionToVehicleAsync(Guid suggestionId, Guid vehicleId)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        MotorfestVehicle? vehicle = await context.MotorfestVehicles.FindAsync(vehicleId);
        if (vehicle != null)
        {
            vehicle.SuggestionId = suggestionId;
            await context.SaveChangesAsync();
        }
    }
    public async Task UnlinkSuggestionFromVehicleAsync(Guid vehicleId)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        MotorfestVehicle? vehicle = await context.MotorfestVehicles.FindAsync(vehicleId);
        if (vehicle != null)
        {
            vehicle.SuggestionId = null;
            await context.SaveChangesAsync();
        }
    }
    public async Task<SuggestionVote> AddSuggestionVoteAsync(SuggestionVote vote)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        SuggestionVote? existingVote = await context.SuggestionVotes
            .Include(x=>x.VotedForVehicle)
            .FirstOrDefaultAsync(x=>
            x.UserId == vote.UserId &&
            (
                (x.VehicleSuggestion1Id == vote.VehicleSuggestion1Id && x.VehicleSuggestion2Id == vote.VehicleSuggestion2Id) ||
                (x.VehicleSuggestion1Id == vote.VehicleSuggestion2Id && x.VehicleSuggestion2Id == vote.VehicleSuggestion1Id)
            )
        );
        Console.WriteLine(existingVote==null);
        if (existingVote is not null) return existingVote;
        context.SuggestionVotes.Add(vote);
        await context.SaveChangesAsync();
        return vote;
    }
    public async Task<Guild> AddGuildAsync(Guild guild)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        Guild guildEntity= (await context.Guilds.AddAsync(guild)).Entity;
        await context.SaveChangesAsync();
        return guildEntity;
    }

    public async Task<ApplicationUser> AddApplicationUserAsync(ApplicationUser applicationUser)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        if (await context.Users.FindAsync(applicationUser.DiscordId)==null)
        {
            await AddUserAsync(new User(applicationUser.DiscordId));
        }
        ApplicationUser? appUser = await context.ApplicationUsers.FirstOrDefaultAsync(x=>x.DiscordId==applicationUser.DiscordId);
        if (appUser != null) return appUser;
        appUser = (await context.ApplicationUsers.AddAsync(applicationUser)).Entity;
        await context.SaveChangesAsync();
        return appUser;
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
    public async Task<int> GetImageLikesCountAsync(Guid imageId)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        UserImage? image = await context.UserImages
            .Include(x => x.ImageLikes)
            .FirstOrDefaultAsync(x => x.Id == imageId);
        
        if (image is null)
        {
            throw new Exception($"Image not found with Id: {imageId}");
        }
    
        return image.ImageLikes!.Count;
    }

    public async Task DeleteImageAsync(Guid imageId)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        UserImage? image = await context.UserImages.FindAsync(imageId);
        if (image is null)
        {
            throw new Exception($"Image not found with Id: {imageId}");
        }
        context.UserImages.Remove(image);
        await context.SaveChangesAsync();
    }

    public async Task AddVehicleSuggestionAsync(Guid imageId, string brand, string model, string year, string? description = null)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        VehicleSuggestion suggestion = new()
        {
            ImageId = imageId,
            Brand = brand,
            Model = model,
            Year = year,
            Description = description
        };
        await context.VehicleSuggestions.AddAsync(suggestion);
        await context.SaveChangesAsync();
    }

    public async Task AddMotorfestVehicleAsync(MotorfestVehicle vehicle)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        await context.MotorfestVehicles.AddAsync(vehicle);
        await context.SaveChangesAsync();
    }

    public async Task<List<MotorfestVehicleBrand>> GetMotorfestVehicleBrandsAsync()
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        return await context.MotorfestVehicleBrands.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<MotorfestVehicleCategory>> GetMotorfestVehicleCategoriesAsync()
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        return await context.MotorfestVehicleCategories.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<MotorfestVehicleCountry>> GetMotorfestVehicleCountriesAsync()
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        return await context.MotorfestVehicleCountries.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<MotorfestVehicleEngineType>> GetMotorfestVehicleEngineTypesAsync()
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        return await context.MotorfestVehicleEngineTypes.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<MotorfestVehiclePeriod>> GetMotorfestVehiclePeriodsAsync()
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        return await context.MotorfestVehiclePeriods.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<MotorfestVehicleStyle>> GetMotorfestVehicleStylesAsync()
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        return await context.MotorfestVehicleStyles.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<MotorfestVehicleTag>> GetMotorfestVehicleTagsAsync()
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        return await context.MotorfestVehicleTags.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<MotorfestVehicleType>> GetMotorfestVehicleTypesAsync()
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        return await context.MotorfestVehicleTypes.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<MotorfestVehicle>> GetMotorfestVehiclesAsync()
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        return await context.MotorfestVehicles
            .Include(x=>x.Brand)
            .Include(x=>x.Category)
            .Include(x=>x.Country)
            .Include(x=>x.EngineType)
            .Include(x=>x.Period)
            .Include(x=>x.Style)
            .Include(x=>x.Tag)
            .Include(x=>x.Type)
            .OrderBy(x => x.Brand.Name)
            .ThenBy(x => x.ModelName)
            .ToListAsync();
    }

    public async Task AddMotorfestVehicleBrandAsync(MotorfestVehicleBrand brand)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        await context.MotorfestVehicleBrands.AddAsync(brand);
        await context.SaveChangesAsync();
    }
    public async Task AddMotorfestVehicleCategoryAsync(MotorfestVehicleCategory category)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        await context.MotorfestVehicleCategories.AddAsync(category);
        await context.SaveChangesAsync();
    }
    public async Task AddMotorfestVehicleCountryAsync(MotorfestVehicleCountry country)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        await context.MotorfestVehicleCountries.AddAsync(country);
        await context.SaveChangesAsync();
    }
    public async Task AddMotorfestVehicleEngineTypeAsync(MotorfestVehicleEngineType engineType)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        await context.MotorfestVehicleEngineTypes.AddAsync(engineType);
        await context.SaveChangesAsync();
    }
    public async Task AddMotorfestVehiclePeriodAsync(MotorfestVehiclePeriod period)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        await context.MotorfestVehiclePeriods.AddAsync(period);
        await context.SaveChangesAsync();
    }
    public async Task AddMotorfestVehicleStyleAsync(MotorfestVehicleStyle style)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        await context.MotorfestVehicleStyles.AddAsync(style);
        await context.SaveChangesAsync();
    }
    public async Task AddMotorfestVehicleTagAsync(MotorfestVehicleTag tag)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        await context.MotorfestVehicleTags.AddAsync(tag);
        await context.SaveChangesAsync();
    }
    public async Task AddMotorfestVehicleTypeAsync(MotorfestVehicleType type)
    {
        await using LiveBotDbContext context = await dbContextFactory.CreateDbContextAsync();
        await context.MotorfestVehicleTypes.AddAsync(type);
        await context.SaveChangesAsync();
    }
}