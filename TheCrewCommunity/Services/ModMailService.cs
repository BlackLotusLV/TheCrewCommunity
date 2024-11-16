using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.Services;

public interface IModMailService
{
    public int TimeoutMinutes { get; }
    Task ProcessModMailDm(DiscordClient client, MessageCreatedEventArgs e, ModMail mmEntry);
    Task CloseModMailAsync(DiscordClient client, ModMail modMail, DiscordUser closer, string closingText, string closingTextToUser);
    Task CloseButton(DiscordClient client, ComponentInteractionCreatedEventArgs e);
    Task OpenButton(DiscordClient client, ComponentInteractionCreatedEventArgs e);
    public string CloseButtonPrefix { get; }
    public string OpenButtonPrefix { get; }
}

public class ModMailService(IDbContextFactory<LiveBotDbContext> dbContextFactory, IDatabaseMethodService databaseMethodService, ILoggerFactory loggerFactory)
    : IModMailService
{
    private readonly ILogger<ModMailService> _logger = loggerFactory.CreateLogger<ModMailService>();
    public int TimeoutMinutes => 120;

    public string CloseButtonPrefix => "closeModMail";
    public string OpenButtonPrefix => "openModMail";

    public async Task ProcessModMailDm(DiscordClient client, MessageCreatedEventArgs e, ModMail mmEntry)
    {
        DiscordGuild guild = client.Guilds.First(w => w.Value.Id == mmEntry.GuildId).Value;
        DiscordMessageBuilder messageBuilder = new();
        List<DiscordEmbed> attachmentEmbeds = [];
        StringBuilder descriptionBuilder = new(e.Message.Content + "\n\n");
        DiscordEmbedBuilder embedBuilder = new()
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                IconUrl = e.Author.AvatarUrl,
                Name = $"{e.Author.Username} ({e.Author.Id})"
            },
            Color = new DiscordColor(mmEntry.ColorHex),
            Title = $"[INBOX] #{mmEntry.Id} Mod Mail user message."
        };

        if (e.Message.Attachments != null)
        {
            foreach (DiscordAttachment attachment in e.Message.Attachments)
            {
                if (attachment.Url is null)
                {
                    client.Logger.LogDebug(CustomLogEvents.ModMail, "Attachment URL was null in Mod Mail message from {Username}({UserId})", e.Author.Username, e.Author.Id);
                    continue;
                }

                switch (attachment.MediaType)
                {
                    case null:
                        client.Logger.LogDebug(CustomLogEvents.ModMail, "Attachment type was null in Mod Mail message from {Username}({UserId})", e.Author.Username, e.Author.Id);
                        descriptionBuilder.AppendLine($"**Unknown Attachment:** {attachment.Url}");
                        break;
                    case var temp when temp.Contains("image"):
                        if (embedBuilder.ImageUrl is null)
                        {
                            embedBuilder.ImageUrl = attachment.Url;
                        }
                        else
                        {
                            attachmentEmbeds.Add(new DiscordEmbedBuilder()
                            {
                                Color = new DiscordColor(mmEntry.ColorHex),
                                ImageUrl = attachment.Url
                            }.Build());
                        }
                        break;
                    case var temp when temp.Contains("text"):
                        descriptionBuilder.AppendLine($"**Text Attachment:** [Link to file]({attachment.Url})");
                        break;
                    default:
                        client.Logger.LogDebug(CustomLogEvents.ModMail, "Attachment type was not image or file in Mod Mail message from {Username}({UserId})", e.Author.Username, e.Author.Id);
                        descriptionBuilder.AppendLine($"**Other type Attachment:** {attachment.Url}");
                        break;
                }
            }
        }
        
        embedBuilder.WithDescription(descriptionBuilder.ToString());

        attachmentEmbeds.Insert(0, embedBuilder.Build());
        
        messageBuilder.AddEmbeds(attachmentEmbeds);

        mmEntry.HasChatted = true;
        mmEntry.LastMessageTime = DateTime.UtcNow;

        LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        liveBotDbContext.ModMail.Update(mmEntry);
        await liveBotDbContext.SaveChangesAsync();


        ulong? modMailChannelId = liveBotDbContext.Guilds.First(w => w.Id == mmEntry.GuildId).ModMailChannelId;
        if (modMailChannelId is not null)
        {
            DiscordChannel modMailChannel = await guild.GetChannelAsync(modMailChannelId.Value);
            await messageBuilder.SendAsync(modMailChannel);

            client.Logger.LogInformation(CustomLogEvents.ModMail, "New Mod Mail message sent to {ChannelName}({ChannelId}) in {GuildName} from {Username}({UserId})", modMailChannel.Name,
                modMailChannel.Id, modMailChannel.Guild.Name, e.Author.Username, e.Author.Id);
        }
    }

    public async Task CloseModMailAsync(DiscordClient client,ModMail modMail, DiscordUser closer, string closingText, string closingTextToUser)
    {
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        modMail.IsActive = false;
        var notificationMessage = string.Empty;
        DiscordGuild guild = await client.GetGuildAsync(modMail.GuildId);
        Guild dbGuild = await liveBotDbContext.Guilds.FindAsync(guild.Id) ?? (await liveBotDbContext.Guilds.AddAsync(new Guild(guild.Id))).Entity;
        if (dbGuild.ModMailChannelId == null)
        {
            client.Logger.LogWarning("User tried to close mod mail, mod mail channel was not found. Something is set up incorrectly. Server ID:{ServerId}",guild.Id);
            return;
        }
        DiscordChannel modMailChannel = await guild.GetChannelAsync(dbGuild.ModMailChannelId.Value);
        DiscordEmbedBuilder embed = new()
        {
            Title = $"[CLOSED] #{modMail.Id} {closingText}",
            Color = new DiscordColor(modMail.ColorHex),
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                Name = $"{closer.Username} ({closer.Id})",
                IconUrl = closer.AvatarUrl
            }
        };
        try
        {
            DiscordMember member = await guild.GetMemberAsync(modMail.UserDiscordId);
            await member.SendMessageAsync(closingTextToUser);
        }
        catch
        {
            notificationMessage = "User could not be contacted anymore, either blocked the bot, left the server or turned off DMs";
        }

        liveBotDbContext.ModMail.Update(modMail);
        await liveBotDbContext.SaveChangesAsync();
        await modMailChannel.SendMessageAsync(notificationMessage, embed: embed);
        _logger.LogInformation(CustomLogEvents.ModMail,"Mod Mail #{ModMailId} closed by {Username}({UserId}) in {GuildName}",modMail.Id,closer.Username,closer.Id,guild.Name);
    }

    public async Task CloseButton(DiscordClient client, ComponentInteractionCreatedEventArgs e)
    {
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        if (e.Interaction.Type != DiscordInteractionType.Component || e.Interaction.User.IsBot || !e.Interaction.Data.CustomId.Contains(CloseButtonPrefix))return;
        ModMail? mmEntry = await liveBotDbContext.ModMail.FindAsync(Convert.ToInt64(e.Interaction.Data.CustomId.Replace(CloseButtonPrefix, "")));
        DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new();
        if (e.Message.Embeds.Count>0)
        {
            discordInteractionResponseBuilder.AddEmbeds(e.Message.Embeds);
        }
        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, discordInteractionResponseBuilder.WithContent(e.Message.Content));
        if (mmEntry is not { IsActive: true }) return;
        await CloseModMailAsync(
            client,
            mmEntry,
            e.Interaction.User,
            $" Mod Mail closed by {e.Interaction.User.Username}",
            $"**Mod Mail closed by {e.Interaction.User.Username}!\n----------------------------------------------------**");
    }

    public async Task OpenButton(DiscordClient client, ComponentInteractionCreatedEventArgs e)
    {
        if (e.Interaction.Type != DiscordInteractionType.Component || e.Interaction.User.IsBot || !e.Interaction.Data.CustomId.Contains(OpenButtonPrefix)) return;
            await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource);

            DiscordGuild guild = await client.GetGuildAsync(Convert.ToUInt64(e.Interaction.Data.CustomId.Replace(OpenButtonPrefix,"")));
            await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
            if (liveBotDbContext.GuildUsers.First(w=>w.GuildId == guild.Id && w.UserDiscordId == e.User.Id).IsModMailBlocked)
            {
                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("You are blocked from using the Mod Mail feature in this server."));
                return;
            }
            if (liveBotDbContext.ModMail.Any(w => w.UserDiscordId == e.User.Id && w.IsActive))
            {
                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("You already have an existing Mod Mail open, please close it before starting a new one."));
                return;
            }

            DiscordMember member;
            try
            {
                member = await guild.GetMemberAsync(e.User.Id);
            }
            catch (NotFoundException)
            {
                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("You are not in the server."));
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error getting member in OpenButton");
                return;
            }

            if (member.CommunicationDisabledUntil is not null && member.CommunicationDisabledUntil > DateTime.UtcNow)
            {
                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"You are currently timed out for <t:{member.CommunicationDisabledUntil.Value.ToUnixTimeSeconds()}:R>. Please wait until the timeout is over."));
                return;
            }
            Random r = new();
            var colorId = $"#{r.Next(0x1000000):X6}";
            ModMail newEntry = new(guild.Id,e.User.Id,DateTime.UtcNow, colorId)
            {
                IsActive = true,
                HasChatted = false
            };

            await databaseMethodService.AddModMailAsync(newEntry);
            
            DiscordButtonComponent closeButton = new(DiscordButtonStyle.Danger, $"{CloseButtonPrefix}{newEntry.Id}", "Close", false, new DiscordComponentEmoji("✖️"));

            await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddComponents(closeButton).WithContent($"**----------------------------------------------------**\n" +
                            $"ModMail entry **open** with `{guild.Name}`. Continue to write as you would normally ;)\n*Mod Mail will time out in {TimeoutMinutes} minutes after last message is sent.*\n" +
                            $"**Subject: No subject, Mod Mail Opened with button**"));

            DiscordEmbedBuilder embed = new()
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = $"{e.User.Username} ({e.User.Id})",
                    IconUrl = e.User.AvatarUrl
                },
                Title = $"[NEW] #{newEntry.Id} Mod Mail created by {e.User.Username}.",
                Color = new DiscordColor(colorId),
                Description = "No subject, Mod Mail Opened with button"
            };

            ulong? modMailChannelId = liveBotDbContext.Guilds.First(w=>w.Id== guild.Id).ModMailChannelId;
            if (modMailChannelId != null)
            {
                DiscordChannel modMailChannel = await guild.GetChannelAsync(modMailChannelId.Value);
                await new DiscordMessageBuilder()
                    .AddComponents(closeButton)
                    .AddEmbed(embed)
                    .SendAsync(modMailChannel);
            }
    }
}