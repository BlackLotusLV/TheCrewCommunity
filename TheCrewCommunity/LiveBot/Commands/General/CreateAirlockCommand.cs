using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class CreateAirlockCommand(IWebHostEnvironment environment, IDbContextFactory<LiveBotDbContext> dbContextFactory)
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
        
        StringBuilder imageUrls = new();
        imageUrls.Append(environment.IsProduction() ? "https://thecrew-community.com" : "https://localhost:5001");
        imageUrls.Append("/images/airlock/");
        
        DiscordMessageBuilder messageBuilder = new();
        messageBuilder.EnableV2Components()
            .AddTextDisplayComponent(new DiscordTextDisplayComponent(headText.ToString()))
            .AddMediaGalleryComponent(new DiscordMediaGalleryItem(imageUrls + "ubiname.png"), new DiscordMediaGalleryItem(imageUrls + "airlockexample.webp"))
            .AddTextDisplayComponent("3️⃣ Click the \"Verify\" button below this message.")
            .AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Primary, "Activate", "Verify"));

        channel ??= await ctx.Guild.CreateTextChannelAsync("🚥-airlock");

        await channel.AddOverwriteAsync(ctx.Guild.EveryoneRole, new(DiscordPermission.ReadMessageHistory),new(DiscordPermission.ViewChannel,DiscordPermission.SendMessages));
        await channel.AddOverwriteAsync(role,new(DiscordPermission.ViewChannel),new());
        await channel.SendMessageAsync(messageBuilder);
        LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.WhiteListSettings.AddAsync(new(ctx.Guild.Id, role.Id));
        await dbContext.SaveChangesAsync();
        await ctx.RespondAsync($"Airlock created! <#{channel.Id}>");
    }
}