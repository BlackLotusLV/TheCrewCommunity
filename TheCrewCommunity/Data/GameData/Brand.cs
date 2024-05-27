using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data.GameData;

public class Brand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(20)]
    public required string Name { get; set; }
    [MaxLength(2)]
    public required string CountryCode { get; set; }
    
    public ICollection<Vehicle>? Vehicles { get; set; }
}