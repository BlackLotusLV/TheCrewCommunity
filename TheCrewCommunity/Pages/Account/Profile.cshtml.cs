using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.WebData;

namespace TheCrewCommunity.Pages.Account;

public class Profile : PageModel
{
    private readonly IDbContextFactory<LiveBotDbContext> _dbContextFactory;
    
    public Profile(UserManager<ApplicationUser> userManager, IDbContextFactory<LiveBotDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    
    public ApplicationUser? ApplicationUser { get; set; }
    
    public async Task OnGetAsync()
    {
        await using LiveBotDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        string? discordIdString = User.Claims.FirstOrDefault(x=>x.Type == ClaimTypes.NameIdentifier)?.Value;
        if (discordIdString is null)
        {
            RedirectToPage("/Account/Login");
            return;
        }
        ulong discordId = ulong.Parse(discordIdString);
        ApplicationUser = await dbContext.ApplicationUsers.FirstOrDefaultAsync(x=>x.DiscordId == discordId);
        if (ApplicationUser is null || User.Identity?.IsAuthenticated is false)
        {
            RedirectToPage("/Account/Login");
        }
    }
}