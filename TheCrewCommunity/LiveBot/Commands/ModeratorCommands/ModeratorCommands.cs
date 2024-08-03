using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

[Command("Mod"), Description("Moderator commands"), RequireGuild]
public class ModeratorCommands(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService, IModeratorWarningService warningService, IModeratorLoggingService moderatorLoggingService, IModMailService modMailService)
{
    [Command("warn"), Description("Warns a user"), RequirePermissions(DiscordPermissions.KickMembers)]
    public async Task Warn(SlashCommandContext ctx,
        [Description("User to warn")] DiscordUser user,
        [Description("Why the user is being warned")] string reason,
        [Description("Image to attach to the warning")] DiscordAttachment? image = null,
        [Description("How long the warning will last")] WarnCommand.TimeOutOptions timeOut = 0)
        => await WarnCommand.ExecuteAsync(warningService, ctx, user, reason, timeOut, image);
    
    [Command("unwarn"), Description("Removes a warning from the user"), RequirePermissions(DiscordPermissions.KickMembers)]
    public async Task UnWarn(SlashCommandContext ctx,
        [Description("User to remove the warning for")] DiscordUser user,
        [Description("The ID of a specific warning. Leave as is if don't want a specific one"), SlashAutoCompleteProvider(typeof(ActiveWarningAutocompleteProvider))] long warningId = -1)
        => await UnWarnCommand.ExecuteAsync(warningService, ctx, user, warningId);
    
    [Command("addnote"), Description("Add a note to the user"), RequirePermissions(DiscordPermissions.ModerateMembers)]
    public async Task AddNote(SlashCommandContext ctx,
        [Description("User to who to add the note to")] DiscordUser user,
        [Description("Contents of the note.")] string note,
        [Description("Image to attach to the note")] DiscordAttachment? image = null)
        => await AddNoteCommand.ExecuteAsync(dbContextFactory, moderatorLoggingService, databaseMethodService, ctx, user, note, image);
    
    [Command("deletenote"), Description("Delete a note from the user."), RequireGuild, RequirePermissions(DiscordPermissions.ModerateMembers)]
    public async Task DeleteNote(SlashCommandContext ctx,
        [Description("User who's note to delete")] DiscordUser user,
        [Description("The ID of the note to delete"), SlashAutoCompleteProvider(typeof(UserNotesAutocompleteProvider))] long noteId)
    => await DeleteNoteCommand.ExecuteAsync(dbContextFactory, moderatorLoggingService, ctx, user, noteId);
    
    [Command("editnote"), Description("Edit a note"), RequirePermissions(DiscordPermissions.ModerateMembers)]
    public async Task EditNote(SlashCommandContext ctx,
        [Description("User who's note to edit")] DiscordUser user,
        [Description("The ID of the note to edit"), SlashAutoCompleteProvider(typeof(UserNotesAutocompleteProvider))] long noteId)
    => await EditNoteCommand.ExecuteAsync(dbContextFactory, moderatorLoggingService, ctx, user, noteId);
    
    [Command("Prune"),Description("Prune the message in the channel"),RequirePermissions(DiscordPermissions.ManageMessages)]
    public async Task Prune(SlashCommandContext ctx,
        [Description("The amount of messages to delete (1-100)")] long messageCount)
    => await PruneCommand.ExecuteAsync(ctx, messageCount);
    
    [Command("Prune"), SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu), RequirePermissions(DiscordPermissions.ManageMessages)]
    public async Task PruneMenu(SlashCommandContext ctx, DiscordMessage targetMessage)
    => await PruneContextMenu.ExecuteAsync(ctx, targetMessage);
    
    [Command("Prune-User"), SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu), RequirePermissions(DiscordPermissions.ManageMessages)]
    public async Task PruneUserMenu(SlashCommandContext ctx, DiscordMessage targetMessage) 
        => await PruneUserContextMenu.ExecuteAsync(ctx, targetMessage);
    
    [Command("whitelist-invite"), Description("Whitelist an invite code"), RequirePermissions(DiscordPermissions.ModerateMembers)]
    public async Task WhitelistInvite( SlashCommandContext ctx, [Description("The invite code to be whitelisted")] string code)
    => await WhitelistInviteCommand.ExecuteAsync(dbContextFactory, ctx, code);
    
    [Command("Message"), Description("Sends a message to specified user. Requires Mod Mail feature enabled."), RequirePermissions(DiscordPermissions.ManageMessages)]
    public async Task MessageAsync(SlashCommandContext ctx, [Description("Specify the user who to mention")] DiscordUser user, [Description("Message to send to the user.")] string message)
    => await MessageCommand.ExecuteAsync(dbContextFactory, databaseMethodService, modMailService, ctx, user, message);
    
    [Command("Stats"),Description("Displays moderator stats for the server."),RequirePermissions(DiscordPermissions.ManageMessages)]
    public async Task StatsAsync(CommandContext ctx)
    => await StatsCommand.ExecuteAsync(dbContextFactory, ctx);
    
    [Command("faq-create"), Description("Creates a new FAQ message"), RequirePermissions(DiscordPermissions.ManageMessages)]
    public async Task CreateFaqAsync(SlashCommandContext ctx)
    => await CreateFaqCommand.ExecuteAsync(ctx);

    [Command("faq-edit"), Description("Edits an existing FAQ message, using the message ID"), RequirePermissions(DiscordPermissions.ManageMessages)]
    public async Task EditFaqAsync(SlashCommandContext ctx, [Description("The message ID to edit")] string messageId)
    => await EditFaqCommand.ExecuteAsync(ctx, messageId);
    
    [Command("Say"), Description("Bot says a something"), RequirePermissions(DiscordPermissions.ManageMessages)]
    public async Task SayAsync(SlashCommandContext ctx, [Description("The message what the bot should say.")] string message,
        [Description("Channel where to send the message")] DiscordChannel? channel = null)
    => await SayCommand.ExecuteAsync(ctx, message, channel);
    
    [Command("timeout"), Description("Timeout a user"), RequirePermissions(DiscordPermissions.KickMembers)]
    public async Task Timeout(SlashCommandContext ctx,
        [Description("User to timeout")] DiscordMember user,
        [Description("How long the timeout will last")] string duration,
        [Description("Reason for the timeout")] string reason)
    => await TimeoutCommand.ExecuteAsync(ctx, user, duration, reason);
}