namespace TheCrewCommunity.Data.Entities.GameData.Motorfest;

public class MotorfestVehicleBrand
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Name { get; set; }
}