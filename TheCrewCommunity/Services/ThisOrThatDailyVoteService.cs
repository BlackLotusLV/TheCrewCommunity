using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.WebData.ThisOrThat;

namespace TheCrewCommunity.Services;

public interface IThisOrThatDailyVoteService
{
    DailyVote? GetDailyVote();
}

public class ThisOrThatDailyVoteService(IDbContextFactory<LiveBotDbContext> dbContextFactory, ILogger<ThisOrThatDailyVoteService> logger) : IThisOrThatDailyVoteService, IHostedService, IDisposable
{
    private DailyVote? _dailyVote = null;
    private Timer? _timer = null;
    
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _dailyVote = await GetOrCreateDailyVoteAsync();
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
            logger.LogInformation("Updating daily vote at 5 minutes past midnight");
            _dailyVote = await GetOrCreateDailyVoteAsync();
            logger.LogInformation("Daily vote updated successfully");
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
            .Where(dv => dv.Date > today.AddDays(-10) && dv.Date < today)
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
            .GroupBy(x => new { x.VehicleSuggestion1Id, x.VehicleSuggestion2Id })
            .Select(g => new
            {
                Pair = g.Key,
                Count = g.Count()
            })
            .ToListAsync();
        // Some AI magic that will probably be broken but was too lazy to figure it out myself. Get the vote with least votes
        var voteCounts = pairVotes.ToDictionary(
            pv=> [pv.Pair.VehicleSuggestion1Id, pv.Pair.VehicleSuggestion2Id],
            pv => pv.Count,
            HashSet<Guid>.CreateSetComparer());

        (VehicleSuggestion, VehicleSuggestion) selectedPair = freePairs
            .OrderBy(pair =>
            {
                var pairIds = new HashSet<Guid> { pair.Item1.Id, pair.Item2.Id };
                return voteCounts.TryGetValue(pairIds, out int count) ? count : 0;
            })
            .First();
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
}