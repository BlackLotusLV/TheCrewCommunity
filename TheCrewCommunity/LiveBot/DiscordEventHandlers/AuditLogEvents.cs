﻿using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Entities.AuditLogs;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers;

public static class AuditLogEvents
{
    public static async Task OnAuditLogCreated(DiscordClient client, GuildAuditLogCreatedEventArgs eventArgs)
    {
        if (eventArgs?.AuditLogEntry is null) return;
        switch (eventArgs.AuditLogEntry.ActionType)
        {
            case DiscordAuditLogActionType.Ban:
                await BanManager(client, eventArgs.Guild, eventArgs.AuditLogEntry as DiscordAuditLogBanEntry);
                break;
            case DiscordAuditLogActionType.MemberUpdate:
                await TimeOutLogger(client, eventArgs.Guild, eventArgs.AuditLogEntry as DiscordAuditLogMemberUpdateEntry);
                await MuteManager(client, eventArgs.Guild, eventArgs.AuditLogEntry as DiscordAuditLogMemberUpdateEntry);
                break;
            case DiscordAuditLogActionType.Kick:
                await KickManager(client,eventArgs.Guild, eventArgs.AuditLogEntry as DiscordAuditLogKickEntry);
                break;
            case DiscordAuditLogActionType.Unban:
                await UnBanManager(client, eventArgs.Guild, eventArgs.AuditLogEntry as DiscordAuditLogBanEntry);
                break;
            default:
                client.Logger.LogDebug(CustomLogEvents.AuditLogManager,"Audit log entry not handled: {AuditLogEntry}",eventArgs.AuditLogEntry.ActionType);
                break;
        }
    }
    
    private static async Task KickManager(DiscordClient client, DiscordGuild guild, DiscordAuditLogKickEntry? logEntry)
    {
        if (logEntry is null)
        {
            client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Audit log entry for Kick event is null, skipping");
            return;
        }
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        var moderatorLoggingService = client.ServiceProvider.GetRequiredService<IModeratorLoggingService>();
        
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guildSettings = await liveBotDbContext.Guilds.AsNoTracking().FirstOrDefaultAsync(x=>x.Id == guild.Id) ??
                              await databaseMethodService.AddGuildAsync(new Guild(guild.Id));
        if (guildSettings.ModerationLogChannelId is null) return;
        DiscordChannel modLogChannel = await guild.GetChannelAsync(guildSettings.ModerationLogChannelId.Value);
        DiscordUser targetUser = await client.GetUserAsync(logEntry.Target.Id);
        GuildUser guildUser = await liveBotDbContext.GuildUsers.FindAsync(targetUser.Id, guild.Id) ??
                              await databaseMethodService.AddGuildUsersAsync(new GuildUser(targetUser.Id, guild.Id));
        DiscordUser responsibleUser = logEntry.UserResponsible ?? client.CurrentUser;
        guildUser.KickCount++;
        liveBotDbContext.GuildUsers.Update(guildUser);
        await liveBotDbContext.SaveChangesAsync();
        StringBuilder descriptionBuilder = new();
        descriptionBuilder.AppendLine("# 👢 User Kicked");
        descriptionBuilder.AppendLine($"- **User:** {targetUser.Mention}");
        descriptionBuilder.AppendLine($"- **Moderator:** {responsibleUser.Mention}");
        descriptionBuilder.AppendLine($"- **Reason:** {logEntry.Reason}");
        descriptionBuilder.AppendLine($"- **Kick Count:** {guildUser.KickCount}");
        
        moderatorLoggingService.AddToQueue(new ModLogItem(
            modLogChannel,
            targetUser,
            descriptionBuilder.ToString(),
            ModLogType.Kick));
        await databaseMethodService.AddInfractionsAsync(
            new Infraction(
                responsibleUser.Id,
                targetUser.Id,
                guild.Id,
                logEntry.Reason?? "Reason unspecified",
                false,
                InfractionType.Kick)
            );
        client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Kick logged for {User} in {Guild} by {ModUser}",targetUser.Username,guild.Name,responsibleUser.Username);
    }

    private static async Task UnBanManager(DiscordClient client, DiscordGuild guild, DiscordAuditLogBanEntry? logEntry)
    {
        if (logEntry is null)
        {
            client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Audit log entry for Unbanning event is null, skipping");
            return;
        }
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        var moderatorLoggingService = client.ServiceProvider.GetRequiredService<IModeratorLoggingService>();
        
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guildSettings = await liveBotDbContext.Guilds.FindAsync(guild.Id) ??
                              await databaseMethodService.AddGuildAsync(new Guild(guild.Id));
        if (guildSettings.ModerationLogChannelId is null) return;
        DiscordChannel modLogChannel = await guild.GetChannelAsync(guildSettings.ModerationLogChannelId.Value);
        DiscordUser targetUser = await client.GetUserAsync(logEntry.Target.Id);
        DiscordUser responsibleUser = logEntry.UserResponsible ?? client.CurrentUser;
        StringBuilder descriptionBuilder = new();
        descriptionBuilder.AppendLine("# ✅ User Unbanned");
        descriptionBuilder.AppendLine($"- **User:** {targetUser.Mention}");
        descriptionBuilder.AppendLine($"- **Moderator:** {responsibleUser.Mention}");
        moderatorLoggingService.AddToQueue(new ModLogItem(
            modLogChannel,
            targetUser,
            descriptionBuilder.ToString(),
            ModLogType.Unban
            ));
        client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Unban logged for {User} in {Guild} by {ModUser}",targetUser.Username,guild.Name,responsibleUser.Username);
    }

    private static async Task TimeOutLogger(DiscordClient client, DiscordGuild guild, DiscordAuditLogMemberUpdateEntry? logEntry)
    {
        if (logEntry is null)
        {
            client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Audit log entry for Guild member updated event is null, skipping");
            return;
        }
        if (logEntry.TimeoutChange.Before == logEntry.TimeoutChange.After) return;
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        var moderatorLoggingService = client.ServiceProvider.GetRequiredService<IModeratorLoggingService>();
        
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        
        Guild guildSettings= liveBotDbContext.Guilds.First(w => w.Id == guild.Id);
        if (guildSettings.ModerationLogChannelId is null) return;
        DiscordChannel modLogChannel = await guild.GetChannelAsync(guildSettings.ModerationLogChannelId.Value);
        DiscordUser targetUser = await client.GetUserAsync(logEntry.Target.Id);
        DiscordUser responsibleUser = logEntry.UserResponsible ?? client.CurrentUser;
        ModLogType modLogType;
        StringBuilder descriptionBuilder = new();
        StringBuilder reasonBuilder = new();
        reasonBuilder.AppendLine(logEntry.Reason ?? "-reason not specified-");
        var infractionType = InfractionType.TimeoutRemoved;

        DateTimeOffset? newTime = logEntry.TimeoutChange.After;
        DateTimeOffset? oldTime = logEntry.TimeoutChange.Before;
        if (newTime is null && oldTime is not null)
        {
            modLogType = ModLogType.TimeOutRemoved;
            descriptionBuilder.AppendLine("# ⌛ Timeout Removed");
            descriptionBuilder.AppendLine($"- **User:**{targetUser.Mention}");
            descriptionBuilder.AppendLine($"- **by:** {responsibleUser.Mention}");
            descriptionBuilder.AppendLine($"- **old timeout:**<t:{oldTime.Value.ToUnixTimeSeconds()}:F>(<t:{oldTime.Value.ToUnixTimeSeconds()}:R>)");
            reasonBuilder.Append($"- **Old timeout:** <t:{oldTime.Value.ToUnixTimeSeconds()}:F>");
        }
        else if (oldTime < newTime && oldTime > DateTimeOffset.UtcNow)
        {
            modLogType = ModLogType.TimeOutExtended;
            descriptionBuilder.AppendLine("# ⌛ User Timeout Extended");
            descriptionBuilder.AppendLine($"- **User:**{targetUser.Mention}");
            descriptionBuilder.AppendLine($"- **by:** {responsibleUser.Mention}");
            descriptionBuilder.AppendLine($"- **reason:** {logEntry.Reason??"-reason not specified-"}");
            descriptionBuilder.AppendLine($"- **until:**<t:{newTime.Value.ToUnixTimeSeconds()}:F>(<t:{newTime.Value.ToUnixTimeSeconds()}:R>)");
            descriptionBuilder.AppendLine($"- ***old timeout:**<t:{oldTime.Value.ToUnixTimeSeconds()}:F>(<t:{oldTime.Value.ToUnixTimeSeconds()}:R>)*");
            infractionType = InfractionType.TimeoutExtended;
            reasonBuilder.AppendLine($"- **Until:** <t:{newTime.Value.ToUnixTimeSeconds()}:F>)")
                .Append($"- **Old timeout:** <t:{oldTime.Value.ToUnixTimeSeconds()}:F>)");
        }
        else if ((oldTime is null && newTime>DateTimeOffset.UtcNow) || (oldTime < newTime && oldTime<DateTimeOffset.UtcNow))
        {
            modLogType = ModLogType.TimedOut;
            
            descriptionBuilder.AppendLine("# ⌛ User Timed Out");
            descriptionBuilder.AppendLine($"- **User:**{targetUser.Mention}");
            descriptionBuilder.AppendLine($"- **by:** {responsibleUser.Mention}");
            descriptionBuilder.AppendLine($"- **reason:** {logEntry.Reason??"-reason not specified-"}");
            descriptionBuilder.AppendLine($"- **until:**<t:{newTime.Value.ToUnixTimeSeconds()}:F>(<t:{newTime.Value.ToUnixTimeSeconds()}:R>)");
            infractionType =  InfractionType.TimeoutAdded;
            reasonBuilder.Append($"- **Until:** <t:{newTime.Value.ToUnixTimeSeconds()}:F>)");
        }
        else if (oldTime > newTime)
        {
            modLogType = ModLogType.TimeOutShortened;
            descriptionBuilder.AppendLine("# ⌛ User Timeout Shortened");
            descriptionBuilder.AppendLine($"- **User:**{targetUser.Mention}");
            descriptionBuilder.AppendLine($"- **by** {responsibleUser.Mention}");
            descriptionBuilder.AppendLine($"- **reason:** {logEntry.Reason??"-reason not specified-"}");
            descriptionBuilder.AppendLine($"- **until:**<t:{newTime.Value.ToUnixTimeSeconds()}:F>(<t:{newTime.Value.ToUnixTimeSeconds()}:R>)");
            descriptionBuilder.AppendLine($"- ***old timeout:**<t:{oldTime.Value.ToUnixTimeSeconds()}:F>(<t:{oldTime.Value.ToUnixTimeSeconds()}:R>)*");
            infractionType = InfractionType.TimeoutReduced;
            reasonBuilder.AppendLine($"- **Until:** <t:{newTime.Value.ToUnixTimeSeconds()}:F>)")
                .Append($"- **Old timeout:** <t:{oldTime.Value.ToUnixTimeSeconds()}:F>)");
            
        }
        else return;
        moderatorLoggingService.AddToQueue(new ModLogItem(modLogChannel,targetUser,descriptionBuilder.ToString(),modLogType));
        await databaseMethodService.AddInfractionsAsync(
            new Infraction(responsibleUser.Id, targetUser.Id, guild.Id, reasonBuilder.ToString(),
                false, infractionType));
        client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"{ModLogType} logged for {User} in {Guild} by {ModUser}",modLogType,targetUser.Username,guild.Name,responsibleUser.Username);
    }
    
    private static async Task BanManager(DiscordClient client, DiscordGuild guild, DiscordAuditLogBanEntry? logEntry)
    {
        if (logEntry is null)
        {
            client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Audit log entry for Ban event is null, skipping");
            return;
        }
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        var moderatorLoggingService = client.ServiceProvider.GetRequiredService<IModeratorLoggingService>();
        
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        GuildUser guildUser = await liveBotDbContext.GuildUsers.FindAsync(logEntry.Target.Id, guild.Id) ??
                              await databaseMethodService.AddGuildUsersAsync(new GuildUser(logEntry.Target.Id, guild.Id));
        guildUser.BanCount++;
        liveBotDbContext.Update(guildUser);
        await liveBotDbContext.SaveChangesAsync();
        
        Guild guildSettings = await liveBotDbContext.Guilds.FirstOrDefaultAsync(w => w.Id == guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(guild.Id));
        if (guildSettings.ModerationLogChannelId is null) return;
        DiscordChannel modLogChannel = await guild.GetChannelAsync(guildSettings.ModerationLogChannelId.Value);
        DiscordUser targetUser = await client.GetUserAsync(logEntry.Target.Id);
        DiscordUser responsibleUser = logEntry.UserResponsible ?? client.CurrentUser;
        StringBuilder descriptionBuilder = new();
        descriptionBuilder.AppendLine("# 🚫 User Banned");
        descriptionBuilder.AppendLine($"- **User:** {targetUser.Mention}");
        descriptionBuilder.AppendLine($"- **Moderator:** {responsibleUser.Mention}");
        descriptionBuilder.AppendLine($"- **Reason:** {logEntry.Reason}");
        descriptionBuilder.AppendLine($"- **Ban Count:** {guildUser.BanCount}");
        moderatorLoggingService.AddToQueue(new ModLogItem(
            modLogChannel,
            targetUser,
            descriptionBuilder.ToString(),
            ModLogType.Ban
            ));
        await databaseMethodService.AddInfractionsAsync(
            new Infraction(responsibleUser.Id,targetUser.Id,guild.Id,logEntry.Reason??"Reason unspecified",false,InfractionType.Ban)
            );
        client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Ban logged for {User} in {Guild} by {ModUser}",targetUser.Username,guild.Name,responsibleUser.Username);
    }

    private static async Task MuteManager(DiscordClient client, DiscordGuild guild, DiscordAuditLogMemberUpdateEntry? logEntry)
    {
        if (logEntry is null)
        {
            client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Audit log entry for Guild member updated event is null, skipping");
            return;
        }
        if (logEntry.MuteChange.Before == logEntry.MuteChange.After || !logEntry.MuteChange.After.HasValue || !logEntry.MuteChange.Before.HasValue) return;
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        var moderatorLoggingService = client.ServiceProvider.GetRequiredService<IModeratorLoggingService>();
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guildSettings = await liveBotDbContext.Guilds.FirstOrDefaultAsync(w => w.Id == guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(guild.Id));
        if (guildSettings.ModerationLogChannelId is null) return;
        DiscordChannel modLogChannel = await guild.GetChannelAsync(guildSettings.ModerationLogChannelId.Value);
        DiscordUser targetUser = await client.GetUserAsync(logEntry.Target.Id);
        DiscordUser responsibleUser = logEntry.UserResponsible ?? client.CurrentUser;
        StringBuilder descriptionBuilder = new();
        var type = ModLogType.Muted;
        switch (logEntry.MuteChange.Before.Value)
        {
            case true when !logEntry.MuteChange.After.Value:
                type = ModLogType.UnMuted;
                descriptionBuilder.AppendLine("# 🔊 User Un-muted");
                break;
            case false when logEntry.MuteChange.After.Value:
                descriptionBuilder.AppendLine("# 🔇 User Muted");
                break;
        }
        descriptionBuilder.AppendLine($"- **User:** {targetUser.Mention}");
        descriptionBuilder.AppendLine($"- **Moderator:** {responsibleUser.Mention}");
        moderatorLoggingService.AddToQueue(new ModLogItem(modLogChannel, targetUser, descriptionBuilder.ToString(), type));
    }
}