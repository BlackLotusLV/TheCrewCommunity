using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Entities.AuditLogs;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.EventHandlers;

public class AuditLogEvents(IModeratorLoggingService moderatorLoggingService, IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService)
{
    public async Task OnAuditLogCreated(DiscordClient client, GuildAuditLogCreatedEventArgs eventArgs)
    {
        if (eventArgs?.AuditLogEntry is null) return;
        switch (eventArgs.AuditLogEntry.ActionType)
        {
            case DiscordAuditLogActionType.Ban:
                await BanManager(client, eventArgs.Guild, eventArgs.AuditLogEntry as DiscordAuditLogBanEntry);
                break;
            case DiscordAuditLogActionType.MemberUpdate:
                await TimeOutLogger(client, eventArgs.Guild, eventArgs.AuditLogEntry as DiscordAuditLogMemberUpdateEntry);
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
    
    private async Task KickManager(DiscordClient client, DiscordGuild guild, DiscordAuditLogKickEntry? logEntry)
    {
        if (logEntry is null)
        {
            client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Audit log entry for Kick event is null, skipping");
            return;
        }
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guildSettings = await liveBotDbContext.Guilds.AsNoTracking().FirstOrDefaultAsync(x=>x.Id == guild.Id) ??
                              await databaseMethodService.AddGuildAsync(new Guild(guild.Id));
        if (guildSettings.ModerationLogChannelId is null) return;
        DiscordChannel modLogChannel = guild.GetChannel(guildSettings.ModerationLogChannelId.Value);
        DiscordUser targetUser = await client.GetUserAsync(logEntry.Target.Id);
        GuildUser guildUser = await liveBotDbContext.GuildUsers.FindAsync(targetUser.Id, guild.Id) ??
                              await databaseMethodService.AddGuildUsersAsync(new GuildUser(targetUser.Id, guild.Id));
        DiscordUser responsibleUser = logEntry.UserResponsible ?? client.CurrentUser;
        guildUser.KickCount++;
        liveBotDbContext.GuildUsers.Update(guildUser);
        await liveBotDbContext.SaveChangesAsync();
        
        moderatorLoggingService.AddToQueue(new ModLogItem(
            modLogChannel,
            targetUser,
            "# User Kicked\n" +
            $"- **User:** {targetUser.Mention}\n" +
            $"- **Moderator:** {responsibleUser.Mention}\n" +
            $"- **Reason:** {logEntry.Reason}\n" +
            $"- **Kick Count:** {guildUser.KickCount}",
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

    private async Task UnBanManager(DiscordClient client, DiscordGuild guild, DiscordAuditLogBanEntry? logEntry)
    {
        if (logEntry is null)
        {
            client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Audit log entry for Unbanning event is null, skipping");
            return;
        }
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guildSettings = await liveBotDbContext.Guilds.FindAsync(guild.Id) ??
                              await databaseMethodService.AddGuildAsync(new Guild(guild.Id));
        if (guildSettings.ModerationLogChannelId is null) return;
        DiscordChannel modLogChannel = guild.GetChannel(guildSettings.ModerationLogChannelId.Value);
        DiscordUser targetUser = await client.GetUserAsync(logEntry.Target.Id);
        DiscordUser responsibleUser = logEntry.UserResponsible ?? client.CurrentUser;
        moderatorLoggingService.AddToQueue(new ModLogItem(
            modLogChannel,
            targetUser,
            "# User Unbanned\n" +
            $"- **User:** {targetUser.Mention}\n" +
            $"- **Moderator:** {responsibleUser.Mention}\n",
            ModLogType.Unban
            ));
        client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Unban logged for {User} in {Guild} by {ModUser}",targetUser.Username,guild.Name,responsibleUser.Username);
    }

    private async Task TimeOutLogger(DiscordClient client, DiscordGuild guild, DiscordAuditLogMemberUpdateEntry? logEntry)
    {
        if (logEntry is null)
        {
            client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Audit log entry for Timeout event is null, skipping");
            return;
        }
        if (logEntry.TimeoutChange.Before == logEntry.TimeoutChange.After) return;
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        
        Guild guildSettings= liveBotDbContext.Guilds.First(w => w.Id == guild.Id);
        if (guildSettings.ModerationLogChannelId is null) return;
        DiscordChannel modLogChannel = guild.GetChannel(guildSettings.ModerationLogChannelId.Value);
        DiscordUser targetUser = await client.GetUserAsync(logEntry.Target.Id);
        DiscordUser responsibleUser = logEntry.UserResponsible ?? client.CurrentUser;
        ModLogType modLogType;
        string description;
        StringBuilder reasonBuilder = new();
        reasonBuilder.AppendLine(logEntry.Reason ?? "-reason not specified-");
        var infractionType = InfractionType.TimeoutRemoved;

        DateTimeOffset? newTime = logEntry.TimeoutChange.After;
        DateTimeOffset? oldTime = logEntry.TimeoutChange.Before;
        if (newTime is null && oldTime is not null)
        {
            modLogType = ModLogType.TimeOutRemoved;
            description ="# Timeout Removed\n" +
                         $"- **User:**{targetUser.Mention}\n" +
                         $"- **by:** {responsibleUser.Mention}\n" +
                         $"- **old timeout:**<t:{oldTime.Value.ToUnixTimeSeconds()}:F>(<t:{oldTime.Value.ToUnixTimeSeconds()}:R>)";
            reasonBuilder.Append($"- **Old timeout:** <t:{oldTime.Value.ToUnixTimeSeconds()}:F>");
        }
        else if (oldTime < newTime && oldTime > DateTimeOffset.UtcNow)
        {
            modLogType = ModLogType.TimeOutExtended;
            description = $"# User Timeout Extended\n" +
                          $"- **User:**{targetUser.Mention}\n" +
                          $"- **by:** {responsibleUser.Mention}\n" +
                          $"- **reason:** {logEntry.Reason??"-reason not specified-"}\n" +
                          $"- **until:**<t:{newTime.Value.ToUnixTimeSeconds()}:F>(<t:{newTime.Value.ToUnixTimeSeconds()}:R>)\n" +
                          $"- ***old timeout:**<t:{oldTime.Value.ToUnixTimeSeconds()}:F>(<t:{oldTime.Value.ToUnixTimeSeconds()}:R>)*";
            infractionType = InfractionType.TimeoutExtended;
            reasonBuilder.AppendLine($"- **Until:** <t:{newTime.Value.ToUnixTimeSeconds()}:F>)")
                .Append($"- **Old timeout:** <t:{oldTime.Value.ToUnixTimeSeconds()}:F>)");
        }
        else if ((oldTime is null && newTime>DateTimeOffset.UtcNow) || (oldTime < newTime && oldTime<DateTimeOffset.UtcNow))
        {
            modLogType = ModLogType.TimedOut;
            description ="# User Timed Out\n" +
                         $"- **User:**{targetUser.Mention}\n" +
                         $"- **by:** {responsibleUser.Mention}\n" +
                         $"- **reason:** {logEntry.Reason??"-reason not specified-"}\n" +
                         $"- **until:**<t:{newTime.Value.ToUnixTimeSeconds()}:F>(<t:{newTime.Value.ToUnixTimeSeconds()}:R>)";
            infractionType =  InfractionType.TimeoutAdded;
            reasonBuilder.Append($"- **Until:** <t:{newTime.Value.ToUnixTimeSeconds()}:F>)");
        }
        else if (oldTime > newTime)
        {
            modLogType = ModLogType.TimeOutShortened;
            description = $"# User Timeout Shortened\n" +
                          $"- **User:**{targetUser.Mention}\n" +
                          $"- **by** {responsibleUser.Mention}\n" +
                          $"- **reason:** {logEntry.Reason??"-reason not specified-"}\n" +
                          $"- **until:**<t:{newTime.Value.ToUnixTimeSeconds()}:F>(<t:{newTime.Value.ToUnixTimeSeconds()}:R>)\n" +
                          $"- ***old timeout:**<t:{oldTime.Value.ToUnixTimeSeconds()}:F>(<t:{oldTime.Value.ToUnixTimeSeconds()}:R>)*";
            infractionType = InfractionType.TimeoutReduced;
            reasonBuilder.AppendLine($"- **Until:** <t:{newTime.Value.ToUnixTimeSeconds()}:F>)")
                .Append($"- **Old timeout:** <t:{oldTime.Value.ToUnixTimeSeconds()}:F>)");
            
        }
        else return;
        moderatorLoggingService.AddToQueue(new ModLogItem(modLogChannel,targetUser,description,modLogType));
        await databaseMethodService.AddInfractionsAsync(
            new Infraction(responsibleUser.Id, targetUser.Id, guild.Id, reasonBuilder.ToString(),
                false, infractionType));
        client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"{ModLogType} logged for {User} in {Guild} by {ModUser}",modLogType,targetUser.Username,guild.Name,responsibleUser.Username);
    }
    
    private async Task BanManager(DiscordClient client, DiscordGuild guild, DiscordAuditLogBanEntry? logEntry)
    {
        if (logEntry is null)
        {
            client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Audit log entry for Ban event is null, skipping");
            return;
        }
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        GuildUser guildUser = await liveBotDbContext.GuildUsers.FindAsync(logEntry.Target.Id, guild.Id) ??
                              await databaseMethodService.AddGuildUsersAsync(new GuildUser(logEntry.Target.Id, guild.Id));
        guildUser.BanCount++;
        liveBotDbContext.Update(guildUser);
        await liveBotDbContext.SaveChangesAsync();
        
        Guild guildSettings = await liveBotDbContext.Guilds.FirstOrDefaultAsync(w => w.Id == guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(guild.Id));
        if (guildSettings.ModerationLogChannelId is null) return;
        DiscordChannel modLogChannel = guild.GetChannel(guildSettings.ModerationLogChannelId.Value);
        DiscordUser targetUser = await client.GetUserAsync(logEntry.Target.Id);
        DiscordUser responsibleUser = logEntry.UserResponsible ?? client.CurrentUser;
        moderatorLoggingService.AddToQueue(new ModLogItem(
            modLogChannel,
            targetUser,
            "# User Banned\n" +
            $"- **User:** {targetUser.Mention}\n" +
            $"- **Moderator:** {responsibleUser.Mention}\n" +
            $"- **Reason:** {logEntry.Reason}\n" +
            $"- **Ban Count:** {guildUser.BanCount}",
            ModLogType.Ban
            ));
        await databaseMethodService.AddInfractionsAsync(
            new Infraction(responsibleUser.Id,targetUser.Id,guild.Id,logEntry.Reason??"Reason unspecified",false,InfractionType.Ban)
            );
        client.Logger.LogInformation(CustomLogEvents.AuditLogManager,"Ban logged for {User} in {Guild} by {ModUser}",targetUser.Username,guild.Name,responsibleUser.Username);
    }
}