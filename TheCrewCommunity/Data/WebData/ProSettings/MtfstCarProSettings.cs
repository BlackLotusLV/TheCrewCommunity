using System.ComponentModel.DataAnnotations;
using TheCrewCommunity.Data.GameData;

namespace TheCrewCommunity.Data.WebData.ProSettings;

public class MtfstCarProSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ulong DiscordId
    {
        get => _discordId;
        set => _discordId = Convert.ToUInt64(value);
    }
    private ulong _discordId;
    public Guid VehicleId { get; set; }
    [MaxLength(40)]
    public required string Name { get; set; }
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    [Range(-20,0)]
    public required sbyte FinalDrive { get; set; }
    [Range(20,60)]
    public byte? PowerToFront { get; set; }
    [Range(-20,0)]
    public required sbyte GripFront { get; set; }
    [Range(-20,0)]
    public required sbyte GripRear { get; set; }
    [Range(40,80)]
    public required byte BrakeToFront { get; set; }
    [Range(-30,0)]
    public required sbyte BrakePower { get; set; }
    [Range(-30,0)]
    public required sbyte LoadFront { get; set; }
    [Range(-30,0)]
    public required sbyte LoadRear { get; set; }
    [Range(-20,10)]
    public required sbyte SpringFront { get; set; }
    [Range(-20,10)]
    public required sbyte SpringRear { get; set; }
    [Range(-20,20)]
    public required sbyte DamperCompressionFront { get; set; }
    [Range(-20,20)]
    public required sbyte DamperCompressionRear { get; set; }
    [Range(-20,20)]
    public required sbyte DamperReboundFront { get; set; }
    [Range(-20,20)]
    public required sbyte DamperReboundRear { get; set; }
    [Range(-20,10)]
    public required sbyte AntiRollBarFront { get; set; }
    [Range(-20,10)]
    public required sbyte AntiRollBarRear { get; set; }
    [Range(-25,25)]
    public required sbyte CamberFront { get; set; }
    [Range(-25,25)]
    public required sbyte CamberRear { get; set; }
    
    public Vehicle Vehicle { get; set; }
    public ApplicationUser ApplicationUser { get; set; }
}