using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data.GameData;

public class VehicleCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [MaxLength(30)]
    public required string Name { get; set; }
    
    public ICollection<Vehicle> Vehicles { get; set; }
}