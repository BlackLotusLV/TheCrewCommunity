using System.ComponentModel.DataAnnotations;
using TheCrewCommunity.Data.WebData;

namespace TheCrewCommunity.Data.GameData;

public class Game
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    [MaxLength(30)]
    public required string Name { get; init; }
    public DateOnly ReleaseDate { get; init; }
    [MaxLength(20)]
    public string? IconFile { get; init; }
    
    public ICollection<Vehicle>? Vehicles { get; init; }
    public ICollection<VehicleCategory>? VehicleCategories { get; init; }
    public ICollection<UserImage>? UserImages { get; init; }
}