using System.ComponentModel.DataAnnotations;

namespace TheCrewCommunity.Data.GameData;

public class Brand
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    [MaxLength(30)]
    public required string Name { get; set; }
    [MaxLength(2)]
    public string? CountryCode { get; set; }
    
    public ICollection<Vehicle>? Vehicles { get; set; }
}