using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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

    private async Task<List<MtfstCarProSettings>> LoadProSettingsDictionaryAsync()
    {
        await using LiveBotDbContext dbContext = await contextFactory.CreateDbContextAsync();
        var list = await dbContext.MotorfestCarProSettings
            .Include(x => x.Vehicle).ThenInclude(vehicle => vehicle.Brand)
            .Include(x=>x.Vehicle).ThenInclude(vehicle => vehicle.VCat)
            .Include(x=>x.Vehicle).ThenInclude(vehicle => vehicle.Game)
            .Include(x => x.ApplicationUser)
            .Where(x=>x.Vehicle.Game.Name.Contains("Motorfest"))
            .ToListAsync();
        return list;
    }

    public async Task<IActionResult> OnGetLoadProSettingsAsync(string vCatUuid = "", string? search ="")
    {
        var data = await LoadProSettingsDictionaryAsync();
        if (vCatUuid != "" && Guid.TryParse(vCatUuid, out Guid vCatGuid))
        {
            data = data.Where(x => x.Vehicle.VCatId == vCatGuid).ToList();
        }
        var result = data.Select(x=> new
        {
            Id=x.Id,
            AuthorDiscordId = x.DiscordId,
            AuthorName = x.ApplicationUser.UserName,
            Vehicle = new
            {
                BrandName = x.Vehicle.Brand.Name,
                Model = x.Vehicle.ModelName,
                Year = x.Vehicle.Year,
                VCatId= x.Vehicle.VCatId
            },
            Name = x.Name,
            Description = x.Description,
            SearchKey=$"{x.Vehicle.Brand.Name} {x.Vehicle.ModelName} {x.ApplicationUser.UserName} {x.Name} {x.Description}"
        });
        if (!string.IsNullOrEmpty(search))
        {
            result = result.OrderBy(x => generalUtils.CalculateLevenshteinDistance(search, x.SearchKey));
        }
        return new JsonResult(result);
    }
}