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

    public int CalculateLevenshteinDistance(ReadOnlySpan<char> str1, ReadOnlySpan<char> str2)
    {
        if (str1 == null) throw new ArgumentNullException(nameof(str1));
        if (str2 == null) throw new ArgumentNullException(nameof(str2));
        if (str1.Length == 0) return str2.Length;
        if (str2.Length == 0) return str1.Length;
        
        Span<int> prevRow = stackalloc int[str2.Length + 1];
        Span<int> currRow = stackalloc int[str2.Length + 1];
        
        for (var j = 0; j <= str2.Length; j++)
            prevRow[j] = j;
        for (var i = 1; i <= str1.Length; i++)
        {
            currRow[0] = i;

            for (var j = 1; j <= str2.Length; j++)
            {
                int cost = (str1[i - 1] == str2[j - 1]) ? 0 : 1;
                currRow[j] = Math.Min(Math.Min(prevRow[j] + 1, currRow[j - 1] + 1), prevRow[j - 1] + cost);
            }

            var temp = prevRow;
            prevRow = currRow;
            currRow = temp;
        }
        return prevRow[str2.Length];
    }
}