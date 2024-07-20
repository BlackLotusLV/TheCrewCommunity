using System.ComponentModel.DataAnnotations;
using TheCrewCommunity.Data.WebData;

namespace TheCrewCommunity.Data.GameData;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(30)]
    public required string Name { get; set; }
    public DateOnly ReleaseDate { get; set; }
    
    public ICollection<Vehicle>? Vehicles { get; set; }
    public ICollection<VehicleCategory>? VehicleCategories { get; set; }
    public ICollection<UserImage>? UserImages { get; set; }
}