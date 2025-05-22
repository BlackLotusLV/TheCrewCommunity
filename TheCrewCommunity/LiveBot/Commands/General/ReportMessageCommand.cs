using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class ReportMessageCommand(IDbContextFactory<LiveBotDbContext> dbContextFactory, InteractivityExtension interactivity)
{
    [Command("Report"), Description("Report a message so moderators can take a look"), SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu), RequireGuild]
    public async Task ExecuteAsync(SlashCommandContext ctx, DiscordMessage targetMessage)
    {
        if (ctx.Guild is null)
        {
            await ctx.RespondAsync("This command can only be used in a guild channel");
            return;
        }
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guild? guild = await dbContext.Guilds.FindAsync(ctx.Guild.Id);
        if (guild?.UserReportsChannelId is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This server is not set up for reporting messages."));
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                    .WithContent("This server is not set up for reporting messages.")
                    .AsEphemeral()
                );
            return;
        }
        
        DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder()
            .WithTitle("Report Message")
            .WithCustomId("report_message")
            .AddTextInputComponent(new DiscordTextInputComponent("Complaint","Complaint","What is your complaint?",null,true,DiscordTextInputStyle.Paragraph)
            );
        await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal,modal);
        
        var response = await interactivity.WaitForModalAsync(modal.CustomId, ctx.User);
        if (response.TimedOut) return;
        
        DiscordEmbedBuilder reportEmbed = new DiscordEmbedBuilder()
            .WithTitle("Message reported")
            .WithDescription($"# Contents:\n`{targetMessage.Content}`")
            .WithAuthor($"{ctx.User.Username}({ctx.User.Id})", null, ctx.User.AvatarUrl);

        var raiseHandButton = new DiscordButtonComponent(DiscordButtonStyle.Primary, $"raiseHand-report-{targetMessage.ChannelId}-{targetMessage.Id}", "Raise Hand", false, new DiscordComponentEmoji("✋"));
        
        DiscordMessageBuilder reportMessage = new DiscordMessageBuilder()
            .AddEmbed(reportEmbed)
            .AddActionRowComponent(raiseHandButton);
            
        DiscordChannel reportChannel = await ctx.Guild.GetChannelAsync(guild.UserReportsChannelId.Value);
        await reportChannel.SendMessageAsync(reportMessage);
        await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Report sent. A Moderator will review it soon. *If actions are taken, you will NOT be informed*").AsEphemeral());
    }
}