using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModMailCommands;
public static class ReplyCommand
{
    public static async ValueTask ExecuteAsync(SlashCommandContext ctx,long id, string reply, IDbContextFactory<LiveBotDbContext> dbContextFactory,IDatabaseMethodService databaseMethodService)
    {
        if (ctx.Guild is null)
        {
            await ctx.RespondAsync("This command can only be used in a server!");
            return;
        }
        await ctx.DeferResponseAsync(true);
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        ModMail? entry = await dbContext.ModMail.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (entry == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Could not find an active entry with this ID."));
            return;
        }

        DiscordEmbedBuilder embed = new()
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                IconUrl = ctx.User.AvatarUrl,
                Name = ctx.User.Username
            },
            Title = $"[REPLY] #{entry.Id} Mod Mail Response",
            Description = $"{ctx.Member.Username} - {reply}",
            Color = new DiscordColor(entry.ColorHex)
        };
        try
        {
            DiscordMember member = await ctx.Guild.GetMemberAsync(entry.UserDiscordId);
            await member.SendMessageAsync($"{ctx.Member.Username} - {reply}");
        }
        catch (Exception e)
        {
            embed.Description = $"User has left the server, blocked the bot or closed their DMs. Could not send a response!\nHere is what you said `{reply}`";
            embed.Title = $"[ERROR] {embed.Title}";
            Console.WriteLine(e.InnerException);
        }

        await dbContext.ModMail
            .Where(x => x.Id == entry.Id)
            .ExecuteUpdateAsync(p => p.SetProperty(x => x.LastMessageTime, x => DateTime.UtcNow));

        Guild guild = await dbContext.Guilds.FindAsync(ctx.Guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(ctx.Guild.Id));

        if (guild.ModMailChannelId != null)
        {

            ulong channelId = guild.ModMailChannelId.Value;
            DiscordChannel mmChannel = ctx.Guild.GetChannel(channelId);
            await mmChannel.SendMessageAsync(embed: embed);
            ctx.Client.Logger.LogInformation(CustomLogEvents.ModMail, "An admin has responded to Mod Mail entry #{EntryId}", entry.Id);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Mod mail #{id} reply sent"));
        }
        else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed).WithContent("Mod mail channel not specified in server settings, sending reply here"));
        }
    }
}