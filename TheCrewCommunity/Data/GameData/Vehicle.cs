using System.ComponentModel.DataAnnotations;
using TheCrewCommunity.Data.WebData.ProSettings;

namespace TheCrewCommunity.Data.GameData;

public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BrandId { get; set; }
    public Guid GameId { get; set; }
    public Guid VCatId { get; set; }
    [MaxLength(50)]
    public required string ModelName { get; set; }
    public int PriceBucks { get; set; } = 0;
    public int PriceCredits { get; set; } = 0;
    public bool IsSummit { get; set; }
    public bool IsPlaylist { get; set; }
    public bool IsMainStage { get; set; }
    public bool IsDownloadableContent { get; set; }
    public bool IsStories { get; set; }
    [MaxLength(3)]
    public required string Transmission { get; set; }
    
    public Brand? Brand { get; set; }
    public VehicleCategory? VCat { get; set; }
    public Game? Game { get; set; }
    
    public ICollection<MtfstCarProSettings> MotorfestCarProSettings { get; set; }
}