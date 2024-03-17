using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.Pages.Account;

public class Logout : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public Logout(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnGet()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }
}