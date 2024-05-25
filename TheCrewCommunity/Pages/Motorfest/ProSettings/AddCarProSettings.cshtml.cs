using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.GameData;
using TheCrewCommunity.Data.WebData.ProSettings;

namespace TheCrewCommunity.Pages.Motorfest.ProSettings;

public class AddCarProSettings(IDbContextFactory<LiveBotDbContext> dbContextFactory) : PageModel
{
    public List<VehicleCategory> VCatList { get; set; } = [];
    public List<Vehicle> VehicleList { get; set; } = [];
    public async Task OnGetAsync()
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
}