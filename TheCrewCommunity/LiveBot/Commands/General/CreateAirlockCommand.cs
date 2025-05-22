using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class CreateAirlockCommand(IDbContextFactory<LiveBotDbContext> dbContextFactory)
{
    [Command("CreateAirlock"), RequireGuild, RequirePermissions(DiscordPermission.Administrator)]
    public async Task CreateAirlock(SlashCommandContext ctx, DiscordRole role, DiscordChannel? channel = null)
    {
        await ctx.DeferResponseAsync(true);
        if (ctx.Guild is null)
        {
            await ctx.RespondAsync("You can't use that here!");
            return;
        }

        StringBuilder headText = new();
        headText.AppendLine("# :traffic_light: AIRLOCK");
        headText.AppendLine("## Welcome! Before you can access the rest of the server:");
        headText.AppendLine("️1️⃣ Agree to the server rules.");
        headText.AppendLine("2️⃣ Set your [Discord server profile](https://support.discord.com/hc/en-us/articles/4409388345495-Server-Profiles) nickname to your Ubisoft Connect username");
        const string imagePath = "https://thecrew-community.com/assets/AirlockAssets/";
        
        DiscordMessageBuilder messageBuilder = new();
        messageBuilder.EnableV2Components()
            .AddTextDisplayComponent(new DiscordTextDisplayComponent(headText.ToString()))
            .AddMediaGalleryComponent(new DiscordMediaGalleryItem(imagePath + "ubiName.png"))
            .AddMediaGalleryComponent(new DiscordMediaGalleryItem(imagePath + "airlockExample.webp"))
            .AddTextDisplayComponent("3️⃣ Click the \"Verify\" button below this message.")
            .AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Primary, "Activate", "Verify"));

        channel ??= await ctx.Guild.CreateTextChannelAsync("🚥-airlock");

        await channel.ModifyAsync(x =>
        {
            x.Topic = "Verification channel - Please follow the instructions to access the server";
            x.PermissionOverwrites = new List<DiscordOverwriteBuilder>
            {
                new DiscordOverwriteBuilder(ctx.Guild.EveryoneRole)
                    .Allow(DiscordPermission.ReadMessageHistory)
                    .Deny(new(DiscordPermission.ViewChannel, DiscordPermission.SendMessages)),
                new DiscordOverwriteBuilder(role)
                    .Allow(DiscordPermission.ViewChannel)
                    .Deny(new())
            };
        });
        await channel.SendMessageAsync(messageBuilder);
        LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.WhiteListSettings.AddAsync(new(ctx.Guild.Id, role.Id));
        await dbContext.SaveChangesAsync();
        await ctx.RespondAsync($"Airlock created! <#{channel.Id}>");
    }
}