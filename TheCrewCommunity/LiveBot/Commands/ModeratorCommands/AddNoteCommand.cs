using System.Text;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;
public static class AddNoteCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory, IModeratorLoggingService moderatorLoggingService, IDatabaseMethodService databaseMethodService, SlashCommandContext ctx, DiscordUser user, string note, DiscordAttachment? image = null)
    {
        await ctx.DeferResponseAsync(true);
        if (ctx.Guild is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This command can only be used in a server!"));
            return;
        }
        if (ctx.Member is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This command can only be used by a member!"));
            return;
        }
        await databaseMethodService.AddInfractionsAsync(new Infraction(ctx.User.Id, user.Id, ctx.Guild.Id, note, false, InfractionType.Note));

        StringBuilder confirmationBuilder = new();
        confirmationBuilder.AppendLine($"{ctx.User.Mention}, a note has been added to {user.Username}({user.Id})");
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guild = await dbContext.Guilds.FindAsync(ctx.Guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(ctx.Guild.Id));
        if (guild.ModerationLogChannelId == 0)
        {
            confirmationBuilder.AppendLine("This server is not set up for logging moderation actions. Contact an Admin to resolve the issue.");
        }
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(confirmationBuilder.ToString()));
        DiscordChannel channel = await ctx.Guild.GetChannelAsync(Convert.ToUInt64(guild.ModerationLogChannelId));
        StringBuilder descriptionBuilder = new();
        descriptionBuilder.AppendLine("# 📝 Note Added")
            .AppendLine($"- **User:** {user.Mention}")
            .AppendLine($"- **Moderator:** {ctx.Member.Mention}")
            .Append($"- **Note:** {note}");
        moderatorLoggingService.AddToQueue(new ModLogItem(
            channel,
            user,
            descriptionBuilder.ToString(),
            ModLogType.Info,
            attachment: image));
    }
}