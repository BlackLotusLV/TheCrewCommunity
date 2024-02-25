using System.ComponentModel;
using DSharpPlus;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Attributes;
using DSharpPlus.Commands.Trees.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class ReportMessageCommand(IDbContextFactory<LiveBotDbContext> dbContextFactory)
{
    [Command("Report"), Description("Report a message so moderators can take a look"), SlashCommandTypes(ApplicationCommandType.MessageContextMenu), RequireGuild]
    public async Task ExecuteAsync(SlashCommandContext ctx, DiscordMessage targetMessage)
    {
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
            .AddComponents(new TextInputComponent("Complaint","Complaint","What is your complaint?",null,true,TextInputStyle.Paragraph)
            );
        await ctx.Interaction.CreateResponseAsync(InteractionResponseType.Modal,modal);
        
        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        var response = await interactivity.WaitForModalAsync(modal.CustomId, ctx.User);
        if (response.TimedOut) return;
        
        DiscordEmbedBuilder reportEmbed = new DiscordEmbedBuilder()
            .WithTitle("Message reported")
            .WithDescription($"# Contents:\n`{targetMessage.Content}`")
            .WithAuthor($"{ctx.User.Username}({ctx.User.Id})", null, ctx.User.AvatarUrl);

        var raiseHandButton = new DiscordButtonComponent(ButtonStyle.Primary, $"raiseHand-report-{targetMessage.ChannelId}-{targetMessage.Id}", "Raise Hand", false, new DiscordComponentEmoji("✋"));
        
        DiscordMessageBuilder reportMessage = new DiscordMessageBuilder()
            .AddEmbed(reportEmbed)
            .AddComponents(raiseHandButton);
            
        DiscordChannel reportChannel = ctx.Guild.GetChannel(guild.UserReportsChannelId.Value);
        await reportChannel.SendMessageAsync(reportMessage);
        await response.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Report sent. A Moderator will review it soon. *If actions are taken, you wil NOT be informed*").AsEphemeral());
    }
}