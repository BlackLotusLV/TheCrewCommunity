using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.EventHandlers;

public sealed class SystemEvents(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService)
{
    public Task SessionCreated(DiscordClient client, SessionReadyEventArgs eventArgs)
    {
        client.Logger.LogInformation(CustomLogEvents.LiveBot,"Client is ready to process events");
        return Task.CompletedTask;
    }
    public async Task GuildAvailable(DiscordClient client, GuildCreateEventArgs e)
    {
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        _ = await dbContext.Guilds.FindAsync(e.Guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(e.Guild.Id));
        client.Logger.LogInformation(CustomLogEvents.LiveBot,"Guild available: {GuildName}", e.Guild.Name);
    }
    public Task ClientErrored(DiscordClient client, ClientErrorEventArgs e)
    {
        client.Logger.LogError(CustomLogEvents.ClientError,e.Exception, "Exception occurred");
        return Task.CompletedTask;
    }
    public Task SlashExecuted(SlashCommandsExtension ext, SlashCommandExecutedEventArgs e)
    {
        ext.Client.Logger.LogInformation(CustomLogEvents.SlashExecuted,"{Username} successfully executed '{CommandName}-{QualifiedName}' command", e.Context.User.Username, e.Context.CommandName, e.Context.QualifiedName);
        return Task.CompletedTask;
    }

    public Task SlashErrored(SlashCommandsExtension ext, SlashCommandErrorEventArgs e)
    {
        ext.Client.Logger.LogError(CustomLogEvents.SlashErrored,e.Exception, "{Username} tried executing '{CommandName}-{QualifiedName}' command, but it errored", e.Context.User.Username, e.Context.CommandName, e.Context.QualifiedName);
        return Task.CompletedTask;
    }

    public Task ContextMenuExecuted(SlashCommandsExtension ext, ContextMenuExecutedEventArgs e)
    {
        ext.Client.Logger.LogInformation(CustomLogEvents.ContextMenuExecuted,"{Username} Successfully executed '{CommandName}' menu command", e.Context.User.Username, e.Context.CommandName);
        return Task.CompletedTask;
    }

    public Task ContextMenuErrored(SlashCommandsExtension ext, ContextMenuErrorEventArgs e)
    {
        ext.Client.Logger.LogError(CustomLogEvents.ContextMenuErrored,e.Exception, "{Username} tried executing '{CommandName}' menu command, but it errored", e.Context.User.Username, e.Context.CommandName);
        return Task.CompletedTask;
    }

    public Task CommandExecuted(CommandsExtension extension, CommandExecutedEventArgs args)
    {
        extension.Client.Logger.LogInformation(CustomLogEvents.CommandExecuted,"{Username}({UserId}) successfully executed '{CommandName}' command", args.Context.User.Username, args.Context.User.Id, args.Context.Command.Name);
        return Task.CompletedTask;
    }
    public Task CommandErrored(CommandsExtension extension, CommandErroredEventArgs args)
    {
        extension.Client.Logger.LogError(CustomLogEvents.CommandErrored,args.Exception, "{Username}({UserId}) tried executing '{CommandName}' command, but it errored", args.Context.User.Username, args.Context.User.Id, args.Context.Command?.Name?? "Unknown");
        return Task.CompletedTask;
    }
}