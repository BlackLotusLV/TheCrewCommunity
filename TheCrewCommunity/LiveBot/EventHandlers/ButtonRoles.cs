using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;

namespace TheCrewCommunity.LiveBot.EventHandlers;

public class ButtonRoles(IDbContextFactory<LiveBotDbContext> dbContextFactory)
{
    private const string ButtonRolePrefix = "ButtonRole-";
    public async Task OnButtonClick(object client, ComponentInteractionCreateEventArgs e)
    {
        if (e.Interaction is not { Type: InteractionType.Component, User.IsBot: false }|| !e.Interaction.Data.CustomId.Contains(ButtonRolePrefix) || e.Interaction.Guild is null) return;
        await using LiveBotDbContext liveBotDbContext = await dbContextFactory.CreateDbContextAsync();
        var rolesList = await liveBotDbContext.ButtonRoles.Where(x => x.GuildId == e.Interaction.GuildId && x.ChannelId == e.Interaction.ChannelId).ToListAsync();
        if (rolesList.Count == 0) return;
        string buttonCustomId = e.Interaction.Data.CustomId.Replace(ButtonRolePrefix,"");
        if (!ulong.TryParse(buttonCustomId,out ulong roleId)) return;
        var buttonRoleInfo = rolesList.Where(roles => e.Interaction.Guild.Roles.Any(guildRole => Convert.ToUInt64(roles.ButtonId) == guildRole.Value.Id)).ToList();
        if (buttonRoleInfo.Count > 0 && buttonRoleInfo[0].ChannelId == e.Interaction.Channel.Id)
        {
            DiscordInteractionResponseBuilder response = new()
            {
                IsEphemeral = true
            };
            var member = e.Interaction.User as DiscordMember;
            DiscordRole role = e.Interaction.Guild.Roles.FirstOrDefault(w => w.Value.Id == roleId).Value;
            if (member is null) return;
            if (member.Roles.Any(w => w.Id == roleId))
            {
                await member.RevokeRoleAsync(role);
                response.Content = $"{member.Mention} the {role.Mention} role has been removed.";
            }
            else
            {
                await member.GrantRoleAsync(role);
                response.Content = $"{member.Mention} you have been given the {role.Mention} role.";
            }

            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);
        }
    }
}