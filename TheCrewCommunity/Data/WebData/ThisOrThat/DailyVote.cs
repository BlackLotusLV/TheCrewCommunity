namespace TheCrewCommunity.Data.WebData.ThisOrThat;

public class DailyVote
{
    public required Guid Id { get; init; } = Guid.CreateVersion7();
    public required Guid VehicleSuggestion1Id { get; init; }
    public required Guid VehicleSuggestion2Id { get; init; }
    public required DateOnly Date { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public required bool IsPostedOnDiscord { get; set; } = false;
    
    public VehicleSuggestion? VehicleSuggestion1 { get; init; }
    public VehicleSuggestion? VehicleSuggestion2 { get; init; }
}