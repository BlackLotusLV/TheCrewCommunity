using System.ComponentModel;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public static class WarnCommand
{
    public static async Task ExecuteAsync(IModeratorWarningService warningService, SlashCommandContext ctx,DiscordUser user,string reason,TimeOutOptions timeOut,DiscordAttachment? image = null)
    {
        if (ctx.Guild is null)
        {
            await ctx.RespondAsync("This command can only be used in a server!");
            return;
        }
        await ctx.DeferResponseAsync(true);
        warningService.AddToQueue(new WarningItem(user, ctx.User, ctx.Guild, ctx.Channel, reason, false, ctx, image));
        if (timeOut == 0) return;
        DiscordMember member;
        try
        {
            member = await ctx.Guild.GetMemberAsync(user.Id);
        }
        catch (Exception)
        {
            ctx.Client.Logger.LogDebug("Could not find member {MemberId} in guild {GuildId}", user.Id, ctx.Guild.Id);
            return;
        }
        await member.TimeoutAsync(DateTimeOffset.Now + TimeSpan.FromSeconds((int)timeOut), "Timed out by warning");
    }
    public enum TimeOutOptions
    {
        [Description("none")] None = 0,
        [Description("60 secs")] SixtySecs = 60,
        [Description("5 min")] FiveMin = 300,
        [Description("10 min")] TenMin = 600,
        [Description("1 hour")] OneHour = 3600,
        [Description("1 day")] OneDay = 86400,
        [Description("1 week")] OneWeek = 604800
    }
}