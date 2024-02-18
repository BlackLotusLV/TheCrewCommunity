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

    [HttpGet]
    public IActionResult Login(string returnUrl = "/")
    {
        return Challenge(new AuthenticationProperties() { RedirectUri = returnUrl }, "Discord");
    }

    [HttpGet]
    public async Task<IActionResult> CallBack()
    {
        AuthenticateResult result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (result.Succeeded != true)
        {
            return BadRequest("Error authenticating with Discord");
        }

        // You can access user info here
        var claims = result.Principal.Identities
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
        }

        await _signInManager.SignInAsync(user, isPersistent: true);
        
        return Ok(claims);
    }
}