using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers;

public static class MemberFlow
{
    public static async Task OnJoin(DiscordClient client, GuildMemberAddedEventArgs e)
    {
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guild = await liveBotDbContext.Guilds.FindAsync(e.Guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(e.Guild.Id));
        if (guild?.WelcomeChannelId == null || guild.HasScreening) return;
        DiscordChannel welcomeChannel = await e.Guild.GetChannelAsync(Convert.ToUInt64(guild.WelcomeChannelId));

        if (guild.WelcomeMessage == null) return;
        string msg = guild.WelcomeMessage;
        msg = msg.Replace("$Mention", $"{e.Member.Mention}");
        await welcomeChannel.SendMessageAsync(msg);

        if (guild.RoleId == null) return;
        DiscordRole role = await e.Guild.GetRoleAsync(Convert.ToUInt64(guild.RoleId));
        await e.Member.GrantRoleAsync(role);
    }
    public static async Task OnLeave(DiscordClient client, GuildMemberRemovedEventArgs e)
    {
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        var modMailService = client.ServiceProvider.GetRequiredService<IModMailService>();
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guild = await liveBotDbContext.Guilds.FindAsync(e.Guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(e.Guild.Id));
        bool pendingCheck = guild is not null && !(guild.HasScreening && e.Member.IsPending == true);
        if (guild is { WelcomeChannelId: not null } && pendingCheck)
        {
            DiscordChannel welcomeChannel = await e.Guild.GetChannelAsync(Convert.ToUInt64(guild.WelcomeChannelId));
            if (guild.GoodbyeMessage != null)
            {
                string msg = guild.GoodbyeMessage;
                msg = msg.Replace("$Username", $"{e.Member.Username}");
                await welcomeChannel.SendMessageAsync(msg);
            }
        }

        ModMail? modMailEntry = liveBotDbContext.ModMail.FirstOrDefault(w => w.UserDiscordId == e.Member.Id && w.GuildId == e.Guild.Id && w.IsActive);
        if (modMailEntry is not null)
        {
            await modMailService.CloseModMailAsync(client, modMailEntry, e.Member, "Mod Mail entry closed due to user leaving",
                "**Mod Mail closed!\n----------------------------------------------------**");
        }
    }

    public static async Task LogJoin(DiscordClient client, GuildMemberAddedEventArgs e)
    {
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        var warningService = client.ServiceProvider.GetRequiredService<IModeratorWarningService>();
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guildSettings = await liveBotDbContext.Guilds.FindAsync(e.Guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(e.Guild.Id));
        if (guildSettings.UserTrafficChannelId == null) return;
        DiscordGuild guild = client.Guilds.FirstOrDefault(w => w.Value.Id == guildSettings.Id).Value;
        DiscordChannel userTraffic = await guild.GetChannelAsync(guildSettings.UserTrafficChannelId.Value);
        DiscordEmbedBuilder embed = new()
        {
            Title = $"📥{e.Member.Username}({e.Member.Id}) has joined the server",
            Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                IconUrl = e.Member.AvatarUrl,
                Text = $"User joined ({e.Guild.MemberCount})"
            },
            Color = new DiscordColor(0x00ff00)
        };
        embed.AddField("User tag", e.Member.Mention);

        var infractions = await liveBotDbContext.Infractions.Where(infraction => infraction.GuildId == e.Guild.Id && infraction.UserId == e.Member.Id).ToListAsync();

        embed.AddField("Infraction count", infractions.Count.ToString());
        DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder()
            .AddEmbed(embed)
            .AddComponents(
                new DiscordButtonComponent(DiscordButtonStyle.Primary, $"{warningService.InfractionButtonPrefix}{e.Member.Id}", "Get infractions"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, $"{warningService.UserInfoButtonPrefix}{e.Member.Id}", "Get User Info")
            );
        await userTraffic.SendMessageAsync(messageBuilder);
    }

    public static async Task LogLeave(DiscordClient client, GuildMemberRemovedEventArgs e)
    {
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var warningService = client.ServiceProvider.GetRequiredService<IModeratorWarningService>();
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild? guildSettings = await liveBotDbContext.Guilds.FirstOrDefaultAsync(x => x.Id == e.Guild.Id);
        if (guildSettings?.UserTrafficChannelId == null) return;
        DiscordGuild guild = client.Guilds.FirstOrDefault(w => w.Value.Id == guildSettings.Id).Value;
        DiscordChannel userTraffic = await guild.GetChannelAsync(guildSettings.UserTrafficChannelId.Value);
        DiscordEmbedBuilder embed = new()
        {
            Title = $"📤{e.Member.Username}({e.Member.Id}) has left the server",
            Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                IconUrl = e.Member.AvatarUrl,
                Text = $"User left ({e.Guild.MemberCount})"
            },
            Color = new DiscordColor(0xff0000)
        };
        embed.AddField("User tag", e.Member.Mention);

        var infractions = await liveBotDbContext.Infractions.Where(infraction => infraction.GuildId == e.Guild.Id && infraction.UserId == e.Member.Id).ToListAsync();

        embed.AddField("Infraction count", infractions.Count.ToString());
        DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder()
            .AddEmbed(embed)
            .AddComponents(
                new DiscordButtonComponent(DiscordButtonStyle.Primary, $"{warningService.InfractionButtonPrefix}{e.Member.Id}", "Get infractions"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, $"{warningService.UserInfoButtonPrefix}{e.Member.Id}", "Get User Info")
            );
        await userTraffic.SendMessageAsync(messageBuilder);
    }
}