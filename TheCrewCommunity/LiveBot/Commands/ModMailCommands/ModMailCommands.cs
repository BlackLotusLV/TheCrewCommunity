using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModMailCommands;
[Command("Modmail"), Description("Moderator Mail commands"), RequireGuild, RequirePermissions(DiscordPermission.ManageMessages)]
public class ModMailCommands(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService, IModMailService modMailService)
{
    [Command("reply"), Description("Replies to a specific mod mail")]
    public async Task ReplyAsync(SlashCommandContext ctx, [SlashAutoCompleteProvider(typeof(ActiveModMailOption)),Description("Mod Mail Entry ID")] long id, [Description("The message to send to the user")] string reply)
        => await ReplyCommand.ExecuteAsync(ctx, id, reply, dbContextFactory, databaseMethodService);
    
    [Command("close"), Description("Closes a specific mod mail")]
    public async Task CloseAsync(SlashCommandContext ctx, [SlashAutoCompleteProvider(typeof(ActiveModMailOption)), Description("Mod Mail Entry ID")] long id)
        => await CloseCommand.ExecuteAsync(ctx, id, dbContextFactory, modMailService);
    
    [Command("Block"), Description("Blocks a user from using mod mail")]
    public async Task BlockAsync(SlashCommandContext ctx, [Description("User to block from using mod mail")] DiscordUser user)
        => await BlockCommand.ExecuteAsync(ctx, user, dbContextFactory);
    
    [Command("Unblock"), Description("Unblocks a user from using mod mail")]
    public async Task UnblockAsync(SlashCommandContext ctx, [Description("User to unblock from using mod mail")] DiscordUser user)
        => await UnblockCommand.ExecuteAsync(ctx, user, dbContextFactory);
}