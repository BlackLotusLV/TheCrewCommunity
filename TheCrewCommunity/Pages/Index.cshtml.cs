using Microsoft.AspNetCore.Mvc.RazorPages;
using TheCrewCommunity.LiveBot;

namespace TheCrewCommunity.Pages;

public class IndexModel(ILiveBotService liveBotService) : PageModel
{

    public string? UptimeString { get; private set; }

    public void OnGet()
    {
        UptimeString = (DateTime.UtcNow-liveBotService.StartTime).ToString(@"dd\.hh\:mm\:ss");
    }
}