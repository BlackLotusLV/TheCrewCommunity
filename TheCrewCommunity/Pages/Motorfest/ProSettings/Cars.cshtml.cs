using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.GameData;
using TheCrewCommunity.Data.WebData.ProSettings;

namespace TheCrewCommunity.Pages.Motorfest.ProSettings;

public class Cars(IDbContextFactory<LiveBotDbContext> contextFactory, GeneralUtils generalUtils) : PageModel
{
    public List<MtfstCarProSettings> ProSettingsList { get; set; } = [];
    public Dictionary<MtfstCarProSettings, string> ProSettingsDictionary { get; } = [];
    public List<VehicleCategory> VCatList { get; set; } = [];

    private static readonly char[] Separator = [' '];

    public async Task OnGetAsync()
    {
        await using LiveBotDbContext dbContext = await contextFactory.CreateDbContextAsync();

        VCatList = await dbContext.VehicleCategories
            .Include(x => x.Game)
            .Where(vCat =>
                vCat.Game != null &&
                vCat.Game.Name.Equals("The Crew Motorfest") &&
                vCat.Type.Equals("car"))
            .AsNoTracking()
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

    public async Task<IActionResult> OnGetLoadProSettingsAsync(string vCatUuid = "", string? search = "")
    {
        var data = await LoadProSettingsDictionaryAsync();
        if (vCatUuid != "" && Guid.TryParse(vCatUuid, out Guid vCatGuid))
        {
            data = data.Where(x => x.Vehicle.VCatId == vCatGuid).ToList();
        }

        var result = data.Select(x => new
        {
            x.Id,
            AuthorDiscordId = x.DiscordId,
            AuthorName = x.ApplicationUser.UserName,
            Vehicle = new
            {
                BrandName = x.Vehicle.Brand.Name,
                Model = x.Vehicle.ModelName,
                x.Vehicle.Year,
                x.Vehicle.VCatId
            },
            x.Name,
            x.LikesCount,
            SearchKey = $"{x.Vehicle.Brand.Name} {x.Vehicle.ModelName} {x.ApplicationUser.UserName} {x.Name} {x.Description}".ToLower()
        });
        if (string.IsNullOrEmpty(search)) return new JsonResult(result);
        string[] searchTokens = search.ToLower().Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        var resultsWithMatchQuality = result.Select(x =>
        {
            string[] comparisonTokens = x.SearchKey.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
            int totalDistance = searchTokens.Sum(searchToken =>
                comparisonTokens.Min(comparisonToken =>
                    generalUtils.CalculateLevenshteinDistance(searchToken, comparisonToken)));
            return (matchQuality: totalDistance, result: x);
        });

        result = resultsWithMatchQuality.OrderBy(x => x.matchQuality).Select(x => x.result);
        return new JsonResult(result);
    }
}