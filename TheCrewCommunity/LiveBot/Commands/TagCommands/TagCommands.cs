using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.TagCommands;

[Command("Tag"), Description("Custom response commands"), RequireGuild]
public class TagCommands(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService)
{
    [Command("Create"), Description("Creates a new tag"), RequirePermissions(DiscordPermissions.ManageMessages)]
    public async Task CreateTag(SlashCommandContext ctx,
        [Description("Name of the tag that will be used to select it"), MaxLength(30)] string name)
    => await CreateTagCommand.ExecuteAsync(dbContextFactory, ctx, name);
    
    [Command("Delete"), Description("Deletes a tag"), RequirePermissions(DiscordPermissions.ManageMessages)]
    public async Task DeleteTag(SlashCommandContext ctx,
        [SlashAutoCompleteProvider(typeof(TagAutoCompleteProvider)), Description("Tag to delete.")] string tagId)
    => await DeleteTagCommand.ExecuteAsync(dbContextFactory, ctx, tagId);
    
    [Command("Edit"), Description("Edit a tag."), RequirePermissions(DiscordPermissions.ManageMessages)]
    public async Task EditTag(SlashCommandContext ctx,
        [SlashAutoCompleteProvider(typeof(TagAutoCompleteProvider)), Description("Tag to edit.")] string tagId)
    => await EditTagCommand.ExecuteAsync(dbContextFactory, databaseMethodService, ctx, tagId);

    [Command("Send"), Description("Sends a tag")]
    public async Task SendTag(SlashCommandContext ctx,
        [SlashAutoCompleteProvider(typeof(TagAutoCompleteProvider)), Description("Tag to send.")] string tag,
        [Description("Target to send the tag to.")] DiscordUser? target = null)
        => await SendTagCommand.ExecuteAsync(dbContextFactory, ctx, tag, false, target);
    
    [Command("Preview"),Description("Previews a tag")]
    public async Task PreviewTag(SlashCommandContext ctx,
        [SlashAutoCompleteProvider(typeof(TagAutoCompleteProvider)), Description("Tag to send.")] string tagId)
    => await SendTagCommand.ExecuteAsync(dbContextFactory,ctx,tagId,true);
}