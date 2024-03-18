using System.Security.Claims;
using DSharpPlus.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.LiveBot;

namespace TheCrewCommunity.Pages.Account;

public class Profile : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILiveBotService _liveBotService;
    private readonly IDbContextFactory<LiveBotDbContext> _dbContextFactory;
    
    public Profile(UserManager<ApplicationUser> userManager, ILiveBotService liveBotService, IDbContextFactory<LiveBotDbContext> dbContextFactory)
    {
        _userManager = userManager;
        _liveBotService = liveBotService;
        _dbContextFactory = dbContextFactory;
    }
    
    public ApplicationUser? ApplicationUser { get; set; }
    public DiscordMember? DiscordMember { get; private set; }
    public DiscordUser? DiscordUser { get; private set; }
    
    
    public async void OnGet()
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
            return;
        }
        DiscordUser = await _liveBotService.DiscordClient.GetUserAsync(ApplicationUser.DiscordId);
        if (DiscordUser is null) return;
        DiscordGuild guild = await _liveBotService.DiscordClient.GetGuildAsync(150283740172517376);
        DiscordMember = await guild.GetMemberAsync(ApplicationUser.DiscordId);
    }
}