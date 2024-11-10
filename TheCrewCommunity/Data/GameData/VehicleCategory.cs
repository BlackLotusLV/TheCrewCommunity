using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data.GameData;

public class VehicleCategory
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    
    [MaxLength(30)]
    public required string Name { get; set; }
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    [MaxLength(6)]
    public required string Type { get; set; }
    
    public ICollection<Vehicle>? Vehicles { get; set; }
}