using DSharpPlus.Entities;

namespace TheCrewCommunity;

public class GeneralUtils
{
    public bool CheckIfMemberAdmin(DiscordMember member)
    {
        return member.Permissions.HasPermission(DiscordPermission.ManageMessages) ||
               member.Permissions.HasPermission(DiscordPermission.KickMembers) ||
               member.Permissions.HasPermission(DiscordPermission.BanMembers) ||
               member.Permissions.HasPermission(DiscordPermission.Administrator);
    }

    public int CalculateLevenshteinDistance(ReadOnlySpan<char> source, ReadOnlySpan<char> target)
    {
        if (source.IsEmpty) return target.Length;
        if (target.IsEmpty) return source.Length;

        Span<int> costs = stackalloc int[target.Length + 1];

        for (var i = 0; i <= target.Length; i++)
            costs[i] = i;

        var previousRowMinCost = 0;

        for (var i = 0; i < source.Length; i++)
        {
            costs[0] = i + 1;
            int previousRowCost = i;

            for (var j = 0; j < target.Length; j++)
            {
                int currentRowCost = previousRowMinCost;

                previousRowMinCost = costs[j + 1];

                costs[j + 1] = source[i] == target[j]
                    ? previousRowCost
                    : 1 + Math.Min(Math.Min(currentRowCost, previousRowMinCost), previousRowCost);
            }
        }

        return costs[target.Length];
    }


    public double CalculateStringSimilarity(ReadOnlySpan<char> searchTerm, ReadOnlySpan<char> target)
    {
        if (searchTerm.IsEmpty || target.IsEmpty)
            return 0;
        Span<char> searchLower = stackalloc char[searchTerm.Length];
        Span<char> targetLower = stackalloc char[target.Length];

        for (var i = 0; i < searchTerm.Length; i++)
        {
            searchLower[i] = char.ToLowerInvariant(searchTerm[i]);
        }
        for (var i = 0; i < target.Length; i++)
        {
            targetLower[i] = char.ToLowerInvariant(target[i]);
        }
        
        searchLower = TrimSpan(searchLower);
        targetLower = TrimSpan(targetLower);
        
        if (searchLower.IsEmpty || targetLower.IsEmpty)
        {
            return 0;
        }
        
        double levenshteinSimilarity = 1.0 - (double) CalculateLevenshteinDistance(searchLower, targetLower) / Math.Max(searchLower.Length, targetLower.Length);
        double consecutiveBonus = CalculateConsecutiveMatchBonus(searchLower, targetLower);
        double exactMatchBonus = Contains(targetLower, searchLower) ? 0.5 : 0;
        double finalScore = (levenshteinSimilarity * 0.3) + (consecutiveBonus * 0.5) + exactMatchBonus;
        
        return Math.Min(1.0, Math.Max(0.0, finalScore));
    }
    private static Span<char> TrimSpan(Span<char> span)
    {
        var start = 0;
        while (start < span.Length && char.IsWhiteSpace(span[start]))
            start++;

        int end = span.Length - 1;
        while (end >= start && char.IsWhiteSpace(span[end]))
            end--;

        return span.Slice(start, end - start + 1);
    }
    private static double CalculateConsecutiveMatchBonus(ReadOnlySpan<char> searchTerm, ReadOnlySpan<char> target)
    {
        if (searchTerm.IsEmpty) return 0;

        var maxConsecutive = 0;
        var currentConsecutive = 0;
        var searchIndex = 0;

        for (var i = 0; i < target.Length && searchIndex < searchTerm.Length; i++)
        {
            if (target[i] == searchTerm[searchIndex])
            {
                currentConsecutive++;
                searchIndex++;

                maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
            }
            else
            {
                currentConsecutive = 0;

                if (searchIndex <= 0) continue;
                i--;
                searchIndex = 0;
            }
        }

        return (double)maxConsecutive / searchTerm.Length;
    }
    private static bool Contains(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
    {
        if (value.Length > source.Length)
            return false;

        for (var i = 0; i <= source.Length - value.Length; i++)
        {
            var found = true;
            for (var j = 0; j < value.Length; j++)
            {
                if (source[i + j] == value[j]) continue;
                found = false;
                break;
            }
            if (found)
                return true;
        }
        return false;
    }

}