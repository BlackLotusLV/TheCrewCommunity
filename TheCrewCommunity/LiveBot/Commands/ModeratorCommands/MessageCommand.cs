using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public static class MessageCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService methodService, IModMailService modMailService,SlashCommandContext ctx, DiscordUser user,string message)
    {
        await ctx.DeferResponseAsync(true);
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        if (ctx.Guild is null)
        {
            throw new NullReferenceException("Guild is null. This should not happen.");
        }
        if (ctx.Member is null)
        {
            throw new NullReferenceException("Member is null. This should not happen.");
        }
        Guild guildSettings = await dbContext.Guilds.FindAsync(ctx.Guild.Id) ?? await methodService.AddGuildAsync(new Guild(ctx.Guild.Id));
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
        messageBuilder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Primary, $"{modMailService.OpenButtonPrefix}{ctx.Guild.Id}", "Open Mod Mail"));
        messageBuilder.WithContent(dmMessage);

        await member.SendMessageAsync(messageBuilder);

        DiscordChannel modMailChannel = await ctx.Guild.GetChannelAsync(guildSettings.ModMailChannelId.Value);
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
}