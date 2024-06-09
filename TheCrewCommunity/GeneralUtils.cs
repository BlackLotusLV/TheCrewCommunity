using DSharpPlus.Entities;

namespace TheCrewCommunity;

public class GeneralUtils
{
    public bool CheckIfMemberAdmin(DiscordMember member)
    {
        return member.Permissions.HasPermission(DiscordPermissions.ManageMessages) ||
               member.Permissions.HasPermission(DiscordPermissions.KickMembers) ||
               member.Permissions.HasPermission(DiscordPermissions.BanMembers) ||
               member.Permissions.HasPermission(DiscordPermissions.Administrator);
    }
    
    public int CalculateLevenshteinDistance(string a, string b)
    {
        var matrix = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; i++)
            matrix[i, 0] = i;
        for (var j = 0; j <= b.Length; j++)
            matrix[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                int cost = b[j - 1] == a[i - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + cost);
            }
        }
        return matrix[a.Length, b.Length];
    }
}