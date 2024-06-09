using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers;

public static class MembershipScreening
{
    public static async Task OnAcceptRules(DiscordClient client, GuildMemberUpdatedEventArgs e)
    {
        if (e.PendingBefore is null || e.PendingAfter is null) return;
        if (e.PendingBefore.Value && !e.PendingAfter.Value)
        {
            var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
            
            await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
            Guild? guild = await liveBotDbContext.Guilds.FindAsync(e.Guild.Id);
            if (guild?.WelcomeChannelId == null || !guild.HasScreening) return;
            DiscordChannel welcomeChannel = await e.Guild.GetChannelAsync(Convert.ToUInt64(guild.WelcomeChannelId));

            if (guild.WelcomeMessage == null) return;
            string msg = guild.WelcomeMessage;
            msg = msg.Replace("$Mention", $"{e.Member.Mention}");
            await welcomeChannel.SendMessageAsync(msg);

            if (guild.RoleId == null) return;
            DiscordRole? role = e.Guild.GetRole(Convert.ToUInt64(guild.RoleId));
            if (role is null)
            {
                client.Logger.LogWarning("Could not find role of id: {RoleId} in guild {GuildName}. Check if it exists. User join role not added",guild.RoleId,e.Guild.Name);
                return;
            }
            await e.Member.GrantRoleAsync(role);
        }
    }
}