﻿using System.Text;
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

[SlashCommandGroup("Mod", "Moderator commands"),SlashRequirePermissions(Permissions.KickMembers), SlashRequireGuild]
public sealed class ModerationCommands : ApplicationCommandModule
{
    public IModeratorWarningService WarningService { private get; set; }
    public IDbContextFactory<LiveBotDbContext> DbContextFactory { private get; set; }
    public IModMailService ModMailService { private get; set; }
    public IModeratorLoggingService ModLogService { private get; set; }
    public IDatabaseMethodService DatabaseMethodService { private get; set; }

    [SlashCommand("warn", "Warn a user.")]
    public async Task Warning(InteractionContext ctx,
        [Option("user", "User to warn")] DiscordUser user,
        [Option("reason", "Why the user is being warned")]
        string reason,
        [Option("TimeOut", "How long the warning will last")]
        TimeOutOptions timeOut = 0,
        [Option("Image", "Image to attach to the warning")]
        DiscordAttachment? image = null)
    {
        await ctx.DeferAsync(true);
        WarningService.AddToQueue(new WarningItem(user, ctx.User, ctx.Guild, ctx.Channel, reason, false, ctx, image));
        if (timeOut == 0) return;
        DiscordMember member;
        try
        {
            member = await ctx.Guild.GetMemberAsync(user.Id);
        }
        catch (Exception)
        {
            ctx.Client.Logger.LogDebug("Could not find member {MemberId} in guild {GuildId}", user.Id, ctx.Guild.Id);
            return;
        }

        await member.TimeoutAsync(DateTimeOffset.Now + TimeSpan.FromSeconds((int)timeOut), "Timed out by warning");
    }

    public enum TimeOutOptions
    {
        [ChoiceName("none")] None = 0,
        [ChoiceName("60 secs")] SixtySecs = 60,
        [ChoiceName("5 min")] FiveMin = 300,
        [ChoiceName("10 min")] TenMin = 600,
        [ChoiceName("1 hour")] OneHour = 3600,
        [ChoiceName("1 day")] OneDay = 86400,
        [ChoiceName("1 week")] OneWeek = 604800
    }

    [SlashCommand("UnWarn", "Removes a warning from the user")]
    public async Task RemoveWarning(InteractionContext ctx,
        [Option("user", "User to remove the warning for")]
        DiscordUser user,
        [Autocomplete(typeof(RemoveWarningOptions))] [Option("Warning_ID", "The ID of a specific warning. Leave as is if don't want a specific one", true)]
        long warningId = -1)
    {
        await ctx.DeferAsync(true);
        await WarningService.RemoveWarningAsync(user, ctx, (int)warningId);
    }

    private sealed class RemoveWarningOptions : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var dbContextFactory = ctx.Services.GetService<IDbContextFactory<LiveBotDbContext>>();
            if (dbContextFactory is null) return [];
            await using LiveBotDbContext databaseContext = await dbContextFactory.CreateDbContextAsync();
            List<DiscordAutoCompleteChoice> result = [];
            var userId = (ulong)ctx.Options.First(x => x.Name == "user").Value;
            foreach (Infraction item in databaseContext.Infractions.Where(w => w.GuildId == ctx.Guild.Id && w.UserId == userId && w.InfractionType == InfractionType.Warning && w.IsActive))
            {
                result.Add(new DiscordAutoCompleteChoice($"#{item.Id} - {item.Reason}", item.Id));
            }

            return result;
        }
    }

    [SlashCommand("Prune", "Prune the message in the channel")]
    public async Task Prune(InteractionContext ctx,
        [Option("Message_Count", "The amount of messages to delete (1-100)")]
        long messageCount)
    {
        await ctx.DeferAsync(true);
        if (messageCount > 100)
        {
            messageCount = 100;
        }

        var messageList = ctx.Channel.GetMessagesAsync((int)messageCount);
        await ctx.Channel.DeleteMessagesAsync(messageList);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Selected messages have been pruned"));
    }
    
    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Prune", false)]
    public async Task PruneContextMenu(ContextMenuContext ctx)
    {
        await ctx.DeferAsync(true);
        if (ctx.TargetMessage.Timestamp.UtcDateTime < DateTime.UtcNow.AddDays(-14))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Message is older than 14 days, cannot prune"));
            return;
        }
        List<DiscordMessage> messages= [ctx.TargetMessage];
        var end = false;
        while (!end)
        {
            var temp = ctx.Channel.GetMessagesAfterAsync(messages.Last().Id)
                .ToBlockingEnumerable()
                .ToList();
            messages.AddRange(temp);
            if (temp.Count < 100)
            {
                end = true;
            }
        }
        
        await ctx.Channel.DeleteMessagesAsync(messages);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Selected messages have been pruned"));
    }
    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Prune User", false)]
    public async Task PruneUserContextMenu(ContextMenuContext ctx)
    {
        const int messageAgeLimit = 14;
        const int batchSize = 100;
        await ctx.DeferAsync(true);
        if (ctx.TargetMessage.Timestamp.UtcDateTime < DateTime.UtcNow.AddDays(-messageAgeLimit))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Message is older than 14 days, cannot prune"));
            return;
        }
        List<DiscordMessage> messages= [ctx.TargetMessage];
        while (true)
        {
            var messageCount = 0;
            await foreach (DiscordMessage message in ctx.Channel.GetMessagesAfterAsync(messages.Last().Id))
            {
                messageCount++;
                if (message.Author == ctx.TargetMessage.Author)
                {
                    messages.Add(message);
                }
            }
            if (messageCount < batchSize)
            {
                break;
            }
        }

        await ctx.Channel.DeleteMessagesAsync(messages);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Selected messages have been pruned"));
    }

    [SlashCommand("AddNote", "Adds a note in the database without warning the user")]
    public async Task AddNote(InteractionContext ctx,
        [Option("user", "User to who to add the note to")]
        DiscordUser user,
        [Option("Note", "Contents of the note.")]
        string note,
        [Option("Image", "Image to attach to the note")]
        DiscordAttachment? image = null)
    {
        await ctx.DeferAsync(true);
        await DatabaseMethodService.AddInfractionsAsync(new Infraction(ctx.User.Id, user.Id, ctx.Guild.Id, note, false, InfractionType.Note));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{ctx.User.Mention}, a note has been added to {user.Username}({user.Id})"));
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        Guild guild = await dbContext.Guilds.FindAsync(ctx.Guild.Id) ?? await DatabaseMethodService.AddGuildAsync(new Guild(ctx.Guild.Id));
        DiscordChannel channel = ctx.Guild.GetChannel(Convert.ToUInt64(guild.ModerationLogChannelId));
        ModLogService.AddToQueue(new ModLogItem(
            channel,
            user,
            "# Note Added\n" +
            $"- **User:** {user.Mention}\n" +
            $"- **Moderator:** {ctx.Member.Mention}\n" +
            $"- **Note:** {note}",
            ModLogType.Info,
            attachment: image));
    }

    [SlashCommand("EditNote", "Edits a not of a user, that you have added")]
    public async Task EditNote(InteractionContext ctx,
        [Option("User", "User who's note to edit")]
        DiscordUser user,
        [Option("Note_ID", "The ID of the note to edit"), Autocomplete(typeof(UserNotesOptions))]
        long noteId)
    {
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        Infraction? infraction = await dbContext.Infractions.FindAsync(noteId);
        if (infraction == null || infraction.UserId != user.Id || infraction.AdminDiscordId != ctx.User.Id || infraction.InfractionType != InfractionType.Note)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().WithContent("Could not find a note with that ID"));
            return;
        }

        string oldNote = infraction.Reason;
        var customId = $"EditNote-{ctx.User.Id}";
        DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder().WithTitle("Edit users note").WithCustomId(customId)
            .AddComponents(new TextInputComponent("Content", "Content", null, infraction.Reason, true, TextInputStyle.Paragraph));
        await ctx.CreateResponseAsync(InteractionResponseType.Modal, modal);

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var response = await interactivity.WaitForModalAsync(customId, ctx.User);
        if (response.TimedOut) return;
        infraction.Reason = response.Result.Values["Content"];
        dbContext.Infractions.Update(infraction);
        await dbContext.SaveChangesAsync();
        await response.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent($"Note `#{infraction.Id}` content changed\nFrom:`{oldNote}`\nTo:`{infraction.Reason}`").AsEphemeral());
        Guild? guild = await dbContext.Guilds.FindAsync(ctx.Guild.Id);
        if (guild is null) return;
        DiscordChannel channel = ctx.Guild.GetChannel(Convert.ToUInt64(guild.ModerationLogChannelId));
        ModLogService.AddToQueue(new ModLogItem(
            channel,
            user,
            "# Note Edited\n" +
            $"- **User:** {user.Mention}\n" +
            $"- **Moderator:** {ctx.Member.Mention}\n" +
            $"- **Old Note:** {oldNote}\n" +
            $"- **New Note:** {infraction.Reason}",
            ModLogType.Info));
    }

    [SlashCommand("DeleteNote", "Deletes a note of a user, that you have added")]
    public async Task DeleteNote(InteractionContext ctx,
        [Option("User", "User who's note to delete")]
        DiscordUser user,
        [Option("Note_ID", "The ID of the note to delete"), Autocomplete(typeof(UserNotesOptions))]
        long noteId)
    {
        await ctx.DeferAsync(true);
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        Infraction? infraction = await dbContext.Infractions.FindAsync(noteId);
        if (infraction == null || infraction.UserId != user.Id || infraction.AdminDiscordId != ctx.User.Id || infraction.InfractionType != InfractionType.Note)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Could not find a note with that ID"));
            return;
        }
        DiscordEmbedBuilder embed = new()
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                IconUrl = user.AvatarUrl,
                Name = user.Username
            },
            Title = $"Do you want to delete this note?",
            Description = $"- **Note:** {infraction.Reason}\n" +
                          $"- **Date:** <t:{infraction.TimeCreated.ToUnixTimeSeconds()}:f>"
        };
        DiscordWebhookBuilder responseBuilder = new DiscordWebhookBuilder()
            .AddEmbed(embed)
            .AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"yes", "Yes"),
                new DiscordButtonComponent(ButtonStyle.Danger, $"no", "No"));
        
        DiscordMessage message = await ctx.EditResponseAsync(responseBuilder);
        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var response = await interactivity.WaitForButtonAsync(message, ctx.Member, TimeSpan.FromSeconds(30));
        if (response.TimedOut) return;
        if (response.Result.Id == "no")
        {
            await response.Result.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Note not deleted")
                    .AsEphemeral()
            );
            return;
        }
        
        dbContext.Infractions.Remove(infraction);
        await dbContext.SaveChangesAsync();
        await response.Result.Interaction.CreateResponseAsync(
            InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"Note `{infraction.Id}` deleted")
                .AsEphemeral()
            );
        Guild? guild = await dbContext.Guilds.FindAsync(ctx.Guild.Id);
        if (guild is null) return;
        DiscordChannel channel = ctx.Guild.GetChannel(Convert.ToUInt64(guild.ModerationLogChannelId));
        ModLogService.AddToQueue(new ModLogItem(
            channel,
            user,
            "# Note Deleted\n" +
            $"- **User:** {user.Mention}\n" +
            $"- **Moderator:** {ctx.Member.Mention}\n" +
            $"- **Note:** {infraction.Reason}",
            ModLogType.Info));
    }
    
    private sealed class UserNotesOptions : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var dbContextFactory = ctx.Services.GetService<IDbContextFactory<LiveBotDbContext>>();
            if (dbContextFactory is null) return [];
            await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            var user = (ulong)ctx.Options.First(x => x.Name == "user").Value;
            var infractions = await dbContext.Infractions
                .Where(x => x.InfractionType == InfractionType.Note && x.AdminDiscordId == ctx.User.Id && x.UserId == user).ToListAsync();
            return infractions.Select(infraction => new DiscordAutoCompleteChoice($"#{infraction.Id} - {infraction.Reason}", infraction.Id)).ToList();
        }
    }

    private async Task SendUserInfractionsMessageAsync(BaseContext ctx, DiscordUser user, bool isModerator = true)
    {
        DiscordWebhookBuilder webhookBuilder = new();
        var embeds = await WarningService.BuildInfractionsEmbedsAsync(ctx.Guild, user, isModerator);
        webhookBuilder.AddEmbed(embeds[0]);
        if (embeds.Count > 1)
        {
            webhookBuilder.AddEmbed(embeds[1]);
        }

        if (embeds.Count <= 2)
        {
            await ctx.EditResponseAsync(webhookBuilder);
            return;
        }

        string leftButtonId = $"left_{ctx.User.Id}",
            rightButtonId = $"right_{ctx.User.Id}",
            stopButtonId = $"stop_{ctx.User.Id}";
        var currentPage = 1;
        DiscordButtonComponent leftButton = new(ButtonStyle.Primary, leftButtonId, "", true, new DiscordComponentEmoji("⬅️")),
            stopButton = new(ButtonStyle.Danger, stopButtonId, "", false, new DiscordComponentEmoji("⏹️")),
            rightButton = new(ButtonStyle.Primary, rightButtonId, "", false, new DiscordComponentEmoji("➡️"));

        webhookBuilder.AddComponents(leftButton, stopButton, rightButton);
        DiscordMessage message = await ctx.EditResponseAsync(webhookBuilder);

        while (true)
        {
            var result = await message.WaitForButtonAsync(ctx.User, TimeSpan.FromSeconds(30));
            if (result.TimedOut || result.Result.Id == stopButtonId)
            {
                webhookBuilder.ClearComponents();
                await ctx.EditResponseAsync(webhookBuilder);
                return;
            }

            webhookBuilder = new DiscordWebhookBuilder();
            if (result.Result.Id == leftButtonId)
            {
                currentPage--;
                if (currentPage == 1)
                {
                    leftButton.Disable();
                }

                if (rightButton.Disabled)
                {
                    rightButton.Enable();
                }

            }
            else if (result.Result.Id == rightButtonId)
            {
                currentPage++;
                if (currentPage == embeds.Count - 1)
                {
                    rightButton.Disable();
                }

                if (leftButton.Disabled)
                {
                    leftButton.Enable();
                }
            }

            webhookBuilder
                .AddEmbeds(new[] { embeds[0], embeds[currentPage] })
                .AddComponents(leftButton, stopButton, rightButton);
            await ctx.EditResponseAsync(webhookBuilder);
            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }
    }

    [SlashCommand("Infractions", "Shows the infractions of the user")]
    public async Task Infractions(InteractionContext ctx, [Option("user", "User to show the infractions for")] DiscordUser user)
    {
        await ctx.DeferAsync();
        await SendUserInfractionsMessageAsync(ctx, user);
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "Infractions", false)]
    public async Task InfractionsContextMenu(ContextMenuContext ctx)
    {
        await ctx.DeferAsync(true);
        await SendUserInfractionsMessageAsync(ctx, ctx.TargetUser);
    }

    [SlashCommand("FAQ", "Creates a new FAQ message")]
    public async Task Faq(InteractionContext ctx)
    {
        var customId = $"FAQ-{ctx.User.Id}";
        DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder().WithTitle("New FAQ entry").WithCustomId(customId)
            .AddComponents(new TextInputComponent("Question", "Question", null, null, true, TextInputStyle.Paragraph))
            .AddComponents(new TextInputComponent("Answer", "Answer", "Answer to the question", null, true, TextInputStyle.Paragraph));
        await ctx.CreateResponseAsync(InteractionResponseType.Modal, modal);

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var response = await interactivity.WaitForModalAsync(customId, ctx.User);
        if (!response.TimedOut)
        {
            await new DiscordMessageBuilder()
                .WithContent($"**Q: {response.Result.Values["Question"]}**\n *A: {response.Result.Values["Answer"].TrimEnd()}*")
                .SendAsync(ctx.Channel);
            await response.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("FAQ message created!").AsEphemeral());
        }
    }

    [SlashCommand("FAQ-Edit", "Edits an existing FAQ message, using the message ID")]
    public async Task FaqEdit(InteractionContext ctx, [Option("Message_ID", "The message ID to edit")] string messageId)
    {
        DiscordMessage message = await ctx.Channel.GetMessageAsync(Convert.ToUInt64(messageId));
        string ogMessage = message.Content.Replace("*", string.Empty);
        string question = ogMessage.Substring(ogMessage.IndexOf(":", StringComparison.Ordinal) + 1, ogMessage.Length - (ogMessage[ogMessage.IndexOf("\n", StringComparison.Ordinal)..].Length + 2))
            .TrimStart();
        string answer = ogMessage[(ogMessage.IndexOf("\n", StringComparison.Ordinal) + 4)..].TrimStart();

        var customId = $"FAQ-Editor-{ctx.User.Id}";
        DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder().WithTitle("FAQ Editor").WithCustomId(customId)
            .AddComponents(new TextInputComponent("Question", "Question", null, question, true, TextInputStyle.Paragraph))
            .AddComponents(new TextInputComponent("Answer", "Answer", null, answer, true, TextInputStyle.Paragraph));

        await ctx.CreateResponseAsync(InteractionResponseType.Modal, modal);

        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var response = await interactivity.WaitForModalAsync(customId, ctx.User);
        if (!response.TimedOut)
        {
            await message.ModifyAsync($"**Q: {response.Result.Values["Question"]}**\n *A: {response.Result.Values["Answer"].TrimEnd()}*");
            await response.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("FAQ message edited").AsEphemeral());
        }
    }

    [SlashCommand("info", "Shows general info about the user.")]
    public async Task Info(InteractionContext ctx, [Option("User", "User who to get the info about.")] DiscordUser user)
    {
        await ctx.DeferAsync();
        DiscordEmbed userInfoEmbed = await WarningService.GetUserInfoAsync(ctx.Guild, user);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(userInfoEmbed));
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "Info", false)]
    public async Task InfoContextMenu(ContextMenuContext ctx)
    {
        await ctx.DeferAsync(true);
        DiscordEmbed embed = await WarningService.GetUserInfoAsync(ctx.Guild, ctx.TargetUser);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [SlashCommand("Message", "Sends a message to specified user. Requires Mod Mail feature enabled.")]
    public async Task Message(InteractionContext ctx, [Option("User", "Specify the user who to mention")] DiscordUser user, [Option("Message", "Message to send to the user.")] string message)
    {
        await ctx.DeferAsync(true);
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        Guild guildSettings = await dbContext.Guilds.FindAsync(ctx.Guild.Id) ?? await DatabaseMethodService.AddGuildAsync(new Guild(ctx.Guild.Id));
        if (guildSettings.ModMailChannelId == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The Mod Mail feature has not been enabled in this server. Contact an Admin to resolve the issue."));
            return;
        }

        DiscordMember member;
        try
        {
            member = await ctx.Guild.GetMemberAsync(user.Id);
        }
        catch (DSharpPlus.Exceptions.NotFoundException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The user is not in the server, can't message."));
            return;
        }

        var dmMessage = $"You are receiving a Moderator DM from **{ctx.Guild.Name}** Discord\n{ctx.User.Username} - {message}";
        DiscordMessageBuilder messageBuilder = new();
        messageBuilder.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{ModMailService.OpenButtonPrefix}{ctx.Guild.Id}", "Open Mod Mail"));
        messageBuilder.WithContent(dmMessage);

        await member.SendMessageAsync(messageBuilder);

        DiscordChannel modMailChannel = ctx.Guild.GetChannel(guildSettings.ModMailChannelId.Value);
        DiscordEmbedBuilder embed = new()
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                IconUrl = member.AvatarUrl,
                Name = member.Username
            },
            Title = $"[MOD DM] Moderator DM to {member.Username}",
            Description = dmMessage
        };
        await modMailChannel.SendMessageAsync(embed: embed);
        ctx.Client.Logger.LogInformation(CustomLogEvents.ModMail, "A Direct message was sent to {Username}({UserId}) from {User2Name}({User2Id}) through Mod Mail system", member.Username, member.Id,
            ctx.Member.Username, ctx.Member.Id);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Message delivered to user. Check Mod Mail channel for logs."));
    }

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Add Button", false)]
    public async Task AddButton(ContextMenuContext ctx)
    {
        if (ctx.TargetMessage.Author != ctx.Client.CurrentUser)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("To add a button, the bot must be the author of the message. Try again").AsEphemeral());
            return;
        }

        var customId = $"AddButton-{ctx.TargetMessage.Id}-{ctx.User.Id}";
        DiscordInteractionResponseBuilder response = new()
        {
            Title = "Button Parameters",
            CustomId = customId
        };
        response.AddComponents(new TextInputComponent("Custom ID", "customId"));
        response.AddComponents(new TextInputComponent("Label", "label"));
        response.AddComponents(new TextInputComponent("Emoji", "emoji", required: false));

        await ctx.CreateResponseAsync(InteractionResponseType.Modal, response);
        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var modalResponse = await interactivity.WaitForModalAsync(customId, ctx.User);

        if (modalResponse.TimedOut) return;

        DiscordMessageBuilder modified = new DiscordMessageBuilder()
            .WithContent(ctx.TargetMessage.Content)
            .AddEmbeds(ctx.TargetMessage.Embeds);

        DiscordComponentEmoji? emoji = null;
        if (modalResponse.Result.Values["emoji"] != string.Empty)
        {
            emoji = ulong.TryParse(modalResponse.Result.Values["emoji"], out ulong emojiId) ? new DiscordComponentEmoji(emojiId) : new DiscordComponentEmoji(modalResponse.Result.Values["emoji"]);
        }

        if (ctx.TargetMessage.Components.Count == 0)
        {
            modified.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, modalResponse.Result.Values["customId"], modalResponse.Result.Values["label"], emoji: emoji));
        }

        foreach (DiscordActionRowComponent row in ctx.TargetMessage.Components)
        {
            if (row.Components.Count == 5)
            {
                modified.AddComponents(row);
            }
            else
            {
                var buttons = row.Components.ToList();
                buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, modalResponse.Result.Values["customId"], modalResponse.Result.Values["label"], emoji: emoji));
                modified.AddComponents(buttons);
            }
        }

        await ctx.TargetMessage.ModifyAsync(modified);
        await modalResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent($"Button added to the message. **Custom ID:** {modalResponse.Result.Values["customId"]}").AsEphemeral());
    }

    [SlashCommand("Stats", "Displays moderator stats for the server.")]
    public async Task Stats(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        var leaderboard = await dbContext.Infractions.Where(x => x.GuildId == ctx.Guild.Id).Select(x => new { UserId = x.AdminDiscordId, Type = x.InfractionType }).ToListAsync();
        var groupedLeaderboard = leaderboard
            .GroupBy(x => x.UserId)
            .Select(x => new
            {
                UserId = x.Key,
                Kicks = x.Count(y => y.Type == InfractionType.Kick),
                Bans = x.Count(y => y.Type == InfractionType.Ban),
                Warnings = x.Count(y => y.Type == InfractionType.Warning)
            })
            .OrderByDescending(x => x.Warnings)
            .ThenByDescending(x => x.Kicks)
            .ThenByDescending(x => x.Bans)
            .ToList();
        StringBuilder leaderboardBuilder = new();
        leaderboardBuilder.AppendLine("```");
        leaderboardBuilder.AppendLine("User".PadRight(30) + "Warnings".PadRight(10) + "Kicks".PadRight(10) + "Bans".PadRight(10));
        foreach (var user in groupedLeaderboard)
        {
            DiscordUser discordUser = await ctx.Client.GetUserAsync(user.UserId);
            leaderboardBuilder.AppendLine($"{discordUser.Username}#{discordUser.Discriminator}".PadRight(30) + $"{user.Warnings}".PadRight(10) + $"{user.Kicks}".PadRight(10) +
                                          $"{user.Bans}".PadRight(10));
        }

        leaderboardBuilder.AppendLine("```");
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(leaderboardBuilder.ToString()));
    }

    [SlashCommand("WhitelistInvite", "Whitelists an invite link code")]
    public async Task WhitelistInvite(InteractionContext ctx, [Option("Code", "The invite code to be whitelisted")] string code)
    {
        await ctx.DeferAsync(true);
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        Guild? guild = await dbContext.Guilds.Include(x=>x.WhitelistedVanities).FirstOrDefaultAsync(x=>x.Id==ctx.Guild.Id);
        if (guild is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This guild is not in the database"));
            return;
        }

        if (guild.WhitelistedVanities is not null && guild.WhitelistedVanities.Any(x=>x.VanityCode==code))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This code is already whitelisted"));
            return;
        }
        await dbContext.VanityWhitelist.AddAsync(new VanityWhitelist
        {
            GuildId = ctx.Guild.Id,
            VanityCode = code
        });
        await dbContext.SaveChangesAsync();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Invite code {code} has been whitelisted"));
        ctx.Client.Logger.LogInformation(CustomLogEvents.InviteLinkFilter, "Invite code {Code} has been whitelisted in {GuildName}({GuildId}) by {Username}({UserId})",
            code, ctx.Guild.Name, ctx.Guild.Id, ctx.User.Username, ctx.User.Id);
    }
    
}