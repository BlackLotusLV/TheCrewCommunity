using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.Pages.Account;

public class Registering : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    
    Registering(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }
    
    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetAsync()
    {
        ApplicationUser? applicationUser = await _userManager.GetUserAsync(User);
        if (applicationUser != null)
        {
            return RedirectToAction("Index", "Home");
        }
        var claims = User.Identities
            .First().Claims
            .Select(claim => new { claim.Issuer, claim.OriginalIssuer, claim.Type, claim.Value });
        string? discordIdString = claims.FirstOrDefault(x=>x.Type is ClaimTypes.NameIdentifier)?.Value;
        if (discordIdString is null)
        {
            return BadRequest("Error getting Discord ID");
        }
        var discordId = Convert.ToUInt64(discordIdString);
        
        string userName = claims.First(x=>x.Type is ClaimTypes.Name)?.Value??discordId.ToString();
        string email = claims.First(x=>x.Type is ClaimTypes.Email)?.Value??"";
        
        applicationUser = new ApplicationUser
        {
            DiscordId = discordId,
            UserName = userName,
            Email = email
        };
        IdentityResult createResult = await _userManager.CreateAsync(applicationUser);
        if (!createResult.Succeeded)
        {
            return BadRequest("Error creating user");
        }
        IdentityResult saveResult = await _userManager.UpdateAsync(applicationUser);
        if (!saveResult.Succeeded)
        {
            return BadRequest("Error saving user");
        }
        return RedirectToAction("index", "Home");
    }
}