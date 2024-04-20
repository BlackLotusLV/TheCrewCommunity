using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class OpenModMailCommand(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService, IModMailService modMailService)
{
    [Command("send-modmail"), Description("Opens a moderator mail in your DMs, to talk to the mod team.")]
    public async Task ExecuteAsync(SlashCommandContext ctx, [Description("Short description of the issue")] string subject = "*Subject left blank*")
    {
        await ctx.DeferResponseAsync(true);
        if (ctx.Guild is null || ctx.Member is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This command requires to be executed in the server you wish to contact."));
            return;
        }
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

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
        await databaseMethodService.AddModMailAsync(newEntry);

        DiscordButtonComponent closeButton = new(DiscordButtonStyle.Danger, $"{modMailService.CloseButtonPrefix}{newEntry.Id}", "Close", false, new DiscordComponentEmoji("✖️"));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Mod Mail #{newEntry.Id} opened, please head over to your Direct Messages with Live Bot to chat to the moderator team!"));

        await ctx.Member.SendMessageAsync(new DiscordMessageBuilder().AddComponents(closeButton).WithContent($"**----------------------------------------------------**\n" +
                                                                                                             $"Mod mail entry **open** with `{ctx.Guild.Name}`. Continue to write as you would normally ;)\n*Mod Mail will time out in {modMailService.TimeoutMinutes} minutes after last message is sent.*\n" +
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
}