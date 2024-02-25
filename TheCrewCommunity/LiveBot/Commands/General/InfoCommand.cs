using System.ComponentModel;
using DSharpPlus;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Attributes;
using DSharpPlus.Commands.Trees.Attributes;
using DSharpPlus.Entities;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class InfoCommand(IModeratorWarningService warningService)
{
    [Command("Info"), Description("Shows general info about the user."), RequireGuild, SlashCommandTypes(ApplicationCommandType.SlashCommand, ApplicationCommandType.UserContextMenu)]
    public async Task ExecuteAsync(SlashCommandContext ctx, [Description("User who to get the info about.")] DiscordUser user)
    {
        await ctx.DeferResponseAsync(true);
        if (ctx.Guild is null)
        {
            throw new NullReferenceException("Guild is null. This should not happen.");
        }
        DiscordEmbed userInfoEmbed = await warningService.GetUserInfoAsync(ctx.Guild, user);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(userInfoEmbed));
    }
}