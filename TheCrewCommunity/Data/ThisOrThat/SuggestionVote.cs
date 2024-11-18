using TheCrewCommunity.Data.WebData;

namespace TheCrewCommunity.Data.ThisOrThat;

public class SuggestionVote
{
    public required Guid Id { get; init; } = Guid.CreateVersion7();
    public required Guid UserId { get; init; }
    public required Guid VehicleSuggestion1Id { get; init; }
    public required Guid VehicleSuggestion2Id { get; init; }
    public required Guid VotedForVehicleId { get; init; }
    
    public VehicleSuggestion? VotedForVehicle { get; init; }
    public VehicleSuggestion? VehicleSuggestion1 { get; init; }
    public VehicleSuggestion? VehicleSuggestion2 { get; init; }
    public ApplicationUser? User { get; init; }
}