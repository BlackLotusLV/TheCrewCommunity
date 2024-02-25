using DSharpPlus;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.TagCommands;

public static class CreateTagCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory,SlashCommandContext ctx, string name)
    {
        ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} started making a tag", ctx.User.Id, ctx.Guild.Id);
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        var tags = await liveBotDbContext.Tags.Where(x=>x.GuildId == ctx.Guild.Id).ToListAsync();
        if (tags.Any(x=>x.Name== name))
        {
            await ctx.RespondAsync($"Tag `{name}` already exists in this server");
            ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} tried to create a tag named {Tag} but it already exists", ctx.User.Id, ctx.Guild.Id, name);
            return;
        }
        
        const string modalId = $"tag_create";
        DiscordInteractionResponseBuilder responseBuilder = new();
        responseBuilder
            .WithTitle($"Create tag Named {name}")
            .WithCustomId(modalId)
            .AddComponents(new TextInputComponent("Content", "content","Content of the tag", min_length: 1, style: TextInputStyle.Paragraph));
        await ctx.Interaction.CreateResponseAsync(InteractionResponseType.Modal, responseBuilder);

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var modalInteractivity = await interactivity.WaitForModalAsync(modalId,ctx.User);
        if (modalInteractivity.TimedOut)
        {
            ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} timed out while editing a tag", ctx.User.Id, ctx.Guild.Id);
            return;
        }
        await modalInteractivity.Result.Interaction.DeferAsync(true);
        string content = modalInteractivity.Result.Values["content"];
        
        Tag tag = new()
        {
            Name = name,
            Content = content,
            GuildId = ctx.Guild.Id,
            OwnerId = ctx.User.Id
        };
        await liveBotDbContext.Tags.AddAsync(tag);
        await liveBotDbContext.SaveChangesAsync();
        await modalInteractivity.Result.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Tag `{name}` created"));
        ctx.Client.Logger.LogInformation(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} created tag named {Tag}", ctx.User.Id, ctx.Guild.Id, tag.Name);
    }
}