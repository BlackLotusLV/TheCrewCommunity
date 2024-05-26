using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.WebData.ProSettings;

namespace TheCrewCommunity.Pages.Motorfest.ProSettings;

public class CarDetails(IDbContextFactory<LiveBotDbContext> contextFactory) : PageModel
{
    public string Title = "Details";
    public int? PowerToRear;
    public int BrakesToRear;
    public MtfstCarProSettings? CarProSettings { get; set; }
    public async Task<IActionResult> OnGetAsync(Guid? proSettingsId)
    {
        if (proSettingsId is null)
        {
            return RedirectToPage("Index");
        }
        LiveBotDbContext dbContext = await contextFactory.CreateDbContextAsync();
        
        CarProSettings = await dbContext.MotorfestCarProSettings
            .Include(x => x.MotorfestCarProSettingLikes)
            .Include(x => x.ApplicationUser)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == proSettingsId);
        if (CarProSettings is null)
        {
            return RedirectToPage("Index");
        }

        Title = CarProSettings.Name;
        PowerToRear = 100 - CarProSettings.PowerToFront;
        BrakesToRear = 100 - CarProSettings.BrakeToFront;
        
        return Page();
    }
}