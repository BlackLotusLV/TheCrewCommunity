﻿using System.Text;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;
public static class EditNoteCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory, IModeratorLoggingService moderatorLoggingService, InteractivityExtension interactivity,SlashCommandContext ctx, DiscordUser user, long noteId)
    {
        if (ctx.Member is null || ctx.Guild is null)
        {
            await ctx.RespondAsync("Incorrect usage of command, please use this command in a guild channel");
            return;
        }
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Infraction? infraction = await dbContext.Infractions.FindAsync(noteId);
        if (infraction is null || infraction.UserId != user.Id || infraction.AdminDiscordId != ctx.User.Id || infraction.InfractionType != InfractionType.Note)
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Could not find a note with that ID"));
            return;
        }

        string oldNote = infraction.Reason??"*No note content*";
        var customId = $"EditNote-{ctx.User.Id}";
        DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder()
            .WithTitle("Edit users note")
            .WithCustomId(customId)
            .AddTextInputComponent(new DiscordTextInputComponent("Content", "Content", null, oldNote, true, DiscordTextInputStyle.Paragraph));
        await ctx.RespondWithModalAsync(modal);

        var response = await interactivity.WaitForModalAsync(customId, ctx.User);
        if (response.TimedOut) return;
        infraction.Reason = response.Result.Values["Content"];
        dbContext.Infractions.Update(infraction);
        await dbContext.SaveChangesAsync();
        await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent($"Note `#{infraction.Id}` content changed\nFrom:`{oldNote}`\nTo:`{infraction.Reason}`").AsEphemeral());
        Guild? guild = await dbContext.Guilds.FindAsync(ctx.Guild.Id);
        if (guild is null) return;
        DiscordChannel channel = await ctx.Guild.GetChannelAsync(Convert.ToUInt64(guild.ModerationLogChannelId));
        StringBuilder descriptionBuilder = new();
        descriptionBuilder.AppendLine("# ✏️ Note Edited")
            .AppendLine($"- **User:** {user.Mention}")
            .AppendLine($"- **Moderator:** {ctx.Member.Mention}")
            .AppendLine($"- **Old Note:** {oldNote}")
            .Append($"- **New Note:** {infraction.Reason}");
        moderatorLoggingService.AddToQueue(new ModLogItem(
            channel,
            user,
            descriptionBuilder.ToString(),
            ModLogType.Info));
    }
}