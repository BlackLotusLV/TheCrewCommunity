using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers;

public static class SystemEvents
{
    public static Task SessionCreated(DiscordClient client, SessionCreatedEventArgs eventArgs)
    {
        client.Logger.LogInformation(CustomLogEvents.LiveBot,"Client is ready to process events");
        return Task.CompletedTask;
    }
    public static async Task GuildAvailable(DiscordClient client, GuildCreatedEventArgs e)
    {
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        _ = await dbContext.Guilds.FindAsync(e.Guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(e.Guild.Id));
        client.Logger.LogInformation(CustomLogEvents.LiveBot,"Guild available: {GuildName}", e.Guild.Name);
    }

    public static Task CommandExecuted(CommandsExtension extension, CommandExecutedEventArgs args)
    {
        extension.Client.Logger.LogInformation(CustomLogEvents.CommandExecuted,"{Username}({UserId}) successfully executed '{CommandName}' command", args.Context.User.Username, args.Context.User.Id, args.Context.Command.FullName);
        return Task.CompletedTask;
    }
    public static Task CommandErrored(CommandsExtension extension, CommandErroredEventArgs args)
    {
        extension.Client.Logger.LogError(CustomLogEvents.CommandErrored,args.Exception, "{Username}({UserId}) tried executing '{CommandName}' command, but it errored", args.Context.User.Username, args.Context.User.Id, args.Context.Command?.FullName?? "Unknown");
        return Task.CompletedTask;
    }
}