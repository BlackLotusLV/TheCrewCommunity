using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TheCrewCommunity.Pages.Account;

public class Login : PageModel
{
    public IActionResult OnGet(string returnUrl = "/")
    {
        return Challenge(new AuthenticationProperties { RedirectUri = "/" }, "Discord");
    }
}