using System.Collections.Immutable;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands;

[SlashCommandGroup("Tag","Tag commands"), SlashRequireGuild]
public sealed class TagCommands : ApplicationCommandModule
{
    public IDbContextFactory<LiveBotDbContext> dbContextFactory { private get; set; }
    public IDatabaseMethodService databaseMethodService { private get; set; }
    
    [SlashCommand("Create", "Creates a tag"), SlashRequirePermissions(Permissions.ManageMessages)]
    public async Task CreateTag(InteractionContext ctx,
        [Option("Name", "Name of the tag that will be used to select it")]
        string name)
    {
        ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} started making a tag", ctx.User.Id, ctx.Guild.Id);
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        var tags = await liveBotDbContext.Tags.Where(x=>x.GuildId == ctx.Guild.Id).ToListAsync();
        if (tags.Any(x=>x.Name== name))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Tag `{name}` already exists in this server"));
            ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} tried to create a tag named {Tag} but it already exists", ctx.User.Id, ctx.Guild.Id, name);
            return;
        }
        
        const string modalId = $"tag_create";
        DiscordInteractionResponseBuilder responseBuilder = new();
        responseBuilder
            .WithTitle($"Create tag Named {name}")
            .WithCustomId(modalId)
            .AddComponents(new TextInputComponent("Content", "content","Content of the tag", min_length: 1, style: TextInputStyle.Paragraph));

        await ctx.CreateResponseAsync(InteractionResponseType.Modal, responseBuilder);
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
    
    [SlashCommand("Delete", "Deletes a tag"),
     SlashRequirePermissions(Permissions.ManageMessages)]
    public async Task DeleteTag(InteractionContext ctx,
        [Autocomplete(typeof(TagOptions)), Option("Tag", "Tag to delete.")]
        string tagId)
    {
        await ctx.DeferAsync(true);
        ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} started deleting a tag", ctx.User.Id, ctx.Guild.Id);
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Tag? tag = await GetTagAsync(Guid.Parse(tagId));
        if (tag is null)
        {
            await EditResponseAsync(ctx, "Tag not found");
            ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} tried to delete a tag but it was not found", ctx.User.Id, ctx.Guild.Id);
            return;
        }
        liveBotDbContext.Tags.Remove(tag);
        await liveBotDbContext.SaveChangesAsync();
        await EditResponseAsync(ctx, $"Tag {tag.Name} deleted");
        ctx.Client.Logger.LogInformation(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} deleted tag named {Tag}", ctx.User.Id, ctx.Guild.Id, tag.Name);
    }

    [SlashCommand("Edit", "Edits a tag"),
     SlashRequirePermissions(Permissions.ManageMessages)]
    public async Task EditTag(InteractionContext ctx,
        [Autocomplete(typeof(TagOptions)), Option("Tag", "Tag to edit.")]
        string tagId)
    {
        ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} started editing a tag", ctx.User.Id, ctx.Guild.Id);
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild? guild = await liveBotDbContext.Guilds.Include(x=>x.Tags).FirstOrDefaultAsync(x=>x.Id == ctx.Guild.Id);
        if (guild is null)
        {
            await EditResponseAsync(ctx, "Guild not found");
            await databaseMethodService.AddGuildAsync(new Guild(ctx.Guild.Id));
            return;
        }
        if (guild.Tags is null || guild.Tags.Count == 0)
        {
            await EditResponseAsync(ctx, "No tags found");
            return;
        }
        var tags = guild.Tags.Where(x=>x.GuildId == ctx.Guild.Id).ToImmutableList();
        Tag? tag = tags.FirstOrDefault(x => x.Id == Guid.Parse(tagId));
        if (tag is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Tag not found"));
            await EditResponseAsync(ctx, "Tag not found");
            ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} tried to edit a tag but it was not found", ctx.User.Id, ctx.Guild.Id);
            return;
        }

        DiscordInteractionResponseBuilder responseBuilder = new();

        const string modalId = $"tag_edit";
        responseBuilder
            .WithTitle("Edit Tag")
            .WithCustomId(modalId)
            .AddComponents(new TextInputComponent("Name", "name", value: tag.Name, min_length: 1))
            .AddComponents(new TextInputComponent("Content", "content", value: tag.Content, min_length: 1, style: TextInputStyle.Paragraph));

        await ctx.CreateResponseAsync(InteractionResponseType.Modal, responseBuilder);
        
        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
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

    [SlashCommand("Send", "Sends a tag")]
    public async Task SendTag(InteractionContext ctx,
        [Autocomplete(typeof(TagOptions)), Option("Tag", "Tag to send.")]
        string tagId,
        [Option("Target", "Target to send the tag to.")] DiscordUser? target = null)
    {
        await SendTagAsync(ctx, tagId, false, target);
    }

    [SlashCommand("Preview", "Previews a tag"),
     SlashRequireGuild]
    public async Task PreviewTag(InteractionContext ctx,
        [Autocomplete(typeof(TagOptions)), Option("Tag", "Tag to preview.")]
        string tagId)
    {
        await SendTagAsync(ctx, tagId, true);
    }

    private async Task SendTagAsync(BaseContext ctx, string tagId, bool isEphemeral, DiscordUser? target = null)
    {
        ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} started sending a tag", ctx.User.Id, ctx.Guild.Id);
        Tag? tag = await GetTagAsync(Guid.Parse(tagId));
        if (tag is null)
        {
            await EditResponseAsync(ctx, "Tag not found");
            ctx.Client.Logger.LogDebug(CustomLogEvents.TagCommand, "User {User} in Guild {Guild} tried to send a tag but it was not found", ctx.User.Id, ctx.Guild.Id);
            return;
        }

        DiscordInteractionResponseBuilder interactionBuilder = new DiscordInteractionResponseBuilder()
            .WithContent($"{(target is not null ? $"{target.Mention},\n" : "")}{tag.Content}")
            .AddMention(new UserMention())
            .AsEphemeral(isEphemeral);

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, interactionBuilder);
    }
    
    private async Task<Tag?> GetTagAsync(Guid tagId)
    {
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        return await liveBotDbContext.Tags.FindAsync(tagId) ?? null;
    }
    
    private static async Task EditResponseAsync(BaseContext ctx, string content)
    {
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(content));
    }

    private sealed class TagOptions : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var dbContextFactory = ctx.Services.GetService<IDbContextFactory<LiveBotDbContext>>();
            await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
            Guild guild = liveBotDbContext.Guilds.Include(x=>x.Tags).First(x=>x.Id == ctx.Guild.Id);
            var tags = guild.Tags.Where(x=>x.GuildId == ctx.Guild.Id).ToImmutableList();
            var result = tags.Select(tag => new DiscordAutoCompleteChoice($"{tag.Name} ({(tag.Content.Length > 50 ? tag.Content[..50] + "..." : tag.Content)})",tag.Id.ToString())).ToList();
            return result;
        }
    }
}