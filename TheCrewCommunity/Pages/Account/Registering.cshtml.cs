using System.Collections.Immutable;
using System.Security.Claims;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data.WebData;

namespace TheCrewCommunity.Pages.Account;

public class Registering(UserManager<ApplicationUser> userManager, DiscordClient discordClient, ILogger<Login> logger) : PageModel
{
    public async Task<IActionResult> OnGet()
    {
        string userId = User.Claims.First(claim => claim.Type is ClaimTypes.NameIdentifier).Value;
        ApplicationUser? applicationUser = await userManager.Users.FirstOrDefaultAsync(x => x.DiscordId == Convert.ToUInt64(userId));
        ExtractMessageClaim(out string? discordIdString, out ulong discordId, out string? userName, out string? email);
            
        if (discordIdString is null)
        {
            return BadRequest("Error getting Discord ID");
        }
            
        DiscordUser? discordUser = await FetchDiscordUserDetails(discordId);
        if (!await CreateAndUpdateUser(discordId, userName, email, discordUser, applicationUser))
        {
            return BadRequest("Error Creating/Updating or saving user");
        }
            
        return Redirect("~/");
    }
    private void ExtractMessageClaim(out string? discordIdString, out ulong discordId, out string userName, out string email)
    {
        var claims = User.Identities
            .First().Claims
            .Select(claim => new { claim.Issuer, claim.OriginalIssuer, claim.Type, claim.Value })
            .ToImmutableList();
        discordIdString = claims.FirstOrDefault(x => x.Type is ClaimTypes.NameIdentifier)?.Value;
        discordId = Convert.ToUInt64(discordIdString);
        userName = claims.First(x => x.Type is ClaimTypes.Name).Value;
        email = claims.First(x => x.Type is ClaimTypes.Email).Value;
    }
    private async Task<DiscordUser?> FetchDiscordUserDetails(ulong discordId)
    {
        DiscordUser? discordUser = null;
        try
        {
            discordUser = await discordClient.GetUserAsync(discordId);
        }
        catch
        {
            logger.LogDebug("Failed to get user of id {ID}", discordId);
        }
        return discordUser;
    }
    private async Task<bool> CreateAndUpdateUser(ulong discordId, string userName, string email, DiscordUser? discordUser, ApplicationUser? applicationUser)
    {
        var create = false;
        if (applicationUser is null)
        {
            applicationUser = new ApplicationUser();
            create = true;
        }
        applicationUser.DiscordId = discordId;
        applicationUser.UserName = userName;
        applicationUser.Email = email;
        applicationUser.GlobalUsername = discordUser?.GlobalName;
        applicationUser.AvatarUrl = discordUser?.AvatarUrl;
        applicationUser.IsModerator = false;
        if (create)
        {
            IdentityResult createResult = await userManager.CreateAsync(applicationUser);
            if (!createResult.Succeeded)
            {
                return false;
            }

            logger.LogInformation(CustomLogEvents.WebAccount, "New user `{UserName}`({Id}) registered", applicationUser.UserName, applicationUser.DiscordId);
        }
        
        IdentityResult saveResult = await userManager.UpdateAsync(applicationUser);
        logger.LogDebug(CustomLogEvents.WebAccount,"Web user updated");
        return saveResult.Succeeded;
    }
}