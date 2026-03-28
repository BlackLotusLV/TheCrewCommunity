using DSharpPlus;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.Entities;
using TheCrewCommunity.Data.Entities.Discord;
using TheCrewCommunity.Services;

namespace TheCrewCommunity.LiveBot.Commands.ModeratorCommands;

public static class SetPersistentMessageCommand
{
    public static async Task ExecuteAsync(IDbContextFactory<LiveBotDbContext> dbContextFactory, IPersistentMessageService persistentMessageService, SlashCommandContext ctx, string messageIdString, DiscordChannel? channel = null)
    {
        await ctx.DeferResponseAsync(true);
        channel ??= ctx.Channel;

        if (!ulong.TryParse(messageIdString, out ulong messageId))
        {
            await ctx.EditResponseAsync("Invalid message ID provided.");
            return;
        }

        DiscordMessage sourceMessage;
        try
        {
            sourceMessage = await channel.GetMessageAsync(messageId);
        }
        catch (Exception)
        {
            await ctx.EditResponseAsync($"Could not find message with ID {messageId} in {channel.Mention}.");
            return;
        }

        string messageContent = sourceMessage.Content;
        if (string.IsNullOrWhiteSpace(messageContent))
        {
            await ctx.EditResponseAsync("The source message has no text content.");
            return;
        }
        
        if (messageContent.Length > 2000)
        {
            await ctx.EditResponseAsync("The source message content exceeds 2000 characters.");
            return;
        }

        await using LiveBotDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        PersistentMessage? persistentMessage = await dbContext.PersistentMessages.FindAsync(channel.Id);

        if (persistentMessage == null)
        {
            persistentMessage = new PersistentMessage
            {
                ChannelId = channel.Id,
                Content = messageContent,
                MessageId = 0,
                LastPostedAt = null
            };
            await dbContext.PersistentMessages.AddAsync(persistentMessage);
        }
        else
        {
            persistentMessage.Content = messageContent;
            dbContext.PersistentMessages.Update(persistentMessage);
        }

        await dbContext.SaveChangesAsync();
        
        persistentMessageService.EnqueueMessageUpdate(channel.Id);

        await ctx.EditResponseAsync($"Persistent message for {channel.Mention} has been set/updated using content from message {messageId}.");
    }
}
