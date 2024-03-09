using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.Controllers;

public class AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDatabaseMethodService dbMethodService) : Controller
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly IDatabaseMethodService _dbMethodService = dbMethodService;

    [Route("Account/Callback")]
    public async Task<IActionResult> DiscordCallback()
    {
        var authResult = await HttpContext.AuthenticateAsync("Discord");
        if (!authResult.Succeeded)
        {
            return RedirectToAction("Error", "Home");
        }
        
        var claims = authResult.Principal.Identities
            .FirstOrDefault()?.Claims
            .Select(claim => new { claim.Issuer, claim.OriginalIssuer, claim.Type, claim.Value });
        
        string? discordIdString = claims.FirstOrDefault(claim => claim.Type == "id")?.Value;

        if (discordIdString is null)
        {
            return BadRequest("Error getting Discord ID");
        }
        
        var discordId = Convert.ToUInt64(discordIdString);
        ApplicationUser? user =  await _userManager.Users.FirstOrDefaultAsync(x=>x.DiscordId == Convert.ToUInt64(discordIdString));

        if (user == null)
        {
            user = new ApplicationUser { DiscordId = discordId };
            IdentityResult createResult = await _userManager.CreateAsync(user);
            
            if (!createResult.Succeeded)
            {
                return BadRequest("Error creating user");
            }
            
            var saveResult = await _userManager.UpdateAsync(user);
            if (!saveResult.Succeeded)
            {
                return BadRequest("Error saving user");
            }
        }

        await _signInManager.SignInAsync(user, isPersistent: true);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = "/")
    {
        return Challenge(new AuthenticationProperties() { RedirectUri = returnUrl }, "Discord");
    }
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}