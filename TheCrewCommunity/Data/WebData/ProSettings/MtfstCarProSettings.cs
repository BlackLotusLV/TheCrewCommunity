using System.ComponentModel.DataAnnotations;
using TheCrewCommunity.Data.GameData;

namespace TheCrewCommunity.Data.WebData.ProSettings;

public class MtfstCarProSettings
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public ulong DiscordId
    {
        get => _discordId;
        init => _discordId = Convert.ToUInt64(value);
    }
    private readonly ulong _discordId;
    public Guid VehicleId { get; init; }
    [MaxLength(40)]
    public required string Name { get; set; }
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedOn { get; init; } = DateTime.UtcNow;
    [Range(0, int.MaxValue, ErrorMessage = "Please enter a value bigger than {1}")]
    public int LikesCount { get; set; } = 0;
    
    [Range(-20,0)]
    public required sbyte FinalDrive { get; init; }
    [Range(20,60)]
    public byte? PowerToFront { get; init; }
    [Range(-20,0)]
    public required sbyte GripFront { get; init; }
    [Range(-20,0)]
    public required sbyte GripRear { get; init; }
    [Range(40,80)]
    public required byte BrakeToFront { get; init; }
    [Range(-30,0)]
    public required sbyte BrakePower { get; init; }
    [Range(-30,0)]
    public required sbyte LoadFront { get; init; }
    [Range(-30,0)]
    public required sbyte LoadRear { get; init; }
    [Range(-20,10)]
    public required sbyte SpringFront { get; init; }
    [Range(-20,10)]
    public required sbyte SpringRear { get; init; }
    [Range(-20,20)]
    public required sbyte DamperCompressionFront { get; init; }
    [Range(-20,20)]
    public required sbyte DamperCompressionRear { get; init; }
    [Range(-20,20)]
    public required sbyte DamperReboundFront { get; init; }
    [Range(-20,20)]
    public required sbyte DamperReboundRear { get; init; }
    [Range(-20,10)]
    public required sbyte AntiRollBarFront { get; init; }
    [Range(-20,10)]
    public required sbyte AntiRollBarRear { get; init; }
    [Range(-25,25)]
    public required sbyte CamberFront { get; init; }
    [Range(-25,25)]
    public required sbyte CamberRear { get; init; }
    
    public Vehicle Vehicle { get; set; }
    public ApplicationUser ApplicationUser { get; set; }
    public ICollection<MtfstCarProSettingsLikes> MotorfestCarProSettingLikes { get; set; }
}