using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public static class SayCommand
{
    public static async Task ExecuteAsync(SlashCommandContext ctx,string message,DiscordChannel? channel = null)
    {
        await ctx.DeferResponseAsync(true);
        channel ??= ctx.Channel;

        await channel.SendMessageAsync(message);
        await ctx.EditResponseAsync("Message has been sent");
    }
}