using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers.MessageCreated;

public static class HandleEvent
{
    public static async Task OnMessageCreated(DiscordClient client, MessageCreatedEventArgs eventArgs)
    {
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        var moderatorLoggingService = client.ServiceProvider.GetRequiredService<IModeratorLoggingService>();
        var modMailService = client.ServiceProvider.GetRequiredService<IModMailService>();
        var warningService = client.ServiceProvider.GetRequiredService<IModeratorWarningService>();
        var userActivityService = client.ServiceProvider.GetRequiredService<IUserActivityService>();
        
        if (eventArgs.Guild is null)
        {
            await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
            ModMail? mmEntry = await liveBotDbContext.ModMail.FirstOrDefaultAsync(w => w.UserDiscordId == eventArgs.Author.Id && w.IsActive);
            if (mmEntry is not null)
                await modMailService.ProcessModMailDm(client, eventArgs, mmEntry);
        }
        if(eventArgs.Guild is null || eventArgs.Author.IsBot) return;
        await userActivityService.UpdateUserActivityAsync(eventArgs.Author, eventArgs.Guild);
    }
}