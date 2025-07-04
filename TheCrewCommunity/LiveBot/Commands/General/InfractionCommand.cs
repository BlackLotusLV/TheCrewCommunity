﻿using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using TheCrewCommunity.Services;
namespace TheCrewCommunity.LiveBot.Commands.General;

public class InfractionCommand(IModeratorWarningService warningService)
{
    [Command("Infractions"), Description("Get the infractions of a user"), RequirePermissions(DiscordPermission.ModerateMembers),
     SlashCommandTypes(DiscordApplicationCommandType.SlashCommand, DiscordApplicationCommandType.UserContextMenu), RequireGuild]
    public async Task ExecuteAsync(SlashCommandContext ctx, [Description("User to get the infractions for")] DiscordUser user)
    {
        await ctx.DeferResponseAsync(true);
        DiscordWebhookBuilder webhookBuilder = new();
        if (ctx.Member is null)
        {
            throw new NullReferenceException("Member is null. This should not happen.");
        }

        if (ctx.Guild is null)
        {
            throw new NullReferenceException("Guild is null. This should not happen.");
        }

        bool isModerator = ctx.Member.Permissions.HasPermission(DiscordPermission.ModerateMembers);
        var embeds = await warningService.BuildInfractionsEmbedsAsync(ctx.Guild, user, isModerator);
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
        DiscordButtonComponent leftButton = new(DiscordButtonStyle.Primary, leftButtonId, "", true, new DiscordComponentEmoji("⬅️")),
            stopButton = new(DiscordButtonStyle.Danger, stopButtonId, "", false, new DiscordComponentEmoji("⏹️")),
            rightButton = new(DiscordButtonStyle.Primary, rightButtonId, "", false, new DiscordComponentEmoji("➡️"));

        webhookBuilder.AddActionRowComponent(leftButton, stopButton, rightButton);
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
                .AddActionRowComponent(leftButton, stopButton, rightButton);
            await ctx.EditResponseAsync(webhookBuilder);
            await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
        }
    }
}