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

    private static readonly Dictionary<string, string> KeysOfEnglish = KeysOf(En.Table), KeysOfRussian = KeysOf(Ru.Table);

    private static Dictionary<string, string> KeysOf(IReadOnlyDictionary<string, string> table)
    {
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, text) in table.OrderBy(pair => pair.Key, StringComparer.Ordinal)) keys.TryAdd(text, key);
        return keys;
    }

    /// <summary>
    /// <paramref name="shown"/>, text on screen in <paramref name="from"/>, in <paramref name="to"/>, line by line; a line that is no
    /// whole text of the table is left as it is. So a label made once says the same again without every maker of one keeping its key;
    /// text with numbers in it is told anew by whoever keeps it current.
    /// </summary>
    public static string Retold(string shown, Language from, Language to)
    {
        if (shown.Length == 0 || from == to) return shown;
        var keys = from == Language.Russian ? KeysOfRussian : KeysOfEnglish;
        return string.Join('\n', shown.Split('\n').Select(line => keys.TryGetValue(line, out var key) ? En.In(to, key) : line));
    }
}
