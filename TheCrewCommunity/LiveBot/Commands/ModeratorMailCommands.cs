using System.Collections.Immutable;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands;

[SlashCommandGroup("ModMail", "Moderator commands for mod mail"), SlashRequireGuild, SlashRequirePermissions(Permissions.ManageMessages)]
public sealed class ModeratorMailCommands : ApplicationCommandModule
{
    public IDbContextFactory<LiveBotDbContext> DbContextFactory { private get; set; }
    public IDatabaseMethodService DatabaseMethodService { private get; set; }
    public IModMailService ModMailService { private get; set; }

    [SlashCommand("Reply", "Replies to a specific mod mail")]
    public async Task Reply(InteractionContext ctx, [Autocomplete(typeof(ActiveModMailOption))] [Option("ID", "Mod Mail Entry ID")] long id,
        [Option("Response", "The message to send to the user")] string reply)
    {
        await ctx.DeferAsync(true);
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
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

        Guild guild = await dbContext.Guilds.FindAsync(ctx.Guild.Id) ?? await DatabaseMethodService.AddGuildAsync(new Guild(ctx.Guild.Id));

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

    private sealed class ActiveModMailOption : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var dbContextFactory = ctx.Services.GetService<IDbContextFactory<LiveBotDbContext>>();
            if (dbContextFactory is null) return [];
            await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
            Guild guild = liveBotDbContext.Guilds.Include(x => x.GuildUsers).ThenInclude(x => x.ModMails.Where(y => y.IsActive)).First(x => x.Id == ctx.Guild.Id);
            var activeModMails = guild.GuildUsers.SelectMany(x => x.ModMails).ToImmutableList();
            List<DiscordAutoCompleteChoice> result = [];
            foreach (ModMail modMail in activeModMails)
            {
                DiscordMember member = await ctx.Guild.GetMemberAsync(modMail.UserDiscordId);
                result.Add(new DiscordAutoCompleteChoice($"#{modMail.Id} - {member.Username}", modMail.Id));
            }
            return result;
        }
    }

    [SlashCommand("close", "Closes a Mod Mail entry")]
    public async Task Close(InteractionContext ctx, [Autocomplete(typeof(ActiveModMailOption))] [Option("ID", "Mod Mail Entry ID")] long id)
    {
        await ctx.DeferAsync(true);
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        ModMail? entry = await dbContext.ModMail.FindAsync(id);
        if (entry is not { IsActive: true })
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Could not find an active entry with this ID."));
            return;
        }

        await ModMailService.CloseModMailAsync(ctx.Client, entry, ctx.User, $" Mod Mail closed by {ctx.User.Username}",
            $"**Mod Mail closed by {ctx.User.Username}!\n----------------------------------------------------**");
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"ModMail entry #{id} closed."));
    }

    [SlashCommand("block", "Blocks a user from using ModMail")]
    public async Task ModMailBlock(InteractionContext ctx, [Option("user", "User to block")] DiscordUser user)
    {
        await ctx.DeferAsync(true);
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        GuildUser? guildUser = await dbContext.GuildUsers.FindAsync(user.Id, ctx.Guild.Id);
        if (guildUser == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{user.Username}({user.Id}) is not a member of this server"));
            return;
        }

        if (guildUser.IsModMailBlocked)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{user.Username}({user.Id}) is already blocked from using ModMail"));
            return;
        }

        guildUser.IsModMailBlocked = true;
        dbContext.GuildUsers.Update(guildUser);
        await dbContext.SaveChangesAsync();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{user.Username}({user.Id}) has been blocked from using ModMail"));
    }

    [SlashCommand("unblock", "Unblocks a user from using ModMail")]
    public async Task ModMailUnblock(InteractionContext ctx, [Option("user", "User to unblock")] DiscordUser user)
    {
        await ctx.DeferAsync(true);
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        GuildUser? guildUser = await dbContext.GuildUsers.FindAsync(user.Id, ctx.Guild.Id);
        if (guildUser == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{user.Username}({user.Id}) is not a member of this server"));
            return;
        }

        if (!guildUser.IsModMailBlocked)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{user.Username}({user.Id}) is not blocked from using ModMail"));
            return;
        }

        guildUser.IsModMailBlocked = false;
        dbContext.GuildUsers.Update(guildUser);
        await dbContext.SaveChangesAsync();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{user.Username}({user.Id}) has been unblocked from using ModMail"));
    }
}