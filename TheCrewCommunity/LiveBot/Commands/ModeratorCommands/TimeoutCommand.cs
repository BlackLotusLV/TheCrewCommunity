using System.Text.RegularExpressions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public static partial class TimeoutCommand
{
    public static async Task ExecuteAsync(InteractivityExtension interactivity, SlashCommandContext ctx, DiscordMember member, string duration, string reason)
    {
        await ctx.DeferResponseAsync(true);
        if (ctx.Guild is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This command can only be used in a server!"));
            return;
        }
        DateTimeOffset timeOutTime = GetTimeOutTime(duration);
        var existingTimeOut = "*None*";
        if (member.CommunicationDisabledUntil is not null && member.CommunicationDisabledUntil > DateTimeOffset.UtcNow)
        {
            existingTimeOut = $"<t:{member.CommunicationDisabledUntil.Value.ToUnixTimeSeconds()}:F>(<t:{member.CommunicationDisabledUntil.Value.ToUnixTimeSeconds()}:R>)";
        }
        if (timeOutTime <= DateTimeOffset.UtcNow)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Failed to parse timeout properly, please try again."));
            return;
        }
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("Timeout Confirmation")
            .WithDescription($"You are about time out {member.Mention}.\n" +
                             $"- **Until:** <t:{timeOutTime.ToUnixTimeSeconds()}:F>(<t:{timeOutTime.ToUnixTimeSeconds()}:R>)\n" +
                             $"- **Reason:** `{reason}`\n" +
                             $"- **Existing TO: {existingTimeOut}**\n" +
                             $"# Are You sure?")
            .WithColor(DiscordColor.Gold);
        DiscordInteractionResponseBuilder confirmationResponse= new DiscordInteractionResponseBuilder()
            .AddEmbed(embed)
            .AddComponents(new DiscordButtonComponent(DiscordButtonStyle.Success, "confirm", "Confirm"), new DiscordButtonComponent(DiscordButtonStyle.Danger, "cancel", "Cancel"));
        DiscordMessage confirmationMessage = await ctx.EditResponseAsync(confirmationResponse);
        var interaction = await interactivity.WaitForButtonAsync(confirmationMessage, ctx.User);
        if (interaction.TimedOut)
        {
            await ctx.EditResponseAsync("Timeout cancelled");
            return;
        }
        switch (interaction.Result.Id)
        {
            case "cancel":
                await ctx.EditResponseAsync("Timeout action cancelled.");
                return;
            case "confirm":
                await member.TimeoutAsync(timeOutTime, reason);
                await ctx.EditResponseAsync($"User {member.Mention} has been timed out until <t:{timeOutTime.ToUnixTimeSeconds()}:F>(<t:{timeOutTime.ToUnixTimeSeconds()}:R>) for `{reason}`");
                break;
        }
    }

    private static DateTimeOffset GetTimeOutTime(string duration)
    {
        Regex regex = MyRegex();
        MatchCollection matches = regex.Matches(duration.ToLower());

        DateTimeOffset timeOutTime = DateTimeOffset.UtcNow;

        foreach (Match match in matches)
        {
            double value = double.Parse(match.Groups[1].Value);
            string unit = match.Groups[2].Value;

            timeOutTime = unit switch
            {
                "d" or "day" or "days" => timeOutTime.AddDays(value),
                "h" or "hour" or "hours" => timeOutTime.AddHours(value),
                "m" or "min" or "mins" or "minute" or "minutes" => timeOutTime.AddMinutes(value),
                "s" or "sec" or "secs" or "second" or "seconds" => timeOutTime.AddSeconds(value),
                _ => timeOutTime.AddSeconds(0)
            };
        }

        return timeOutTime;
    }

    [GeneratedRegex(@"(\d+)([a-z]+)")]
    private static partial Regex MyRegex();
}