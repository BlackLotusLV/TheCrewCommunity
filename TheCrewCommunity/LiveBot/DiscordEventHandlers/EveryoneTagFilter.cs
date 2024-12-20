﻿using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers;

public static partial class EveryoneTagFilter
{
    public static async Task OnMessageCreated(DiscordClient client, MessageCreatedEventArgs e)
    {
        if (e.Author.IsBot || e.Guild is null) return;
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var warningService = client.ServiceProvider.GetRequiredService<IModeratorWarningService>();

        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild? guild = await liveBotDbContext.Guilds.FindAsync(e.Guild.Id);
        DiscordMember member = await e.Guild.GetMemberAsync(e.Author.Id);
        if (
            guild is { ModerationLogChannelId: not null, HasEveryoneProtection: true } &&
            !member.Permissions.HasPermission(DiscordPermission.MentionEveryone) &&
            e.Message.Content.Contains("@everyone") &&
            !EveryoneTagRegex().IsMatch(e.Message.Content)
        )
        {
            var msgDeleted = false;
            try
            {
                await e.Message.DeleteAsync();
            }
            catch (NotFoundException)
            {
                msgDeleted = true;
            }

            if (!msgDeleted)
            {
                await member.TimeoutAsync(DateTimeOffset.UtcNow + TimeSpan.FromHours(1), "Spam protection triggered - everyone tag");
                warningService.AddToQueue(new WarningItem(e.Author, client.CurrentUser, e.Guild, e.Channel, "Tried to tag everyone", true));
            }
        }
    }
    [GeneratedRegex("`[a-zA-Z0-1.,:/ ]{0,}@everyone[a-zA-Z0-1.,:/ ]{0,}`")]
    private static partial Regex EveryoneTagRegex();
}