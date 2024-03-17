using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TheCrewCommunity.Pages.Account;

public class Login : PageModel
{
    private readonly ILogger<Login> _logger;

    public Login(ILogger<Login> logger)
    {
        _logger = logger;
    }

    public IActionResult OnGet(string returnUrl = "/")
    {
        _logger.LogInformation("Login - this is unique");
        return Challenge(new AuthenticationProperties() { RedirectUri = returnUrl }, "Discord");
    }
}