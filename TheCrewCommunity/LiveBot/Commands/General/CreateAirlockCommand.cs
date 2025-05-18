using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class CreateAirlockCommand
{
    [Command("CreateAirlock"), RequireGuild, RequirePermissions(DiscordPermission.Administrator)]
    public async Task CreateAirlock(SlashCommandContext ctx)
    {
        await ctx.RespondAsync("not implomented");

        StringBuilder headText = new();
        headText.AppendLine("# :traffic_light: AIRLOCK");
        headText.AppendLine("Welcome! Before you can access the rest of the server:");
        headText.AppendLine("️⃣ Agree to the server rules.");
        headText.AppendLine("2️⃣ Set your Discord server profile nickname (https://support.discord.com/hc/en-us/articles/4409388345495-Server-Profiles) to your Ubisoft Connect username");
        
        DiscordMessageBuilder messageBuilder = new();
        messageBuilder.EnableV2Components()
            .AddTextDisplayComponent(new DiscordTextDisplayComponent(headText.ToString()))
            .AddMediaGalleryComponent(new DiscordMediaGalleryItem(""), new DiscordMediaGalleryItem(""))
    }
}