using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.Pages.Account;

public class Profile : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    
    public Profile(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }
    
    public ApplicationUser? ApplicationUser { get; set; }
    
    public async void OnGet()
    {
        if(User.Identity is null || !User.Identity.IsAuthenticated)
        {
            RedirectToPage("/Account/Login");
        }
        ApplicationUser = await _userManager.GetUserAsync(User);
    }
}