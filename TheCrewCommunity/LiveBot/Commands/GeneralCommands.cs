using System.Collections.Immutable;
using System.Text;
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

public sealed class GeneralCommands : ApplicationCommandModule
{
    public IModMailService ModMailService { private get; set; }
    public IDbContextFactory<LiveBotDbContext> DbContextFactory { private get; set; }
    public IDatabaseMethodService DatabaseMethodService { private get; set; }

    [SlashCommand("LiveBot-info", "Information about live bot")]
    public async Task LiveBotInfo(InteractionContext ctx)
    {
        const string changelog = "- Adjusted formatting for mod logs\n" +
                                 "- Mod logs now hook in to AuditLogs for accurate and more data";
        DiscordUser user = ctx.Client.CurrentUser;
        DiscordEmbedBuilder embed = new()
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                IconUrl = user.AvatarUrl,
                Name = user.Username
            }
        };
        embed.AddField("Version:", "test", true);

        embed.AddField("Programmed in:", "C#", true);
        embed.AddField("Programmed by:", "<@86725763428028416>", true);
        embed.AddField("LiveBot info", "General purpose bot with a level system, stream notifications, greeting people and various other functions related to The Crew franchise");
        embed.AddField("Change log:", changelog);
        await ctx.CreateResponseAsync(embed: embed);
    }

    [SlashCommand("Send-ModMail", "Creates a new ModMailChannel")]
    public async Task ModMail(InteractionContext ctx, [Option("subject", "Short Description of the issue")] string subject = "*Subject left blank*")
    {
        await ctx.DeferAsync(true);
        if (ctx.Guild is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This command requires to be executed in the server you wish to contact."));
            return;
        }
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();

        GuildUser? userRanks = await dbContext.GuildUsers.FirstOrDefaultAsync(w => w.GuildId == ctx.Guild.Id && w.UserDiscordId == ctx.User.Id);
        if (userRanks == null || userRanks.IsModMailBlocked)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are blocked from using the Mod Mail feature in this server."));
            return;
        }

        Guild? guild = await dbContext.Guilds.FirstOrDefaultAsync(w => w.Id == ctx.Guild.Id);

        if (guild?.ModMailChannelId == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The Mod Mail feature has not been set up in this server. Can't open ModMail."));
            return;
        }

        if (await dbContext.ModMail.AnyAsync(w => w.UserDiscordId == ctx.User.Id && w.IsActive))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You already have an existing Mod Mail open, please close it before starting a new one."));
            return;
        }

        Random r = new();
        var colorId = $"#{r.Next(0x1000000):X6}";
        ModMail newEntry = new(ctx.Guild.Id, ctx.User.Id, DateTime.UtcNow, colorId);
        await DatabaseMethodService.AddModMailAsync(newEntry);

        DiscordButtonComponent closeButton = new(ButtonStyle.Danger, $"{ModMailService.CloseButtonPrefix}{newEntry.Id}", "Close", false, new DiscordComponentEmoji("✖️"));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Mod Mail #{newEntry.Id} opened, please head over to your Direct Messages with Live Bot to chat to the moderator team!"));

        await ctx.Member.SendMessageAsync(new DiscordMessageBuilder().AddComponents(closeButton).WithContent($"**----------------------------------------------------**\n" +
                                                                                                             $"Mod mail entry **open** with `{ctx.Guild.Name}`. Continue to write as you would normally ;)\n*Mod Mail will time out in {ModMailService.TimeoutMinutes} minutes after last message is sent.*\n" +
                                                                                                             $"**Subject: {subject}**"));

        DiscordEmbedBuilder embed = new()
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                Name = $"{ctx.User.Username} ({ctx.User.Id})",
                IconUrl = ctx.User.AvatarUrl
            },
            Title = $"[NEW] #{newEntry.Id} Mod Mail created by {ctx.User.Username}.",
            Color = new DiscordColor(colorId),
            Description = subject
        };

        DiscordChannel modMailChannel = ctx.Guild.GetChannel(guild.ModMailChannelId.Value);
        await new DiscordMessageBuilder()
            .AddComponents(closeButton)
            .AddEmbed(embed)
            .SendAsync(modMailChannel);
    }

    [SlashRequireGuild]
    [SlashCommand("RoleTag", "Pings a role under specific criteria.")]
    public async Task RoleTag(InteractionContext ctx, [Autocomplete(typeof(RoleTagOptions))] [Option("Role", "Which role to tag")] long id)
    {
        await ctx.DeferAsync(true);
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        Guild guild = await dbContext.Guilds.Include(x => x.RoleTagSettings).FirstOrDefaultAsync(x => x.Id == ctx.Guild.Id) ?? await DatabaseMethodService.AddGuildAsync(new Guild(ctx.Guild.Id));
        if (guild.RoleTagSettings is null || guild.RoleTagSettings.Count == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("There are no roles to tag in this server."));
            return;
        }

        RoleTagSettings? roleTagSettings = guild.RoleTagSettings.FirstOrDefault(x=>x.Id==id);
        
        if (roleTagSettings == null || roleTagSettings.GuildId != ctx.Guild.Id || roleTagSettings.ChannelId is not null && roleTagSettings.ChannelId != ctx.Channel.Id)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The role you tried to select does not exist or can't be tagged in this channel."));
            return;
        }

        if (roleTagSettings.LastTimeUsed > DateTime.UtcNow - TimeSpan.FromMinutes(roleTagSettings.Cooldown))
        {
            TimeSpan remainingTime = TimeSpan.FromMinutes(roleTagSettings.Cooldown) - (DateTime.UtcNow - roleTagSettings.LastTimeUsed);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                $"This role can't be mentioned right now, cooldown has not passed yet. ({remainingTime.Hours} Hours {remainingTime.Minutes} Minutes {remainingTime.Seconds} Seconds left)"));
            return;
        }

        DiscordRole role = ctx.Guild.GetRole(roleTagSettings.RoleId);

        await new DiscordMessageBuilder()
            .WithContent($"{role.Mention} - {ctx.Member.Mention}: {roleTagSettings.Message}")
            .WithAllowedMention(new RoleMention(role))
            .SendAsync(ctx.Channel);

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Role Tagged"));
        roleTagSettings.LastTimeUsed = DateTime.UtcNow;

        dbContext.RoleTagSettings.Update(roleTagSettings);
        await dbContext.SaveChangesAsync();
    }

    private sealed class RoleTagOptions : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var dbContextFactory = ctx.Services.GetService<IDbContextFactory<LiveBotDbContext>>();
            if (dbContextFactory == null) return [];
            await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
            List<DiscordAutoCompleteChoice> result = [];
            foreach (RoleTagSettings item in liveBotDbContext.RoleTagSettings.Where(w => w.GuildId == ctx.Guild.Id && (w.ChannelId == ctx.Channel.Id || w.ChannelId == null)))
            {
                result.Add(new DiscordAutoCompleteChoice($"{(item.LastTimeUsed > DateTime.UtcNow - TimeSpan.FromMinutes(item.Cooldown) ? "(On cooldown) " : "")}{item.Description}", item.Id));
            }
            return result;
        }
    }

    [SlashRequireGuild]
    [SlashCommand("Rank", "Shows your server rank without the leaderboard.")]
    public async Task Rank(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        var activityList = await dbContext.UserActivity
            .Where(x => x.Date > DateTime.UtcNow.AddDays(-30) && x.GuildId == ctx.Guild.Id)
            .GroupBy(x => x.UserDiscordId)
            .Select(g => new { UserID = g.Key, Points = g.Sum(x => x.Points) })
            .OrderByDescending(x => x.Points)
            .ToListAsync();
        User? userInfo = await dbContext.Users.FindAsync(ctx.User.Id);
        if (userInfo == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Could not find your rank in the database"));
            await DatabaseMethodService.AddUserAsync(new User(ctx.User.Id));
            return;
        }

        var rank = 0;
        foreach (var item in activityList)
        {
            rank++;
            if (item.UserID != ctx.User.Id) continue;
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                $"You are ranked **#{rank}** in {ctx.Guild.Name} server with **{item.Points}** points. Your cookie stats are: {userInfo.CookiesTaken} Received /  {userInfo.CookiesGiven} Given"));
            break;
        }
    }

    [SlashRequireGuild,
     SlashCommand("submit-photo", "Submit a photo to the photo competition.")]
    public async Task SubmitPhoto(InteractionContext ctx,
        [Autocomplete(typeof(GeneralUtils.PhotoContestOption)), Option("Competition", "To which competition to submit to.")]
        long competitionId,
        [Option("Photo", "The photo to submit.")]
        DiscordAttachment image)
    {
        await ctx.DeferAsync(true);
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        Guild guild = await dbContext.Guilds
            .Include(x => x.PhotoCompSettings)
            .ThenInclude(x => x.Entries)
            .FirstOrDefaultAsync(x => x.Id == ctx.Guild.Id);
        PhotoCompSettings competitionSettings = guild.PhotoCompSettings.FirstOrDefault(x => x.Id == competitionId);
        if (competitionSettings is null || competitionSettings.IsOpen is false)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The competition is not open. Or you have provided an invalid competition ID. Please try again."));
            return;
        }

        if (competitionSettings.MaxEntries != 0 && competitionSettings.Entries.Count(x => x.UserId == ctx.User.Id) >= competitionSettings.MaxEntries)
        {
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent($"You have reached the maximum amount of entries for this competition. You can submit a maximum of {competitionSettings.MaxEntries} entries."));
            return;
        }

        var excludedParameters = guild.PhotoCompSettings
            .Where(x => x.IsOpen && x.Entries.Any(entry => entry.UserId == ctx.User.Id))
            .Select(x => x.CustomParameter).ToImmutableArray();
        if (excludedParameters.Any(customParameter => customParameter == competitionSettings.CustomParameter))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You have already submitted to this competition. Please try again."));
            return;
        }

        DiscordChannel dumpChannel = ctx.Guild.GetChannel(competitionSettings.DumpChannelId);
        DiscordMessageBuilder messageBuilder = new();
        DiscordEmbedBuilder embedBuilder = new()
        {
            Description = $"# 📷 {ctx.User.Username} submitted a photo\n" +
                          $"- User: {ctx.User.Mention}({ctx.User.Id})\n" +
                          $"- Competition: {competitionSettings.CustomName}\n" +
                          $"- Date: <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:F>"
        };

        using HttpClient client = new();
        HttpResponseMessage response = await client.GetAsync(image.Url);
        string fileName = Guid.NewGuid() + image.FileName;
        List<string> imageExtensions = [".png", ".jpg", ".jpeg"];
        if (!imageExtensions.Contains(Path.GetExtension(fileName)))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Unsupported file format, make sure your image is of type .png, .jpg or .jpeg"));
            return;
        }

        if (response.IsSuccessStatusCode)
        {
            var fileStream = new MemoryStream();
            await response.Content.CopyToAsync(fileStream);
            fileStream.Position = 0;
            messageBuilder.AddFile(fileName, fileStream);
            embedBuilder.ImageUrl = $"attachment://{fileName}";
            messageBuilder.AddEmbed(embedBuilder);
        }
        else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Failed to download image. Please try again."));
            return;
        }

        DiscordMessage message = await messageBuilder.SendAsync(dumpChannel);
        message = await dumpChannel.GetMessageAsync(message.Id);
        await DatabaseMethodService.AddPhotoCompEntryAsync(new PhotoCompEntries(ctx.User.Id, competitionSettings.Id,
            message.Embeds[0].Image.Url.ToString(), DateTime.UtcNow));
        await ctx.EditResponseAsync(
            new DiscordWebhookBuilder().WithContent(
                $"Photo submitted to \"{competitionSettings.CustomName}\" competition."));
    }

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Report Message")]
    public async Task ReportMessage(ContextMenuContext ctx)
    {
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        Guild? guild = await dbContext.Guilds.FindAsync(ctx.Guild.Id);
        if (guild?.UserReportsChannelId is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This server is not set up for reporting messages."));
            await ctx.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("This server is not set up for reporting messages.")
                    .AsEphemeral()
                );
            return;
        }
        
        DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder()
            .WithTitle("Report Message")
            .WithCustomId("report_message")
            .AddComponents(new TextInputComponent("Complaint","Complaint","What is your complaint?",null,true,TextInputStyle.Paragraph)
            );
        await ctx.CreateResponseAsync(InteractionResponseType.Modal, modal);
        
        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var response = await interactivity.WaitForModalAsync(modal.CustomId, ctx.User);
        if (response.TimedOut) return;

        DiscordEmbedBuilder reportEmbed = new DiscordEmbedBuilder()
            .WithTitle("Message reported")
            .WithDescription($"# Contents:\n`{ctx.TargetMessage.Content}`")
            .WithAuthor($"{ctx.User.Username}({ctx.User.Id})", null, ctx.User.AvatarUrl);

        var raiseHandButton = new DiscordButtonComponent(ButtonStyle.Primary, $"raiseHand-report-{ctx.TargetMessage.ChannelId}-{ctx.TargetMessage.Id}", "Raise Hand", false, new DiscordComponentEmoji("✋"));
        
        DiscordMessageBuilder reportMessage = new DiscordMessageBuilder()
            .AddEmbed(reportEmbed)
            .AddComponents(raiseHandButton);
            
        DiscordChannel reportChannel = ctx.Guild.GetChannel(guild.UserReportsChannelId.Value);
        await reportChannel.SendMessageAsync(reportMessage);
        await response.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Report sent. A Moderator will review it soon. *If actions are taken, you wil NOT be informed*").AsEphemeral());
    }
}