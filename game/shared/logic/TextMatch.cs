using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stillwake.Game.Shared;

/// <summary>How well a typed text names something, best first; a name that misses gets none.</summary>
public enum MatchTier
{
    /// <summary>The whole name, word for word.</summary>
    Exact,
    /// <summary>Every typed word starts a word of the name.</summary>
    Prefix,
    /// <summary>Every typed word starts a word of the name, one at least off by a letter: "furnase".</summary>
    Near,
}

/// <summary>
/// Whether a typed text names something (board-search-related): by its words' starts, case ignored, ё read as е,
/// a slip of one letter forgiven in a word of <see cref="NearFrom"/> letters or more.
/// </summary>
public static class TextMatch
{
    /// <summary>The shortest typed word a slip is forgiven in: shorter ones would match too much among short names.</summary>
    public const int NearFrom = 4;

    /// <summary>The words of <paramref name="text"/>, lower case, ё as е, split at anything but letters and digits.</summary>
    public static string[] Words(string text)
    {
        var folded = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            char lower = char.ToLowerInvariant(c);
            folded.Append(lower == 'ё' ? 'е' : char.IsLetterOrDigit(lower) ? lower : ' ');
        }
        return folded.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>How well the typed <paramref name="query"/> words name <paramref name="name"/>'s words; none if a word of it starts no word of the name.</summary>
    public static MatchTier? Of(IReadOnlyList<string> query, IReadOnlyList<string> name)
    {
        if (query.Count == 0) return null;
        if (query.SequenceEqual(name, StringComparer.Ordinal)) return MatchTier.Exact;
        var worst = MatchTier.Prefix;
        foreach (string word in query)
        {
            if (name.Any(n => n.StartsWith(word, StringComparison.Ordinal))) continue;
            if (word.Length < NearFrom || !name.Any(n => Starts(word, n))) return null;
            worst = MatchTier.Near;
        }
        return worst;
    }

    /// <summary>Whether <paramref name="word"/> is one slip from the start of <paramref name="name"/>: a letter wrong, missing, extra or two swapped.</summary>
    private static bool Starts(string word, string name)
    {
        for (int length = word.Length - 1; length <= word.Length + 1; length++)
            if (length <= name.Length && Slips(word, name[..length]) <= 1) return true;
        return false;
    }

    /// <summary>The letters wrong, missing, extra or swapped between two words (optimal string alignment).</summary>
    private static int Slips(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1]) d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        return d[a.Length, b.Length];
    }
}
