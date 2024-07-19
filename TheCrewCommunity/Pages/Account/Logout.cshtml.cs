using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheCrewCommunity.Data.WebData;

namespace TheCrewCommunity.Pages.Account;

public class Logout(SignInManager<ApplicationUser> signInManager, ILogger<Logout> logger) : PageModel
{
    public async Task<IActionResult> OnGet()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await signInManager.SignOutAsync();
        logger.LogDebug("User logged out");
        return Redirect("~/");
    }
}