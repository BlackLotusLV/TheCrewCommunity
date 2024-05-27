using System.Collections.Immutable;
using System.Security.Claims;
using DSharpPlus.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data.WebData;
using TheCrewCommunity.LiveBot;

namespace TheCrewCommunity.Pages.Account;

public class Registering : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILiveBotService _liveBotService;
    private readonly ILogger<Registering> _logger;
    
    public Registering(UserManager<ApplicationUser> userManager, ILiveBotService liveBotService, ILogger<Registering> logger)
    {
        _userManager = userManager;
        _liveBotService = liveBotService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        string userId = User.Claims.First(claim => claim.Type is ClaimTypes.NameIdentifier).Value;
        ApplicationUser? applicationUser = await _userManager.Users.FirstOrDefaultAsync(x => x.DiscordId == Convert.ToUInt64(userId));

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

        return RedirectToPage("./Profile");
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
            discordUser = await _liveBotService.DiscordClient.GetUserAsync(discordId);
        }
        catch
        {
            _logger.LogDebug("Failed to get user of id {ID}", discordId);
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
        applicationUser.IsModerator = discordUser is not null && await ComputeModeratorStatus(discordUser, discordId);
        if (create)
        {
            IdentityResult createResult = await _userManager.CreateAsync(applicationUser);
            if (!createResult.Succeeded)
            {
                return false;
            }
        }
        
        IdentityResult saveResult = await _userManager.UpdateAsync(applicationUser);
        return saveResult.Succeeded;
    }
    private async Task<bool> ComputeModeratorStatus(DiscordUser discordUser, ulong discordId)
    {
        DiscordMember? discordMember = null;
        try
        {
            DiscordGuild guild = await _liveBotService.DiscordClient.GetGuildAsync(150283740172517376);
            discordMember = await guild.GetMemberAsync(discordId);
        }
        catch
        {
            _logger.LogDebug("Failed to get member of id {ID}", discordId);
        }

        return discordMember is { Permissions: DiscordPermissions.ModerateMembers } or { Permissions: DiscordPermissions.All };
    }
}