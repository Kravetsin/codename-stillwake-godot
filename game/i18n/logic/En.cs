using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Stillwake.Game.I18n;

/// <summary>
/// Player-facing text, by key (apps/game/src/i18n/en.ts). Refusal reasons use the sim's keys,
/// <c>&lt;moduleId&gt;.&lt;why&gt;</c>. Plain C#, so <c>game.tests</c> checks it against the sim.
/// </summary>
public static partial class En
{
    /// <summary>One feature's texts, in its <c>logic/&lt;Name&gt;.En.cs</c>; <see cref="Table"/> gathers every one.</summary>
    private sealed class Strings : Dictionary<string, string>
    {
        public Strings() : base(StringComparer.Ordinal)
        {
        }
    }

    /// <summary>Every key of every <see cref="Strings"/> field of this class, the files of the features included.</summary>
    public static readonly IReadOnlyDictionary<string, string> Table;

    // Built here, not in an initializer: the order of initializers across partial files is undefined,
    // and a static constructor runs after them all.
    static En()
    {
        var table = new Dictionary<string, string>(StringComparer.Ordinal);
        var owner = new Dictionary<string, string>(StringComparer.Ordinal);
        var fields = typeof(En).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(Strings))
            .OrderBy(f => f.Name, StringComparer.Ordinal);
        foreach (var field in fields)
            foreach (var (key, text) in (Strings)field.GetValue(null)!)
            {
                if (!table.TryAdd(key, text))
                    throw new InvalidOperationException($"i18n key \"{key}\" is in both {owner[key]} and {field.Name}");
                owner[key] = field.Name;
            }
        Table = table;
    }

    /// <summary>The text for <paramref name="key"/>, or the key itself while it has none, with each <c>{name}</c> in it filled.</summary>
    public static string T(string key, params (string Name, object Value)[] values)
    {
        var text = Table.TryGetValue(key, out var found) ? found : key;
        if (values.Length == 0) return text;
        return Named().Replace(text, m =>
        {
            foreach (var (name, value) in values)
                if (name == m.Groups[1].Value) return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            return m.Value;
        });
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex Named();
}
