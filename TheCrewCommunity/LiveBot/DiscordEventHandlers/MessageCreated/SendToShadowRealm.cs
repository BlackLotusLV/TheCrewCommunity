using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.Entities.Discord;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers.MessageCreated;

public static class SendToShadowRealm
{
    public static async Task Banish(DiscordClient client, MessageCreatedEventArgs e)
    {
        await e.Message.DeleteAsync();
        GeneralUtils utils = client.ServiceProvider.GetRequiredService<GeneralUtils>();
        LiveBotDbContext dbContext = client.ServiceProvider.GetRequiredService<LiveBotDbContext>();
        DiscordMember member = await e.Guild.GetMemberAsync(e.Author.Id);
        if (utils.CheckIfMemberAdmin(member))
        {
            try
            {
                await member.TimeoutAsync(DateTimeOffset.UtcNow+TimeSpan.FromDays(1), "Posted in Shadow Realm, Investigate!");
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException exception)
            {
                Guild? guild = await dbContext.Guilds.FindAsync(e.Guild.Id);
                DiscordChannel channel = await e.Guild.GetChannelAsync(Convert.ToUInt64(guild?.ModerationLogChannelId));
                await channel.SendMessageAsync($"Failed to timeout {member.Mention} - User posted in shadow realm, investigate!");
            }
            return;
        }

        await member.BanAsync(TimeSpan.FromDays(7), "Posted in Shadow Realm. Probably compromised");
        Console.WriteLine("user posted in naughty zone");
    }
}