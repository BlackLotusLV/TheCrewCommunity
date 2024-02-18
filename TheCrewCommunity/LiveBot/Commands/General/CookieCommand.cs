using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.General;

public class CookieCommand(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService)
{
    [Command("cookie"), Description("Gives user a cookie."), DisplayName("Cookie"), RequireGuild]
    public async Task ExecuteAsync(SlashCommandContext ctx, [Description("Member who you want to give the cooky to.")] DiscordMember member)
    {
        if (ctx.Member is null)
        {
            throw new NullReferenceException("Member is null");
        }
        if (ctx.Member == member)
        {
            var response = new DiscordInteractionResponseBuilder()
                .WithContent("You can't give yourself a cookie")
                .AsEphemeral();
            await ctx.RespondAsync(response);
            return;
        }
        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        User giver = await dbContext.Users.FindAsync(ctx.Member.Id) ?? await databaseMethodService.AddUserAsync(new User(ctx.Member.Id));
        User receiver = await dbContext.Users.FindAsync(member.Id) ?? await databaseMethodService.AddUserAsync(new User(member.Id));
        
        if (giver.CookieDate.Date == DateTime.UtcNow.Date)
        {
            var response = new DiscordInteractionResponseBuilder()
                .WithContent($"Your cookie box is empty. You can give a cookie in {24 - DateTime.UtcNow.Hour} Hours, {59 - DateTime.UtcNow.Minute - 1} Minutes, {59 - DateTime.UtcNow.Second} Seconds.")
                .AsEphemeral();
            await ctx.RespondAsync(response);
            return;
        }
        
        giver.CookieDate = DateTime.UtcNow.Date;
        giver.CookiesGiven++;
        receiver.CookiesTaken++;
        
        dbContext.Users.UpdateRange(giver, receiver);
        await dbContext.SaveChangesAsync();

        var followupMessage = new DiscordMessageBuilder()
            .WithContent($"{member.Mention}, {ctx.Member.Mention} has given you a :cookie:")
            .WithAllowedMention(new UserMention());
        await ctx.RespondAsync(followupMessage);
    }
}