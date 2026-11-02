using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Stillwake.Game.I18n;

/// <summary>
/// A text's <c>{name}</c>s filled, and its words that follow a number: <c>{cells|клетка|клетки|клеток}</c> is the form for 1, for 2
/// and for 5 of what <c>cells</c> holds. The number itself is written apart, so a text may put it where it likes.
/// </summary>
public static partial class NumberForms
{
    /// <summary>Which of a Russian word's three forms goes with <paramref name="n"/>: 0 for 1 and 21, 1 for 2 and 23, 2 for 5, 11 and 12.</summary>
    public static int Russian(long n)
    {
        long tens = Math.Abs(n) % 100, ones = tens % 10;
        if (tens is >= 11 and <= 14) return 2;
        return ones == 1 ? 0 : ones is >= 2 and <= 4 ? 1 : 2;
    }

    /// <summary><paramref name="text"/> with every <c>{name}</c> and <c>{name|one|few|many}</c> of <paramref name="values"/> filled, the others left as they are.</summary>
    public static string Fill(string text, IReadOnlyList<(string Name, object Value)> values)
    {
        string worded = Worded().Replace(text, m => Value(m.Groups[1].Value) is { } said ? m.Groups[2 + FormOf(said)].Value : m.Value);
        return Named().Replace(worded, m => Value(m.Groups[1].Value) ?? m.Value);

        string? Value(string name)
        {
            foreach (var (each, value) in values)
                if (each == name) return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            return null;
        }
    }

    /// <summary>What is not a whole number (1.5, 1,024) takes the form most numbers take.</summary>
    private static int FormOf(string said) =>
        long.TryParse(said, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long n) ? Russian(n) : 2;

    /// <summary>The names a text fills, each once, sorted: what a translation must have the same of.</summary>
    public static SortedSet<string> NamesIn(string text)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        foreach (Match m in Named().Matches(text)) names.Add(m.Groups[1].Value);
        foreach (Match m in Worded().Matches(text)) names.Add(m.Groups[1].Value);
        return names;
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex Named();

    [GeneratedRegex(@"\{(\w+)\|([^|{}]*)\|([^|{}]*)\|([^|{}]*)\}")]
    private static partial Regex Worded();
}
