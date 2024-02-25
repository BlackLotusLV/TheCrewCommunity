using DSharpPlus.Commands.Processors.SlashCommands;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.TagCommands;

public static class DeleteTagCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory, SlashCommandContext ctx,string tagId)
    {
        await ctx.DeferResponseAsync(true);
        ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} started deleting a tag", ctx.User.Id, ctx.Guild.Id);
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Tag? tag = await liveBotDbContext.Tags.FindAsync(tagId) ?? null;
        if (tag is null)
        {
            await ctx.EditResponseAsync("Tag not found");
            ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} tried to delete a tag but it was not found", ctx.User.Id, ctx.Guild.Id);
            return;
        }
        liveBotDbContext.Tags.Remove(tag);
        await liveBotDbContext.SaveChangesAsync();
        await ctx.EditResponseAsync($"Tag {tag.Name} deleted");
        ctx.Client.Logger.LogInformation(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} deleted tag named {Tag}", ctx.User.Id, ctx.Guild.Id, tag.Name);
    }
}