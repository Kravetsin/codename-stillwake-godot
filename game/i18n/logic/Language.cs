using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.I18n;

/// <summary>A language the game speaks.</summary>
public enum Language
{
    English,
    Russian,
}

/// <summary>Languages by the names they are kept under, and the text already on screen told again in another one.</summary>
public static class Languages
{
    /// <summary>What <paramref name="language"/> is kept as in the settings.</summary>
    public static string NameOf(Language language) => language == Language.Russian ? "ru" : "en";

    /// <summary>The language kept as <paramref name="name"/>; none for a name it is not.</summary>
    public static Language? Named(string name) => name switch
    {
        "ru" => Language.Russian,
        "en" => Language.English,
        _ => null,
    };

    /// <summary>A first run's language, from the system's two-letter one: Russian on a Russian system, else English.</summary>
    public static Language OfSystem(string locale) => locale.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ? Language.Russian : Language.English;

    private static (Language Language, Shared.Binds Binds, Func<string, string> Shown, Dictionary<string, string> Keys)? _told;

    /// <summary>
    /// Every text of <paramref name="language"/> as it is shown now, its key placeholders filled, to its key: what <see cref="Retold"/>
    /// reads a label by. Kept till the language or the binds change; taken before a rebind, it still knows the labels made before it.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Told(Language language)
    {
        if (_told is var (l, b, s, kept) && l == language && b == Shared.Binds.Current && s == Shared.Binds.Shown) return kept;
        var table = language == Language.Russian ? Ru.Table : En.Table;
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in table.Keys.OrderBy(k => k, StringComparer.Ordinal)) keys.TryAdd(En.In(language, key), key);
        _told = (language, Shared.Binds.Current, Shared.Binds.Shown, keys);
        return keys;
    }

    /// <summary>
    /// <paramref name="shown"/>, text on screen in <paramref name="from"/>, in <paramref name="to"/>, line by line; a line that is no
    /// whole text of the table is left as it is. So a label made once says the same again without every maker of one keeping its key;
    /// text with numbers in it is told anew by whoever keeps it current.
    /// </summary>
    public static string Retold(string shown, Language from, Language to) => from == to ? shown : Retold(shown, Told(from), to);

    /// <summary><paramref name="shown"/> read by <paramref name="told"/> (<see cref="Told"/>, taken before a change) and said in <paramref name="to"/> now.</summary>
    public static string Retold(string shown, IReadOnlyDictionary<string, string> told, Language to)
    {
        if (shown.Length == 0) return shown;
        return string.Join('\n', shown.Split('\n').Select(line => told.TryGetValue(line, out var key) ? En.In(to, key) : line));
    }
}
