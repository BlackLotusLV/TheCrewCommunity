using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.TagCommands;

public class SendTagCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory, SlashCommandContext ctx, string tagId, bool isEphemeral, DiscordUser? target = null)
    {
        if (ctx.Guild is null) return;
        ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} started sending a tag", ctx.User.Id, ctx.Guild.Id);
        LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Tag? tag = await liveBotDbContext.Tags.FindAsync(Guid.Parse(tagId));
        if (tag is null)
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Tag not found").AsEphemeral(isEphemeral));
            ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} tried to send a tag but it was not found", ctx.User.Id, ctx.Guild.Id);
            return;
        }

        DiscordInteractionResponseBuilder interactionBuilder = new DiscordInteractionResponseBuilder()
            .WithContent($"{(target is not null ? $"{target.Mention},\n" : "")}{tag.Content}")
            .AddMention(new UserMention())
            .AsEphemeral(isEphemeral);

        await ctx.RespondAsync(interactionBuilder);
    }
}