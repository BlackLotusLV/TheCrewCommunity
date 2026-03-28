namespace TheCrewCommunity.Data.Entities.GameData.Motorfest;

public class MotorfestVehicle
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required Guid BrandId { get; set; }
    public MotorfestVehicleBrand? Brand { get; set; }
    public required string ModelName { get; set; }
    public required DateOnly Year { get; set; }
    public required Guid EngineTypeId { get; set; }
    public MotorfestVehicleEngineType? EngineType { get; set; }
    public required Guid CategoryId { get; set; }
    public MotorfestVehicleCategory? Category { get; set; }
    public required Guid PeriodId { get; set; }
    public MotorfestVehiclePeriod? Period { get; set; }
    public int PriceBucks { get; set; }
    public int PriceCredits { get; set; }
    public required Guid StyleId { get; set; }
    public MotorfestVehicleStyle? Style { get; set; }
    public required Guid TagId { get; set; }
    public MotorfestVehicleTag? Tag { get; set; }
    public required string CountryId { get; set; }
    public MotorfestVehicleCountry? Country { get; set; }
    public required Guid TypeId { get; set; }
    public MotorfestVehicleType? Type { get; set; }
    public Guid? ImageId { get; set; }
}