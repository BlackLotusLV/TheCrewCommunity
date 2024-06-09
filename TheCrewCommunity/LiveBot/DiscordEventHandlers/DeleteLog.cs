using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.DiscordEventHandlers;

public static class DeleteLog
{
    //private const int MaxTitleLength = 256;
    private const int MaxDescriptionLength = 4096;
    //private const int MaxFields = 25;
    //private const int MaxFieldNameLength = 256;
    //private const int MaxFieldValueLength = 1024;
    //private const int MaxFooterLength = 2048;
    //private const int MaxEmbedLength = 6000;
    private static readonly List<string> ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".tiff", ".jfif", ".svg", ".ico"];
    
    public static async Task OnMessageDeleted(DiscordClient client, MessageDeletedEventArgs args)
    {
        if (args.Guild is null) return;
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        HttpClient httpClient = client.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guild = await liveBotDbContext.Guilds.FindAsync(args.Guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(args.Guild.Id));
        if (guild.DeleteLogChannelId is null) return;
        DiscordChannel deleteLogChannel = await client.Guilds.FirstOrDefault(w => w.Value.Id == guild.Id).Value.GetChannelAsync(guild.DeleteLogChannelId.Value);
        
        if (args.Message.Author is null)
        {
            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor(0xFF6600),
                Description = $"# Message Deleted\n" +
                              $"- **Author:**  UNKNOWN\n" +
                              $"- **Channel:** {args.Channel.Mention}\n" +
                              $"*Uncached message deleted, no info found. Logging as much as possible.*"
            };
            DiscordMessageBuilder message = new();
            message.AddEmbed(embed.Build());
            await deleteLogChannel.SendMessageAsync(message);
            client.Logger.LogInformation(CustomLogEvents.DeleteLog, "Uncached message deleted in {Channel}", args.Channel.Name);
            return;
        }
        if (args.Message.Author.IsBot) return;
        string msgContent = args.Message.Content == "" ? "*message didn't contain any text*" : $"*{args.Message.Content}*",
            replyInfo = "*not a reply*";
        StringBuilder sb = new();
        if (args.Message is { MessageType: DiscordMessageType.Reply, Reference.Message.Author: not null })
        {
            replyInfo = $"[Reply to {args.Message.Reference.Message.Author.Username}](<{args.Message.Reference.Message.JumpLink}>)";
        }

        StringBuilder mentionedUserBuilder = new();
        foreach (DiscordUser mentionedUser in args.Message.MentionedUsers)
        {
            mentionedUserBuilder.Append($"{mentionedUser.Mention};");
        }
        var mentionedUsers = mentionedUserBuilder.ToString();

        sb.Append($"# Message Deleted\n" +
                  $"- **Author:** {args.Message.Author.Mention}({args.Message.Author.Id})\n" +
                  $"- **Channel:** {args.Channel.Mention}\n" +
                  $"- **Attachment Count:** {args.Message.Attachments.Count}\n" +
                  $"- **Reply:** {replyInfo}\n" +
                  $"- **Mentioned:** {mentionedUsers}\n" +
                  $"- **Time posted:** <t:{args.Message.CreationTimestamp.ToUnixTimeSeconds()}:F>\n" +
                  $"- **Message:** ");
        if (sb.ToString().Length + msgContent.Length > MaxDescriptionLength)
        {
            sb.Append($"{msgContent.AsSpan(0, MaxDescriptionLength - (sb.ToString().Length+3))}...");
        }
        else
        {
            sb.Append($"{msgContent}");
        }

        DiscordEmbedBuilder embedBuilder = new()
        {
            Color = new DiscordColor(0xFF6600),
            Description = sb.ToString(),
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                IconUrl = args.Message.Author.AvatarUrl,
                Name = $"{args.Message.Author.Username}'s message deleted"
            }
        };
        if (args.Message.Stickers is not null && args.Message.Stickers.Count != 0)
        {
            embedBuilder.AddField("Sticker", $"[{args.Message.Stickers[0].Name}]({args.Message.Stickers[0].StickerUrl})");
        }
        DiscordMessageBuilder messageBuilder = new();
        List<DiscordEmbed> attachmentEmbeds = [];
        List<MemoryStream> imageStreams = [];
        StringBuilder attachmentNames = new();
        foreach (DiscordAttachment messageAttachment in args.Message.Attachments)
        {
            attachmentNames.AppendLine($"- {messageAttachment.FileName}");
            if (!ImageExtensions.Contains(Path.GetExtension(messageAttachment.FileName??""))) continue;
            HttpResponseMessage response = await httpClient.GetAsync(messageAttachment.Url);
            if (!response.IsSuccessStatusCode) continue;
            var uniqueFileName = $"{Guid.NewGuid()}-{messageAttachment.FileName}";
            MemoryStream ms = new();
            await response.Content.CopyToAsync(ms);
            ms.Position = 0;
            messageBuilder.AddFile(uniqueFileName, ms);
            imageStreams.Add(ms);
            if (embedBuilder.ImageUrl is null)
            {
                embedBuilder.ImageUrl = $"attachment://{uniqueFileName}";
            }
            else
            {
                attachmentEmbeds.Add(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor(0xFF6600),
                        ImageUrl = $"attachment://{uniqueFileName}"
                    }.Build()
                );
            }
        }

        if (args.Message.Attachments.Count!=0)
        {
            embedBuilder.AddField("Attachment Names", attachmentNames.ToString());
        }

        attachmentEmbeds.Insert(0, embedBuilder.Build());
        messageBuilder.AddEmbeds(attachmentEmbeds);

        await deleteLogChannel.SendMessageAsync(messageBuilder);
        await Parallel.ForEachAsync(imageStreams.AsEnumerable(), async (stream, _) => await stream.DisposeAsync());
        client.Logger.LogInformation(CustomLogEvents.DeleteLog, "{User}'s message was deleted in {Channel}", args.Message.Author.Username, args.Channel.Name);
    }
    
    public static async Task OnBulkDelete(DiscordClient client, MessagesBulkDeletedEventArgs e)
    {
        
        var dbContextFactory = client.ServiceProvider.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
        var databaseMethodService = client.ServiceProvider.GetRequiredService<IDatabaseMethodService>();
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        Guild guildSettings = await liveBotDbContext.Guilds.FindAsync(e.Guild.Id) ?? await databaseMethodService.AddGuildAsync(new Guild(e.Guild.Id));
        if (guildSettings.DeleteLogChannelId == null) return;
        DiscordGuild guild = client.Guilds.FirstOrDefault(w => w.Value.Id == guildSettings.Id).Value;
        DiscordChannel deleteLog = await guild.GetChannelAsync(guildSettings.DeleteLogChannelId.Value);
        StringBuilder sb = new();
        foreach (DiscordMessage message in e.Messages.Reverse())
        {
            if (message.Author is not null && message.Channel is not null)
            {
                if (!message.Author.IsBot)
                {
                    sb.AppendLine($"{message.Author.Username}{message.Author.Mention} {message.Timestamp} " +
                                  $"\n{message.Channel.Mention} - {message.Content}");
                }
            }
            else
            {
                sb.AppendLine($"Author Unknown {message.Timestamp}" +
                              $"\n- Bot was offline when this message was created.");
            }
        }

        if (sb.ToString().Length < 2000)
        {
            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor(0xFF6600),
                Title = "Bulk delete log",
                Description = sb.ToString()
            };
            await deleteLog.SendMessageAsync(embed: embed);
            client.Logger.LogInformation(CustomLogEvents.DeleteLog, "Bulk delete log sent");
        }
        else
        {
            await File.WriteAllTextAsync($"{Path.GetTempPath()}{e.Messages.Count}-BulkDeleteLog.txt", sb.ToString());
            await using var upFile = new FileStream($"{Path.GetTempPath()}{e.Messages.Count}-BulkDeleteLog.txt", FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
            var msgBuilder = new DiscordMessageBuilder
            {
                Content = $"Bulk delete log(Over the message cap) ({e.Messages.Count}) [{e.Messages[0].Timestamp} - {e.Messages[^1].Timestamp}]"
            };
            msgBuilder.AddFile(upFile);
            await deleteLog.SendMessageAsync(msgBuilder);
            client.Logger.LogInformation(CustomLogEvents.DeleteLog, "Bulk delete log sent - over message cap");
        }
    }
}