using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;
public static class DeleteNoteCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory, IModeratorLoggingService moderatorLoggingService,SlashCommandContext ctx, DiscordUser user, long noteId)
    {
        if (ctx.Guild is null)
        {
            await ctx.RespondAsync("This command can only be used in a server!");
            return;
        }
        if (ctx.Member is null)
        {
            await ctx.RespondAsync("This command can only be used by a member!");
            return;
        }
        await ctx.DeferResponseAsync(true);
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
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
            .AddComponents(new DiscordButtonComponent(DiscordButtonStyle.Success, $"yes", "Yes"),
                new DiscordButtonComponent(DiscordButtonStyle.Danger, $"no", "No"));
        
        DiscordMessage message = await ctx.EditResponseAsync(responseBuilder);
        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var response = await interactivity.WaitForButtonAsync(message, ctx.Member, TimeSpan.FromSeconds(30));
        if (response.TimedOut) return;
        if (response.Result.Id == "no")
        {
            await response.Result.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Note not deleted")
                    .AsEphemeral()
            );
            return;
        }
        
        dbContext.Infractions.Remove(infraction);
        await dbContext.SaveChangesAsync();
        await response.Result.Interaction.CreateResponseAsync(
            DiscordInteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"Note `{infraction.Id}` deleted")
                .AsEphemeral()
            );
        Guild? guild = await dbContext.Guilds.FindAsync(ctx.Guild.Id);
        if (guild is null) return;
        DiscordChannel channel = await ctx.Guild.GetChannelAsync(Convert.ToUInt64(guild.ModerationLogChannelId));
        moderatorLoggingService.AddToQueue(new ModLogItem(
            channel,
            user,
            "# Note Deleted\n" +
            $"- **User:** {user.Mention}\n" +
            $"- **Moderator:** {ctx.Member.Mention}\n" +
            $"- **Note:** {infraction.Reason}",
            ModLogType.Info));
    }
}