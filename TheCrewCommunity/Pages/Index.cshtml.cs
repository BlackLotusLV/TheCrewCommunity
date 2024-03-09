using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheCrewCommunity.LiveBot;

namespace TheCrewCommunity.Pages;

public class IndexModel(ILogger<IndexModel> logger, ILiveBotService liveBotService) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;

    public string? UptimeString { get; private set; }

    public void OnGet()
    {
        UptimeString = (DateTime.UtcNow-liveBotService.StartTime).ToString(@"dd\.hh\:mm\:ss");
    }
}