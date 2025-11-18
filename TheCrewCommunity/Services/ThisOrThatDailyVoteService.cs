using System.Diagnostics;
using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.WebData;
using TheCrewCommunity.Data.WebData.ThisOrThat;

namespace TheCrewCommunity.Services;

public interface IThisOrThatDailyVoteService
{
    DailyVote? GetDailyVote();
    Task Vote(DiscordClient client, ComponentInteractionCreatedEventArgs args);
}

public class ThisOrThatDailyVoteService(IDbContextFactory<LiveBotDbContext> dbContextFactory, ILogger<ThisOrThatDailyVoteService> logger, DiscordClient discordClient, IDatabaseMethodService databaseMethodService) : IThisOrThatDailyVoteService, IHostedService, IDisposable
{
    private DailyVote? _dailyVote = null;
    private Timer? _timer = null;
    
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _dailyVote = await GetOrCreateDailyVoteAsync();
        logger.LogInformation(CustomLogEvents.DailyTot,"Daily vote service started");
        await PostOrDailyVoteToDiscordAsync();
        SetupTimer();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        await Task.CompletedTask;
    }
    
    public DailyVote? GetDailyVote()
    {
        return _dailyVote;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    private void SetupTimer()
    {
        _timer = new Timer(
            TimerCallback,
            null,
            CalculateNextRunTime(DateTime.UtcNow)-DateTime.UtcNow,
            TimeSpan.FromHours(24)
        );
    }
    private void TimerCallback(object? state)
    {
        try
        {
            Task.Run(UpdateDailyVoteAsync).Wait();

        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in Daily Vote TimerCallback");
        }
        finally
        {
            SetupTimer();
        }
    }
    
    private DateTime CalculateNextRunTime(DateTime now)
    {
        // Set target time to 00:05:00 (5 minutes after midnight)
        var target = new DateTime(now.Year, now.Month, now.Day, 0, 5, 0, DateTimeKind.Utc);
        
        // If it's already past the target time, move to next day
        if (now > target)
        {
            target = target.AddDays(1);
        }
        
        return target;
    }
    private async Task UpdateDailyVoteAsync()
    {
        try
        {
            logger.LogInformation(CustomLogEvents.DailyTot,"Updating daily vote at 5 minutes past midnight");
            _dailyVote = await GetOrCreateDailyVoteAsync();
            logger.LogInformation(CustomLogEvents.DailyTot,"Daily vote updated successfully");
            await PostOrDailyVoteToDiscordAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating daily vote");
        }
    }


    private async Task<DailyVote?> GetOrCreateDailyVoteAsync()
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get todays daily vote from DB, if it exists, return
        DailyVote? dailyVote = await dbContext.DailyVotes
            .Include(x=>x.VehicleSuggestion1)
            .Include(x=>x.VehicleSuggestion2)
            .FirstOrDefaultAsync(x => x.Date == today);
        if (dailyVote is not null) return dailyVote;
        // Generate a list of unique pairs from the DB. If there are less than 2, return
        var uniquePairs = await GenerateUniquePairList();
        if (uniquePairs.Count < 1) return null;
        // Get a list of daily vote pairs from the last 10 days
        var recentPairs = await dbContext.DailyVotes
            .Where(dv => dv.Date > today.AddDays(-1000) && dv.Date < today)
            .Select(dv => new HashSet<Guid>
            {
                dv.VehicleSuggestion1Id,
                dv.VehicleSuggestion2Id
            })
            .ToHashSetAsync(HashSet<Guid>.CreateSetComparer());
        // Get a list of free pairs by removing the recent pairs from the unique pairs, if no free pairs remaining, set unique pairs as free pairs
        var freePairs = uniquePairs
            .Where(x => !recentPairs.Contains([x.Item1.Id, x.Item2.Id]))
            .ToList();
        if (freePairs.Count < 1)
        {
            freePairs = uniquePairs;
        }
        // get all pair group votes and count of them
        var pairVotes = await dbContext.SuggestionVotes
            .Select(x => new
            {
                Id1 = x.VehicleSuggestion1Id.CompareTo(x.VehicleSuggestion2Id) < 0
                    ? x.VehicleSuggestion1Id
                    : x.VehicleSuggestion2Id,
                id2 = x.VehicleSuggestion1Id.CompareTo(x.VehicleSuggestion2Id) < 0
                    ? x.VehicleSuggestion2Id
                    : x.VehicleSuggestion1Id,
            })
            .GroupBy(x => new { x.Id1, x.id2 })
            .Select(g => new
            {
                Pair = g.Key,
                Count = g.Count()
            }).ToListAsync();

        var voteCounts = pairVotes.ToDictionary(
                pv=> [pv.Pair.Id1, pv.Pair.id2], pv=>pv.Count, HashSet<Guid>.CreateSetComparer());
        var minVoteCount = freePairs
            .Min(pair => {
                var pairIds = new HashSet<Guid> { pair.Item1.Id, pair.Item2.Id };
                return voteCounts.TryGetValue(pairIds, out int count) ? count : 0;
            });

        var candidatePairs = freePairs
            .Where(pair => {
                var pairIds = new HashSet<Guid> { pair.Item1.Id, pair.Item2.Id };
                return (voteCounts.TryGetValue(pairIds, out int count) ? count : 0) == minVoteCount;
            })
            .ToList();

        var random = new Random();
        (VehicleSuggestion, VehicleSuggestion) selectedPair = candidatePairs[random.Next(candidatePairs.Count)];

        // create database entry for this nonsense :)
        dbContext.Attach(selectedPair.Item1);
        dbContext.Attach(selectedPair.Item2);
        dailyVote = new DailyVote
        {
            Date = today,
            VehicleSuggestion1Id = selectedPair.Item1.Id,
            VehicleSuggestion2Id = selectedPair.Item2.Id,
            Id = Guid.CreateVersion7(),
            VehicleSuggestion1 = selectedPair.Item1,
            VehicleSuggestion2 = selectedPair.Item2,
            IsPostedOnDiscord = false
        };
        dbContext.DailyVotes.Add(dailyVote);
        await dbContext.SaveChangesAsync();
        return dailyVote;
    }
    private async Task<List<(VehicleSuggestion, VehicleSuggestion)>> GenerateUniquePairList()
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        var vehicleSuggestions = await dbContext.VehicleSuggestions.ToListAsync();
        
        if (vehicleSuggestions.Count < 2)
        {
            return [];
        }
        var uniquePairs = new List<(VehicleSuggestion, VehicleSuggestion)>();
        for (var i = 0; i < vehicleSuggestions.Count; i++)
        {
            for (int j = i + 1; j < vehicleSuggestions.Count; j++)
            {
                uniquePairs.Add((vehicleSuggestions[i], vehicleSuggestions[j]));
            }
        }
        return uniquePairs;
    }
    private bool methodLockout = false;
    private async Task PostOrDailyVoteToDiscordAsync()
    {
        if (methodLockout) return;
        methodLockout = true;
        logger.LogDebug(CustomLogEvents.DailyTot,"Daily vote method lockout enabled, continuing");
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guild[] guilds = await dbContext.Guilds.Where(x=>x.ThisOrThatDailyChannelId != null).ToArrayAsync();
        if (guilds.Length < 1) return;
        logger.LogDebug(CustomLogEvents.DailyTot,"Found guilds with this or that daily channel set");
        DailyVote? dailyVote = GetDailyVote();
        if (dailyVote is null || dailyVote.IsPostedOnDiscord) return;
        logger.LogDebug(CustomLogEvents.DailyTot,"Daily vote database entry found and not posted on discord");
        string vehicle1Name = $"{dailyVote.VehicleSuggestion1.Brand} - {dailyVote.VehicleSuggestion1.Model}({dailyVote.VehicleSuggestion1.Year})";
        string vehicle2Name = $"{dailyVote.VehicleSuggestion2.Brand} - {dailyVote.VehicleSuggestion2.Model}({dailyVote.VehicleSuggestion2.Year})";

        StringBuilder HeadTextBuilder = new();
        HeadTextBuilder.AppendLine("# This or That *[Discord Beta]*");
        HeadTextBuilder.AppendLine($"## {vehicle1Name} VS {vehicle2Name}");
        
        DiscordMessageBuilder messageBuilder = new();
        messageBuilder.EnableV2Components()
            .AddTextDisplayComponent(HeadTextBuilder.ToString())
            .AddMediaGalleryComponent(
                new DiscordMediaGalleryItem($"https://imagedelivery.net/Gym1gfQYlAl-qmVmCPEnkA/{dailyVote.VehicleSuggestion1.ImageId}/public", vehicle1Name),
                new DiscordMediaGalleryItem($"https://imagedelivery.net/Gym1gfQYlAl-qmVmCPEnkA/{dailyVote.VehicleSuggestion2.ImageId}/public", vehicle2Name))
            .AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Primary, $"{dailyVote.Id}-DailyVote-1", vehicle1Name),
                new DiscordLinkButtonComponent("https://thecrew-community.com/ThisOrThat/Leaderboard", "See Results"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, $"{dailyVote.Id}-DailyVote-2", vehicle2Name));
        logger.LogDebug("Daily vote message builder created");

        var dGuilds = discordClient.GetGuildsAsync();
        foreach (Guild guild in guilds)
        {
            logger.LogDebug(CustomLogEvents.DailyTot,"Posting daily vote to server {ServerId}", guild.Id);
            DiscordGuild server = await dGuilds.FirstOrDefaultAsync(x=>x.Id == guild.Id);
            if (server is null) break;
            logger.LogDebug(CustomLogEvents.DailyTot,"Server found");
            DiscordChannel totChannel = await server.GetChannelAsync(guild.ThisOrThatDailyChannelId.Value); 
            if (totChannel is null) break;
            logger.LogDebug(CustomLogEvents.DailyTot,"Channel found");
            await messageBuilder.SendAsync(totChannel);
            logger.LogDebug(CustomLogEvents.DailyTot,"Message sent");
        }
        dailyVote.IsPostedOnDiscord = true;
        dbContext.Update(dailyVote);
        await dbContext.SaveChangesAsync();
        logger.LogDebug(CustomLogEvents.DailyTot,"Daily vote database entry updated to mark as posted on discord");
        
        methodLockout = false;
    }

    public async Task Vote(DiscordClient client, ComponentInteractionCreatedEventArgs args)
    {
        DailyVote? dailyVote = GetDailyVote();
        if (dailyVote is null)
        {
            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("No daily vote available at this time. Try again later.").AsEphemeral());
            return;
        }

        if (!args.Interaction.Data.CustomId.Contains(dailyVote.Id.ToString()))
        {
            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("This vote has expired, head over to the website to get other vote options.").AsEphemeral());
            return;
        }
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        SuggestionVote? usersVote = await dbContext.SuggestionVotes.Include(x=>x.User).FirstOrDefaultAsync(x => x.VehicleSuggestion1Id == dailyVote.VehicleSuggestion1Id && x.VehicleSuggestion2Id==dailyVote.VehicleSuggestion2Id && x.User!.DiscordId == args.User.Id);
        if (usersVote is not null)
        {
            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You have already voted for this").AsEphemeral());
            return;
        }
        Guid votedFor = args.Interaction.Data.CustomId.Contains("-DailyVote-1") ? dailyVote.VehicleSuggestion1Id : dailyVote.VehicleSuggestion2Id;

        ApplicationUser applicationUser = await databaseMethodService.AddApplicationUserAsync(new ApplicationUser()
        {
            DiscordId = args.User.Id,
            AvatarUrl = args.User.AvatarUrl,
            GlobalUsername = args.User.GlobalName,
            UserName = args.User.Username

        });
        usersVote = new SuggestionVote()
        {
            Id = Guid.CreateVersion7(),
            UserId = applicationUser.Id,
            VehicleSuggestion1Id = dailyVote.VehicleSuggestion1Id,
            VehicleSuggestion2Id = dailyVote.VehicleSuggestion2Id,
            VotedForVehicleId = votedFor
        };
        dbContext.SuggestionVotes.Add(usersVote);
        await dbContext.SaveChangesAsync();
        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Vote recorded successfully").AsEphemeral());
    }
}