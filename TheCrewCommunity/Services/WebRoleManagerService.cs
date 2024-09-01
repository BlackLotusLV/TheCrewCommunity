using Microsoft.AspNetCore.Identity;

namespace TheCrewCommunity.Services;

public class WebRoleManagerService(IServiceProvider serviceProvider, ILogger<WebRoleManagerService> logger) : IHostedService
{
    private readonly string[] _roles = ["Administrator", "Moderator", "Banned"];
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (string role in _roles)
        {
            if (await roleManager.RoleExistsAsync(role)) continue;
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            logger.LogInformation(CustomLogEvents.WebRoleManager, "Added a new role to the database: `{Role}`", role);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug(CustomLogEvents.WebRoleManager, "No stop service functionality required, ");
        return Task.CompletedTask;
    }
}