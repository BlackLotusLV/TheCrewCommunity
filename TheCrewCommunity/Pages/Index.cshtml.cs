using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheCrewCommunity.LiveBot;

namespace TheCrewCommunity.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ILiveBotService _liveBotService;

    public IndexModel(ILogger<IndexModel> logger, ILiveBotService liveBotService)
    {
        _logger = logger;
        _liveBotService = liveBotService;
    }
    public string UptimeString { get; private set; }

    public void OnGet()
    {
        UptimeString = (DateTime.UtcNow-_liveBotService.StartTime).ToString(@"dd\.hh\:mm\:ss");
    }
}