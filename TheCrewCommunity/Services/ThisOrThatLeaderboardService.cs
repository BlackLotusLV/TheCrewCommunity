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

        _leaderboard = dbContext.VehicleSuggestions
            .Include(x => x.VotesFor)
            .AsEnumerable()
            .Select(suggestion =>
            {
                using LiveBotDbContext internalDbContext = dbContextFactory.CreateDbContext();
                var entry = new LeaderboardEntry
                {
                    VehicleSuggestion = suggestion,
                    TotalWins = suggestion.VotesFor!.Count(vote => vote.VotedForVehicleId == suggestion.Id),
                    TotalMatches = internalDbContext.SuggestionVotes.Count(vote => vote.VehicleSuggestion1Id == suggestion.Id || vote.VehicleSuggestion2Id == suggestion.Id)
                };
                entry.WinRatio = entry.TotalMatches > 0 ? (double)entry.TotalWins / entry.TotalMatches : 0;
                return entry;
            })
            .OrderByDescending(entry => entry.WinRatio)
            .ThenByDescending(entry => entry.TotalWins)
            .ToList();

        for (var i = 0; i < _leaderboard.Count; i++)
        {
            _leaderboard[i].Rank = i + 1;
        }
        
        _nextRefresh = DateTime.UtcNow.Add(_updateInterval);
    }

    public async Task UpdateVoterListAsync()
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        int totalSuggestions = await dbContext.VehicleSuggestions.CountAsync();
        int totalMatchups = totalSuggestions * (totalSuggestions + 1) / 2;
        _voters = dbContext.ApplicationUsers
            .Include(x=>x.SuggestionVotes)
            .ThenInclude(x=>x.VotedForVehicle)
            .Select(appUser => new VoterEntry
            {
                TotalMatches = appUser.SuggestionVotes.Count,
                Percent = (float)appUser.SuggestionVotes.Count / totalMatchups,
                Username = appUser.GlobalUsername ?? string.Empty
            })
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
    }

    public class VoterEntry
    {
        public int Rank { get; set; }
        public required string Username { get; init; }
        public required float Percent { get; init; }
        public required int TotalMatches { get; init; }
    }
}