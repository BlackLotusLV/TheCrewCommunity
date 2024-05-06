using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.GameData;
using TheCrewCommunity.Data.WebData.ProSettings;

namespace TheCrewCommunity.Pages.Motorfest.ProSettings;

public class Cars(IDbContextFactory<LiveBotDbContext> contextFactory, GeneralUtils generalUtils, ILogger<Cars> logger) : PageModel
{
    public List<MtfstCarProSettings> ProSettingsList { get; set; } = [];
    public Dictionary<MtfstCarProSettings, string> ProSettingsDictionary { get; } = [];
    public List<VehicleCategory> VCatList { get; set; } = [];
    public async Task OnGetAsync()
    {
        await using LiveBotDbContext dbContext = await contextFactory.CreateDbContextAsync();

        VCatList = await dbContext.VehicleCategories
            .Include(x => x.Game)
            .Where(vCat =>
                vCat.Game != null &&
                vCat.Game.Name.Equals("The Crew Motorfest") &&
                vCat.Type.Equals("car"))
            .ToListAsync();
        
        ProSettingsList = await dbContext.MotorfestCarProSettings
            .Include(x => x.Vehicle).ThenInclude(vehicle => vehicle.Brand)
            .Include(x=>x.Vehicle).ThenInclude(vehicle => vehicle.VCat)
            .Include(x=>x.Vehicle).ThenInclude(vehicle => vehicle.Game)
            .Include(x => x.ApplicationUser)
            .Where(x=>x.Vehicle.Game.Name.Contains("Motorfest"))
            .ToListAsync();
        
        foreach (MtfstCarProSettings item in ProSettingsList)
        {
            ProSettingsDictionary.Add(item,$"{item.Vehicle.Brand.Name} {item.Vehicle.ModelName} {item.Name} {item.ApplicationUser.GlobalUsername}");
        }
    }

    private async Task LoadProSettingsDictionary(string uuid)
    {
        if (Guid.TryParse(uuid, out Guid id))
        {
            logger.LogDebug("Failed to parse GUID by provided string `{Uuid}` for pro settings loading", uuid);
            return;
        }
        ProSettingsDictionary.Clear();
        foreach (MtfstCarProSettings item in ProSettingsList.Where(x=>x.Vehicle.VCatId == id))
        {
            ProSettingsDictionary.Add(item,$"{item.Vehicle.Brand.Name} {item.Vehicle.ModelName} {item.Name} {item.ApplicationUser.GlobalUsername}");
        }
        
    }
    

    [HttpGet]
    public IActionResult GetProSettings(string search)
    {
        // match search term with dictionary by comparing value leivenstein distance from generalutils
        var searchResults = ProSettingsDictionary.Where(x => generalUtils.CalculateLevenshteinDistance(x.Value, search) <= 3).Select(x => x.Key).ToList();

        return new JsonResult(searchResults);
    }
}