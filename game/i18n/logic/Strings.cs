using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Stillwake.Game.I18n;

/// <summary>One feature's texts in one language, a field in its <c>logic/&lt;Name&gt;.En.cs</c> or <c>.Ru.cs</c>.</summary>
internal sealed class Strings : Dictionary<string, string>
{
    public Strings() : base(StringComparer.Ordinal)
    {
    }

    /// <summary>
    /// Every key of every <see cref="Strings"/> field of <paramref name="language"/>, by reflection: called from an initializer
    /// of its class, the fields gathered must be declared in the other partial files, so it is the class's static constructor that calls.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Gather(Type language)
    {
        var table = new Dictionary<string, string>(StringComparer.Ordinal);
        var owner = new Dictionary<string, string>(StringComparer.Ordinal);
        var fields = language.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(Strings))
            .OrderBy(f => f.Name, StringComparer.Ordinal);
        foreach (var field in fields)
            foreach (var (key, text) in (Strings)field.GetValue(null)!)
            {
                if (!table.TryAdd(key, text))
                    throw new InvalidOperationException($"i18n key \"{key}\" is in both {owner[key]} and {field.Name}");
                owner[key] = field.Name;
            }
        return table;
    }
}
