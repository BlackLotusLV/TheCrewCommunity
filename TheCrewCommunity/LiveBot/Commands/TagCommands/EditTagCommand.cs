using System.Collections.Immutable;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.TagCommands;

public static class EditTagCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService methodService, InteractivityExtension interactivity, SlashCommandContext ctx, string tagId)
    {
        if (ctx.Guild is null)
        {
            await ctx.RespondAsync("This command can only be used in a guild channel");
            return;
        }
        ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} started editing a tag", ctx.User.Id, ctx.Guild.Id);
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild? guild = await liveBotDbContext.Guilds.Include(x=>x.Tags).FirstOrDefaultAsync(x=>x.Id == ctx.Guild.Id);
        if (guild is null)
        {
            await ctx.RespondAsync("Guild not found");
            await methodService.AddGuildAsync(new Guild(ctx.Guild.Id));
            return;
        }
        if (guild.Tags is null || guild.Tags.Count == 0)
        {
            await ctx.RespondAsync("No tags found");
            return;
        }
        var tags = guild.Tags.Where(x=>x.GuildId == ctx.Guild.Id).ToImmutableList();
        Tag? tag = tags.FirstOrDefault(x => x.Id == Guid.Parse(tagId));
        if (tag is null)
        {
            await ctx.RespondAsync("Tag not found");
            ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} tried to edit a tag but it was not found", ctx.User.Id, ctx.Guild.Id);
            return;
        }

        DiscordInteractionResponseBuilder responseBuilder = new();

        const string modalId = "tag_edit";
        responseBuilder
            .WithTitle("Edit Tag")
            .WithCustomId(modalId)
            .AddTextInputComponent(new DiscordTextInputComponent("Name", "name", value: tag.Name, min_length: 1))
            .AddTextInputComponent(new DiscordTextInputComponent("Content", "content", value: tag.Content, min_length: 1, style: DiscordTextInputStyle.Paragraph));

        await ctx.RespondAsync(responseBuilder);
        
        var result = await interactivity.WaitForModalAsync(modalId,ctx.User);
        if (result.TimedOut)
        {
            ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} timed out while editing a tag", ctx.User.Id, ctx.Guild.Id);
            return;
        }
        await result.Result.Interaction.DeferAsync(true);
        string name = result.Result.Values["name"];
        string content = result.Result.Values["content"];

        if (tags.All(x => x.Name != name))
        {
            tag.Name = name;
        }
        tag.Content = content;
        liveBotDbContext.Tags.Update(tag);
        await liveBotDbContext.SaveChangesAsync();
        await result.Result.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Tag {tag.Name} edited"));
        ctx.Client.Logger.LogInformation(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} edited tag named {Tag}", ctx.User.Id, ctx.Guild.Id, tag.Name);
    }
}