using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.WebData.ThisOrThat;

namespace TheCrewCommunity.Services;

public interface IThisOrThatLeaderboardService
{
    List<ThisOrThatLeaderboardService.LeaderboardEntry> GetLeaderboard();
    List<ThisOrThatLeaderboardService.VoterEntry> GetVoterList();
    DateTime GetNextRefreshTime();
    Task UpdateLeaderboardAsync();
    Task UpdateVoterListAsync();
}

public class ThisOrThatLeaderboardService(IDbContextFactory<LiveBotDbContext> dbContextFactory, ILogger<ThisOrThatLeaderboardService> logger) : IHostedService, IThisOrThatLeaderboardService, IDisposable
{
    private List<LeaderboardEntry> _leaderboard = [];
    private List<VoterEntry> _voters = [];
    private Timer? _timer;
    private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(5);
    private DateTime _nextRefresh;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("ThisOrThatLeaderboardService is starting.");
        await UpdateLeaderboardAsync();
        await UpdateVoterListAsync();
        _timer = new Timer(RefreshLeaderboardCallback, null, _updateInterval, _updateInterval);
        _nextRefresh = DateTime.UtcNow.Add(_updateInterval);
    }
    
    private void RefreshLeaderboardCallback(object? state)
    {
        try
        {
            // Start a new task but don't wait for it to complete
            Task.Run(RefreshLeaderboardAsync).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    logger.LogError(task.Exception, "Error occurred during leaderboard refresh");
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in RefreshLeaderboardCallback");
        }
    }

    private async Task RefreshLeaderboardAsync()
    {
        try
        {
            logger.LogInformation("Refreshing leaderboards");
            await UpdateLeaderboardAsync();
            await UpdateVoterListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing leaderboards");
        }
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("ThisOrThatLeaderboardService is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        
        await Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
    }
    
    public DateTime GetNextRefreshTime()
    {
        return _nextRefresh;
    }

    public List<LeaderboardEntry> GetLeaderboard()
    {
        return _leaderboard;
    }
    public List<VoterEntry> GetVoterList()
    {
        return _voters;
    }

    public async Task UpdateLeaderboardAsync()
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

        List<VehicleSuggestion> suggestions = await dbContext.VehicleSuggestions.ToListAsync();
        List<SuggestionVote> allVotes = await dbContext.SuggestionVotes.ToListAsync();

        Dictionary<Guid, int> winsMap = suggestions.ToDictionary(s => s.Id, _ => 0);
        Dictionary<Guid, int> matchesMap = suggestions.ToDictionary(s => s.Id, _ => 0);
        foreach (SuggestionVote vote in allVotes)
        {
            if (winsMap.ContainsKey(vote.VotedForVehicleId))
            {
                winsMap[vote.VotedForVehicleId]++;
            }

            if (matchesMap.ContainsKey(vote.VehicleSuggestion1Id))
            {
                matchesMap[vote.VehicleSuggestion1Id]++;
            }

            if (matchesMap.ContainsKey(vote.VehicleSuggestion2Id))
            {
                matchesMap[vote.VehicleSuggestion2Id]++;
            }
        }

        var leaderboardEntries = suggestions.Select(suggestion =>
        {
            var entry = new LeaderboardEntry
            {
                VehicleSuggestion = suggestion,
                TotalWins = winsMap[suggestion.Id],
                TotalMatches = matchesMap[suggestion.Id]
            };
            entry.WinRatio = entry.TotalMatches > 0 ? (double)entry.TotalWins / entry.TotalMatches : 0;
            return entry;
        }).ToList();

        // V2 Ranking logic
        var matchups = allVotes.GroupBy(v =>
        {
            var id1 = v.VehicleSuggestion1Id;
            var id2 = v.VehicleSuggestion2Id;
            return id1.CompareTo(id2) < 0 ? (id1, id2) : (id2, id1);
        }).ToList();

        Dictionary<Guid, double> v2PointsMap = suggestions.ToDictionary(s => s.Id, _ => 0.0);

        foreach (var matchup in matchups)
        {
            Guid vehicle1Id = matchup.Key.Item1;
            Guid vehicle2Id = matchup.Key.Item2;

            int vehicle1Votes = 0;
            int vehicle2Votes = 0;
            foreach (SuggestionVote vote in matchup)
            {
                if (vote.VotedForVehicleId == vehicle1Id)
                {
                    vehicle1Votes++;
                }
                else if (vote.VotedForVehicleId == vehicle2Id)
                {
                    vehicle2Votes++;
                }
            }

            int totalVotes = vehicle1Votes + vehicle2Votes;

            if (totalVotes == 0) continue;

            if (vehicle1Votes > vehicle2Votes)
            {
                double ratio = (double)vehicle1Votes / totalVotes;
                int points = ratio switch
                {
                    1.0 => 10,
                    >= 0.75 => 5,
                    _ => 1
                };
                v2PointsMap[vehicle1Id] += points;
            }
            else if (vehicle2Votes > vehicle1Votes)
            {
                double ratio = (double)vehicle2Votes / totalVotes;
                int points = ratio switch
                {
                    1.0 => 10,
                    >= 0.75 => 5,
                    _ => 1
                };
                v2PointsMap[vehicle2Id] += points;
            }
        }

        foreach (LeaderboardEntry entry in leaderboardEntries)
        {
            entry.V2Points = v2PointsMap[entry.VehicleSuggestion.Id];
        }

        _leaderboard = leaderboardEntries
            .OrderByDescending(entry => entry.WinRatio)
            .ThenByDescending(entry => entry.TotalWins)
            .ToList();

        for (var i = 0; i < _leaderboard.Count; i++)
        {
            _leaderboard[i].Rank = i + 1;
        }

        List<LeaderboardEntry> v2Sorted = [.. _leaderboard.OrderByDescending(e => e.V2Points)];
        for (var i = 0; i < v2Sorted.Count; i++)
        {
            v2Sorted[i].V2Rank = i + 1;
        }

        _nextRefresh = DateTime.UtcNow.Add(_updateInterval);
    }

    public async Task UpdateVoterListAsync()
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        int totalSuggestions = await dbContext.VehicleSuggestions.CountAsync();
        int totalMatchups = totalSuggestions * (totalSuggestions - 1) / 2;
        _voters = dbContext.ApplicationUsers
            .Include(x=>x.SuggestionVotes)
            .ThenInclude(x=>x.VotedForVehicle)
            .Select(appUser => new VoterEntry
            {
                TotalMatches = appUser.SuggestionVotes.Count,
                Percent = (float)appUser.SuggestionVotes.Count / totalMatchups,
                Username = appUser.UserName ?? string.Empty
            })
            .OrderByDescending(x=>x.Percent)
            .ToList();
        for (var i = 0; i < _voters.Count; i++)
        {
            _voters[i].Rank = i + 1;
        }
    }

    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public required VehicleSuggestion VehicleSuggestion { get; init; }
        public int TotalMatches { get; init; }
        public int TotalWins { get; init; }
        public double WinRatio { get; set; }
        public double V2Points { get; set; }
        public int V2Rank { get; set; }
    }

    public class VoterEntry
    {
        public int Rank { get; set; }
        public required string Username { get; init; }
        public required float Percent { get; init; }
        public required int TotalMatches { get; init; }
    }
}