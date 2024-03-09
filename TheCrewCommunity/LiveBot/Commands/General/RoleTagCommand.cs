using System.Collections.ObjectModel;
using System.ComponentModel;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.Attributes;
using DSharpPlus.Commands.Trees.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class RoleTagCommand(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService)
{
    [Command("Roletag"), Description("Pings a role under specific criteria."), RequireGuild]
    public async Task ExecuteAsync(SlashCommandContext ctx, [SlashAutoCompleteProvider(typeof(RoleTagAutoCompleteProvider)),Description("Which role to tag")] long id)
    {
        await ctx.DeferResponseAsync(true);
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        if (ctx.Guild is null || ctx.Member is null)
        {
            await ctx.EditResponseAsync("This command can only be used in a server.");
            return;
        }
        Guild guild = await dbContext.Guilds.Include(x => x.RoleTagSettings).FirstOrDefaultAsync(x => x.Id == ctx.Guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(ctx.Guild.Id));
        
        if (guild.RoleTagSettings is null || guild.RoleTagSettings.Count == 0)
        {
            await ctx.EditResponseAsync("There are no roles to tag in this server.");
            return;
        }
        
        RoleTagSettings? roleTagSettings = guild.RoleTagSettings.FirstOrDefault(x=>x.Id==id);
        if (roleTagSettings == null || roleTagSettings.GuildId != ctx.Guild.Id || roleTagSettings.ChannelId is not null && roleTagSettings.ChannelId != ctx.Channel.Id)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The role you tried to select does not exist or can't be tagged in this channel."));
            return;
        }

        if (roleTagSettings.LastTimeUsed > DateTime.UtcNow - TimeSpan.FromMinutes(roleTagSettings.Cooldown))
        {
            TimeSpan remainingTime = TimeSpan.FromMinutes(roleTagSettings.Cooldown) - (DateTime.UtcNow - roleTagSettings.LastTimeUsed);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                $"This role can't be mentioned right now, cooldown has not passed yet. ({remainingTime.Hours} Hours {remainingTime.Minutes} Minutes {remainingTime.Seconds} Seconds left)"));
            return;
        }

        DiscordRole role = ctx.Guild.GetRole(roleTagSettings.RoleId);

        await new DiscordMessageBuilder()
            .WithContent($"{role.Mention} - {ctx.Member.Mention}: {roleTagSettings.Message}")
            .WithAllowedMention(new RoleMention(role))
            .SendAsync(ctx.Channel);

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("Role Tagged"));
        roleTagSettings.LastTimeUsed = DateTime.UtcNow;

        dbContext.RoleTagSettings.Update(roleTagSettings);
        await dbContext.SaveChangesAsync();
    }
}
public sealed class RoleTagAutoCompleteProvider(IDbContextFactory<LiveBotDbContext> dbContextFactory) : IAutoCompleteProvider
{
    public async ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        if (ctx.Guild is null)
        {
            return ReadOnlyDictionary<string, object>.Empty;
        }
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Dictionary<string, object> result = [];
        foreach (RoleTagSettings item in dbContext.RoleTagSettings.Where(w => w.GuildId == ctx.Guild.Id && (w.ChannelId == ctx.Channel.Id || w.ChannelId == null)))
        {
            result.Add($"{(item.LastTimeUsed > DateTime.UtcNow - TimeSpan.FromMinutes(item.Cooldown) ? "(On cooldown) " : "")}{item.Description}", item.Id);
        }
        return result;
    }
}