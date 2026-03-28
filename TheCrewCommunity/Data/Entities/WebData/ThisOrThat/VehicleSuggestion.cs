using System.ComponentModel.DataAnnotations;

using TheCrewCommunity.Data.Entities.GameData.Motorfest;

namespace TheCrewCommunity.Data.Entities.WebData.ThisOrThat;

public class VehicleSuggestion
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required Guid ImageId { get; set; }
    [MaxLength(30)]
    public required string Brand { get; set; }
    [MaxLength(50)]
    public required string Model { get; set; }
    [MaxLength(4)]
    public required string Year { get; set; }
    [MaxLength(200)]
    public string? Description { get; set; }
    
    public ICollection<SuggestionVote>? VotesFor { get; set; }
    public ICollection<MotorfestVehicle>? Implementations { get; set; }
}