using System.Text;
using DSharpPlus;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.Services;

public interface IModeratorWarningService
{
    public void StartService(DiscordClient client);
    public void StopService();
    public void AddToQueue(WarningItem value);
    public Task RemoveWarningAsync(DiscordUser user, SlashCommandContext ctx, int warningId);
    Task<DiscordEmbed> GetUserInfoAsync(DiscordGuild guild, DiscordUser user);
    Task<List<DiscordEmbed>> BuildInfractionsEmbedsAsync(DiscordGuild guild, DiscordUser user, bool adminCommand = false);
    string UserInfoButtonPrefix { get; }
    string InfractionButtonPrefix { get; }
}

public class ModeratorWarningService(
    IDbContextFactory<LiveBotDbContext> dbContextFactory,
    IDatabaseMethodService databaseMethodService,
    ILoggerFactory loggerFactory,
    IModeratorLoggingService moderatorLoggingService)
    : BaseQueueService<WarningItem>(dbContextFactory, databaseMethodService, loggerFactory), IModeratorWarningService
{
    private const string _infractionButtonPrefix = "GetInfractions-";
    private const string _userInfoButtonPrefix = "GetUserInfo-";
    public string InfractionButtonPrefix { get; } = _infractionButtonPrefix;
    public string UserInfoButtonPrefix { get; } = _userInfoButtonPrefix;

    private const string NotConfiguredMessage = "This server has not set up this feature!";
    private const string KickMessage = "Due to you exceeding the Infraction threshold, you have been kicked";
    private const string BanMessage = "Due to you exceeding the Infraction threshold, you have been banned";
    private const string AutoModeratorMessage = "This message was sent by Auto Moderator, contact staff if you think this is a mistake";

    private protected override async Task ProcessQueueItem(WarningItem item)
    {
        await using LiveBotDbContext dbContext = await DbContextFactory.CreateDbContextAsync();
        Guild guild = await dbContext.Guilds.FindAsync(item.Guild.Id) ?? await DatabaseMethodService.AddGuildAsync(new Guild(item.Guild.Id));
        DiscordMember? member = await TryGetMember(item.Guild, item.User);

        if (member is null && !item.AutoMessage)
        {
            await item.Channel.SendMessageAsync($"User {item.User.Username}*({item.User.Id})* is no longer in the server.");
        }

        bool kick = false, ban = false;
        if (guild.ModerationLogChannelId == null)
        {
            if (item.InteractionContext == null)
            {
                await item.Channel.SendMessageAsync(NotConfiguredMessage);
            }
            else
            {
                await item.InteractionContext.EditResponseAsync(new DiscordWebhookBuilder().WithContent(NotConfiguredMessage));
            }

            return;
        }

        DiscordChannel modLog = item.Guild.GetChannel(Convert.ToUInt64(guild.ModerationLogChannelId));

        Infraction newInfraction = new(item.Admin.Id, item.User.Id, item.Guild.Id, item.Reason, true, InfractionType.Warning);
        await DatabaseMethodService.AddInfractionsAsync(newInfraction);

        int warningCount = await dbContext.Infractions.CountAsync(w => w.UserId == item.User.Id && w.GuildId == item.Guild.Id && w.InfractionType == InfractionType.Warning);
        int infractionLevel = await dbContext.Infractions.CountAsync(w => w.UserId == item.User.Id && w.GuildId == item.Guild.Id && w.InfractionType == InfractionType.Warning && w.IsActive);
        DiscordEmbedBuilder embedToUser = new()
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor()
            {
                Name = item.Guild.Name,
                IconUrl = item.Guild.IconUrl
            },
            Title = "You have been warned!"
        };
        embedToUser.AddField("Reason", item.Reason);
        embedToUser.AddField("Infraction Level", $"{infractionLevel}", true);
        embedToUser.AddField("Warning by", $"{item.Admin.Mention}", true);
        embedToUser.AddField("Server", item.Guild.Name, true);

        string warningDescription = "# User Warned\n" +
                                    $"- **User:** {item.User.Mention}\n" +
                                    $"- **Infraction level:** {infractionLevel}\n" +
                                    $"- **Infractions:** {warningCount}\n" +
                                    $"- **Moderator:** {item.Admin.Mention}\n" +
                                    $"- **Reason:** {item.Reason}\n" +
                                    $"*Infraction ID: {newInfraction.Id}*";

        switch (infractionLevel)
        {
            case > 4:
                embedToUser.AddField("Banned", BanMessage);
                ban = true;
                break;
            case > 2:
                embedToUser.AddField("Kicked", KickMessage);
                kick = true;
                break;
        }

        if (item.AutoMessage)
        {
            embedToUser.WithFooter(AutoModeratorMessage);
        }
        
        if (member is not null)
        {
            await TrySendInfractionMessage(member, embedToUser.Build());
        }
        
        if (kick && member is not null)
        {
            await member.RemoveAsync("Exceeded warning limit!");
        }

        if (ban)
        {
            await item.Guild.BanMemberAsync(item.User.Id, 0, "Exceeded warning limit!");
        }

        moderatorLoggingService.AddToQueue(new ModLogItem(modLog, item.User, warningDescription, ModLogType.Warning, "",item.Attachment));

        if (item.InteractionContext == null)
        {
            DiscordMessage info = await item.Channel.SendMessageAsync($"{item.User.Username}, Has been warned!");
            await Task.Delay(10000);
            await info.DeleteAsync();
        }
        else
        {
            await item.InteractionContext.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent(
                    $"{item.Admin.Mention}, The user {item.User.Mention}({item.User.Id}) has been warned. Please check the log for additional info."));
            await Task.Delay(10000);
            await item.InteractionContext.DeleteResponseAsync();
        }
    }
    public async Task RemoveWarningAsync(DiscordUser user, SlashCommandContext ctx, int warningId)
        {
            if (ctx.Guild is null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This command can only be used in a guild channel"));
                return;
            }
            await using LiveBotDbContext liveBotDbContext = await DbContextFactory.CreateDbContextAsync();
            Guild? guild = await liveBotDbContext.Guilds.FindAsync(ctx.Guild.Id);
            var infractions = await liveBotDbContext.Infractions.Where(w => ctx.Guild.Id == w.GuildId && user.Id == w.UserId && w.InfractionType == InfractionType.Warning && w.IsActive).ToListAsync();
            int infractionLevel = infractions.Count;

            if (infractionLevel == 0)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This user does not have any infractions that are active, did you provide the correct user?"));
                return;
            }

            StringBuilder modMessageBuilder = new();
            DiscordMember? member = null;
            if (guild?.ModerationLogChannelId == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This server has not set up this feature."));
                return;
            }

            try
            {
                member = await ctx.Guild.GetMemberAsync(user.Id);
            }
            catch (Exception)
            {
                modMessageBuilder.AppendLine($"{user.Mention} is no longer in the server.");
            }

            DiscordChannel modLog = ctx.Guild.GetChannel(Convert.ToUInt64(guild.ModerationLogChannelId));
            Infraction? entry = infractions.FirstOrDefault(f => f.IsActive && f.Id == warningId);
            entry ??= infractions.Where(f => f.IsActive).OrderBy(f => f.Id).First();
            entry.IsActive = false;

            liveBotDbContext.Infractions.Update(entry);
            await liveBotDbContext.SaveChangesAsync();
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Infraction #{entry.Id} deactivated for {user.Username}({user.Id})"));
            
            string description = $"# User Warning Removed\n" +
                                 $"- **User:** {user.Mention}\n" +
                                 $"- **Infraction ID:** {entry.Id}\n" +
                                 $"- **Infraction level:** {infractionLevel - 1}\n" +
                                 $"- **Moderator:** {ctx.User.Mention}\n";
            try
            {
                if (member is not null) await member.SendMessageAsync($"Your infraction level in **{ctx.Guild.Name}** has been lowered to {infractionLevel - 1} by {ctx.User.Mention}");
            }
            catch
            {
                modMessageBuilder.AppendLine($"{user.Mention} could not be contacted via DM.");
            }

            moderatorLoggingService.AddToQueue(new ModLogItem(modLog, user, description, ModLogType.UnWarn, modMessageBuilder.ToString()));
        }

        public async Task<DiscordEmbed> GetUserInfoAsync(DiscordGuild guild, DiscordUser user)
        {
            DiscordEmbedBuilder embedBuilder = new()
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = user.Username,
                    IconUrl = user.AvatarUrl
                },
                Title = $"{user.Username} Info",
                ImageUrl = user.AvatarUrl,
                Url = $"https://discordapp.com/users/{user.Id}"
            };
            DiscordMember? member = null;
            try
            {
                member = await guild.GetMemberAsync(user.Id);
            }
            catch (NotFoundException)
            {
                Logger.LogDebug("Moderator tried to get info on user {User} in {Guild} but they are not in the server", user.Username, guild.Name);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to get member in GetUserInfoAsync");
            }
            embedBuilder
                .AddField("Nickname", member is null ? "*User not in this server*" : member.Username??"*None*", true)
                .AddField("ID", user.Id.ToString(), true)
                .AddField("Account Created On", $"<t:{user.CreationTimestamp.ToUnixTimeSeconds()}:F>")
                .AddField("Server Join Date", member is null ? "User not in this server" : $"<t:{member.JoinedAt.ToUnixTimeSeconds()}:F>")
                .AddField("Accepted rules?", member is null ? "User not in this server" : member.IsPending == null ? "Guild has no member screening" : member.IsPending.Value ? "No" : "Yes");
            return embedBuilder.Build();
        }

        public async Task<List<DiscordEmbed>> BuildInfractionsEmbedsAsync(DiscordGuild guild, DiscordUser user, bool adminCommand = false)
        {
            await using LiveBotDbContext liveBotDbContext = await DbContextFactory.CreateDbContextAsync();
            GuildUser userStats = await liveBotDbContext.GuildUsers.FindAsync(user.Id, guild.Id) ?? await DatabaseMethodService.AddGuildUsersAsync(new GuildUser(user.Id, guild.Id));
            int kickCount = userStats.KickCount;
            int banCount = userStats.BanCount;
            var userInfractions = await liveBotDbContext.Infractions.Where(w => w.UserId == user.Id && w.GuildId == guild.Id).OrderBy(w => w.TimeCreated).ToListAsync();
            if (!adminCommand)
            {
                userInfractions.RemoveAll(w => w.InfractionType == InfractionType.Note);
            }
            DiscordEmbedBuilder statsEmbed = new()
            {
                Color = new DiscordColor(0xFF6600),
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = $"{user.Username}({user.Id})",
                    IconUrl = user.AvatarUrl
                },
                Description = $"- **Times warned:** {userInfractions.Count(w => w.InfractionType == InfractionType.Warning)}\n" +
                              $"- **Times kicked:** {kickCount}\n" +
                              $"- **Times banned:** {banCount}\n" +
                              $"- **Infraction level:** {userInfractions.Count(w => w.IsActive)}\n" +
                              $"- **Infraction count:** {userInfractions.Count(w => w.IsActive)}\n" +
                              $"- **Mod Mail blocked:** {(userStats.IsModMailBlocked?"Yes":"No")}",
                Title = "Infraction History",
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = user.AvatarUrl,
                }
            };
            const int pageCap = 6;
            DiscordEmbedBuilder infractionEmbed = new()
            {
                Color = new DiscordColor(0xFF6600)
            };
            List<DiscordEmbed> embeds = [statsEmbed.Build()];

            var infractions = new string[(int)Math.Ceiling((double)userInfractions.Count / pageCap)];
            for (var i = 0; i < userInfractions.Count; i+=pageCap)
            {
                StringBuilder reason = new();
                for (int j = i; j < i + pageCap && j < userInfractions.Count; j++)
                {
                    Infraction infraction = userInfractions[j];
                    reason.AppendLine($"### {GetReasonTypeEmote(infraction.InfractionType)}Infraction #{infraction.Id} *({infraction.InfractionType.ToString()})*\n" +
                                      $"- **By:** <@{infraction.AdminDiscordId}>\n" +
                                      $"- **Date:** <t:{infraction.TimeCreated.ToUnixTimeSeconds()}>\n" +
                                      $"- **Reason:** {infraction.Reason}");
                    if (infraction.InfractionType == InfractionType.Warning)
                    {
                        reason.AppendLine($"- **Is active:** {(infraction.IsActive ? "✅" : "❌")}");
                    }
                }
                infractions[i/pageCap]=reason.ToString();
                infractionEmbed
                    .WithDescription(reason.ToString())
                    .WithTitle($"Infraction History ({i/pageCap+1}/{infractions.Length})");
                embeds.Add(infractionEmbed.Build());
            }

            return embeds;
        }
        private static string GetReasonTypeEmote(InfractionType infractionType)
        {
            return infractionType switch
            {
                InfractionType.Ban => "[🔨]",
                InfractionType.Kick => "[👢]",
                InfractionType.Note => "[📝]",
                InfractionType.Warning => "[⚠️]",
                InfractionType.TimeoutAdded => "[⏳]",
                InfractionType.TimeoutRemoved => "[⌛]",
                InfractionType.TimeoutReduced => "[⏳]",
                InfractionType.TimeoutExtended => "[⏳]",
                _ => "❓"
            };
        }

    private async Task<string> TrySendInfractionMessage(DiscordMember member, DiscordEmbed embed)
    {
        try
        {
            await member.SendMessageAsync(embed: embed);
            return string.Empty;
        }
        catch (Exception)
        {
            return $":exclamation:{member.Username}*({member.Id})* could not be contacted via DM. Reason not sent";
        }
    }

    private async Task<DiscordMember?> TryGetMember(DiscordGuild guild, SnowflakeObject user)
    {
        try
        {
            return await guild.GetMemberAsync(user.Id);
        }
        catch (NotFoundException e)
        {
            Logger.LogDebug(CustomLogEvents.LiveBot, e, "User {User} not found in guild {Guild}", user.Id, guild.Name);
            return null;
        }
        catch (Exception e)
        {
            Logger.LogError(CustomLogEvents.LiveBot,e, "Failed to get user {User} in guild {Guild}", user.Id, guild.Name);
            return null;
        }
    }
}
public class WarningItem(
    DiscordUser user,
    DiscordUser admin,
    DiscordGuild server,
    DiscordChannel channel,
    string reason,
    bool autoMessage,
    SlashCommandContext? interactionContext = null,
    DiscordAttachment? attachment = null)
{
    public DiscordUser User { get; set; } = user;
    public DiscordUser Admin { get; set; } = admin;
    public DiscordGuild Guild { get; set; } = server;
    public DiscordChannel Channel { get; set; } = channel;
    public string Reason { get; set; } = reason;
    public bool AutoMessage { get; set; } = autoMessage;
    public SlashCommandContext? InteractionContext { get; set; } = interactionContext;
    public DiscordAttachment? Attachment { get; set; } = attachment;
}