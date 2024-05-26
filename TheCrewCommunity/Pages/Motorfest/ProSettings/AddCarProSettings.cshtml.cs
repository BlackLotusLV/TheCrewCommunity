using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.GameData;
using TheCrewCommunity.Data.WebData;
using TheCrewCommunity.Data.WebData.ProSettings;

namespace TheCrewCommunity.Pages.Motorfest.ProSettings;

public class AddCarProSettings(IDbContextFactory<LiveBotDbContext> dbContextFactory, ILogger<AddCarProSettings> logger) : PageModel
{
    public List<VehicleCategory> VCatList { get; set; } = [];
    [BindProperty]
    public CarProSettingsViewModel FormData { get; set; }
    public async Task OnGetAsync()
    {
        if (User.Identity is null || User.Identity.IsAuthenticated is false)
        {
            RedirectToPage("/Account/Login");
            return;
        }
        await FetchVCatListAsync();
    }

    private async Task FetchVCatListAsync()
    {
        LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        VCatList = await dbContext.VehicleCategories
        .Include(x => x.Game)
        .Where(vCat =>
            vCat.Game != null &&
            vCat.Game.Name.Equals("The Crew Motorfest") &&
            vCat.Type.Equals("car"))
        .AsNoTracking()
        .ToListAsync();
    }

    public async Task<IActionResult> OnGetLoadBrandsAsync(Guid vCatId)
    {
        LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        var result = dbContext.Brands
            .Include(x => x.Vehicles)
            .Where(x => x.Vehicles.Any(vehicle=>vehicle.VCatId==vCatId))
            .Select(x => new
            {
                x.Id,
                x.Name
            })
            .AsNoTracking();
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnGetLoadVehiclesAsync(Guid vCatId, Guid brandId)
    {
        LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        var result = dbContext.Vehicles
            .Where(x => x.VCatId == vCatId && x.BrandId == brandId)
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                BrandName = x.Brand.Name,
                x.ModelName,
                x.Transmission
            });
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostSubmitProSettingsAsync()
    {
        if (User.Identity is null || User.Identity.IsAuthenticated is false)
        {
            return Unauthorized();
        }
        LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        ApplicationUser? appUser = null;
        if (ulong.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out ulong userId))
        {
            appUser = await dbContext.ApplicationUsers.FirstOrDefaultAsync(x=>x.DiscordId == userId);
        }
        if (appUser is null)
        {
            return BadRequest("User not found. Please login");
        }
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError("","Failed to validate the form, please check that input data is correct");
            logger.LogDebug("Pro settings model validation failed");
            if (FormData.VehicleId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(CarProSettingsViewModel.VehicleId), "You must select a vehicle!");
                logger.LogDebug("Pro settings creation stopped due to vehicle ID being empty");
            }
            if (string.IsNullOrEmpty(FormData.Name))
            {
                ModelState.AddModelError(nameof(CarProSettingsViewModel.Name),"You must provide a name for the pro setting entry!");
                logger.LogDebug("Pro settings name not provided, is empty or null");
            }
            await FetchVCatListAsync();
            return Page();
        }
        
        MtfstCarProSettings newEntry = new()
        {
            Name = FormData.Name,
            DiscordId = appUser.DiscordId,
            VehicleId = FormData.VehicleId,
            Description = FormData.Description ?? string.Empty,
            LikesCount = 0,
            FinalDrive = FormData.FinalDrive,
            PowerToFront = FormData.PowerToFront,
            GripFront = FormData.GripFront,
            GripRear = FormData.GripRear,
            BrakeToFront = FormData.BrakeToFront,
            BrakePower = FormData.BrakePower,
            LoadFront = FormData.LoadFront,
            LoadRear = FormData.LoadRear,
            SpringFront = FormData.SpringFront,
            SpringRear = FormData.SpringRear,
            DamperCompressionFront = FormData.DamperCompressionFront,
            DamperCompressionRear = FormData.DamperCompressionRear,
            DamperReboundFront = FormData.DamperReboundFront,
            DamperReboundRear = FormData.DamperReboundRear,
            AntiRollBarFront = FormData.AntiRollBarFront,
            AntiRollBarRear = FormData.AntiRollBarRear,
            CamberFront = FormData.CamberFront,
            CamberRear = FormData.CamberRear
        };
        
        MtfstCarProSettings? existingEntry = await dbContext.MotorfestCarProSettings
            .FirstOrDefaultAsync(x =>
                x.DiscordId == userId &&
                x.VehicleId == newEntry.VehicleId &&
                x.FinalDrive == newEntry.FinalDrive &&
                x.PowerToFront == newEntry.PowerToFront &&
                x.GripFront == newEntry.GripFront &&
                x.GripRear == newEntry.GripRear &&
                x.BrakeToFront == newEntry.BrakeToFront &&
                x.BrakePower == newEntry.BrakePower &&
                x.LoadFront == newEntry.LoadFront &&
                x.LoadRear == newEntry.LoadRear &&
                x.SpringFront == newEntry.SpringFront &&
                x.SpringRear == newEntry.SpringRear &&
                x.DamperCompressionFront == newEntry.DamperCompressionFront &&
                x.DamperCompressionRear == newEntry.DamperCompressionRear &&
                x.DamperReboundFront == newEntry.DamperReboundFront &&
                x.DamperReboundRear == newEntry.DamperReboundRear &&
                x.AntiRollBarFront == newEntry.AntiRollBarFront &&
                x.AntiRollBarRear == newEntry.AntiRollBarRear &&
                x.CamberFront == newEntry.CamberFront &&
                x.CamberRear == newEntry.CamberRear);
        if (existingEntry != null)
        {
            ModelState.AddModelError("", "A configuration with the same settings already exists.");
            logger.LogDebug("New port settings entry creation attempt stopped due to one already existing with same properties");
            await FetchVCatListAsync();
            return Page();
        }


        await dbContext.MotorfestCarProSettings.AddAsync(newEntry);
        await dbContext.SaveChangesAsync();
        string redirectUrl = Url.Page("/Motorfest/ProSettings/CarDetails", new { proSettingsId = newEntry.Id })?? throw new ApplicationException("Unable to generate URL for newly created entry.");
        logger.LogDebug("Created a pro settings entry for {Car} by {User}", newEntry.VehicleId, newEntry.DiscordId);
        return Redirect(redirectUrl);
    }

    public class CarProSettingsViewModel
    {
        public required string Name { get; set; }
        public required Guid VehicleId { get; set; }
        public string? Description { get; set; }
        public sbyte FinalDrive { get; set; }
        public byte? PowerToFront { get; set; }
        public sbyte GripFront { get; set; }
        public sbyte GripRear { get; set; }
        public byte BrakeToFront { get; set; }
        public sbyte BrakePower { get; set; }
        public sbyte LoadFront { get; set; }
        public sbyte LoadRear { get; set; }
        public sbyte SpringFront { get; set; }
        public sbyte SpringRear { get; set; }
        public sbyte DamperCompressionFront { get; set; }
        public sbyte DamperCompressionRear { get; set; }
        public sbyte DamperReboundFront { get; set; }
        public sbyte DamperReboundRear { get; set; }
        public sbyte AntiRollBarFront { get; set; }
        public sbyte AntiRollBarRear { get; set; }
        public sbyte CamberFront { get; set; }
        public sbyte CamberRear { get; set; }
    }
}