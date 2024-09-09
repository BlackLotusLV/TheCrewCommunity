using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.WebData;

namespace TheCrewCommunity.Services;

public class WebRoleManagerService(IServiceProvider serviceProvider, ILogger<WebRoleManagerService> logger, IDbContextFactory<LiveBotDbContext> dbContextFactory) : IHostedService
{
    private readonly string[] _roles = ["Administrator", "Moderator", "Banned"];
    private const ulong AdminId = 86725763428028416;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (string role in _roles)
        {
            if (await roleManager.RoleExistsAsync(role)) continue;
            var identityRole = new IdentityRole<Guid>(role)
            {
                NormalizedName = role.ToUpper()
            };
            IdentityResult roleResult = await roleManager.CreateAsync(identityRole);
            if (roleResult.Succeeded)
            {
                logger.LogInformation(CustomLogEvents.WebRoleManager, "Added a new role to the database: `{Role}`", role);
            }
            else
            {
                logger.LogInformation(CustomLogEvents.WebRoleManager,"Failed to add the role of name `{RoleName}` due to error: {Error}",role, roleResult.Errors.Select(e=>e.Description));
            }
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser? admin = await userManager.Users.SingleOrDefaultAsync(x => x.DiscordId == AdminId, cancellationToken: cancellationToken);
        if (admin is null)
        {
            logger.LogInformation(CustomLogEvents.WebRoleManager,"Admin of Id: {Id} was not found in user list, please check if everything is setup correctly", AdminId);
            return;
        }
        await userManager.AddToRoleAsync(admin, "Administrator");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug(CustomLogEvents.WebRoleManager, "No stop service functionality required, ");
        return Task.CompletedTask;
    }
}