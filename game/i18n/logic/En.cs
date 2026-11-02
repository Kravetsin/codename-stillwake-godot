using System.Collections.Generic;

namespace Stillwake.Game.I18n;

/// <summary>
/// Player-facing text, by key (apps/game/src/i18n/en.ts). Refusal reasons use the sim's keys,
/// <c>&lt;moduleId&gt;.&lt;why&gt;</c>. Plain C#, so <c>game.tests</c> checks it against the sim. Russian beside it, in <see cref="Ru"/>.
/// </summary>
public static partial class En
{
    /// <summary>Every key of every <see cref="Strings"/> field of this class, the files of the features included.</summary>
    public static readonly IReadOnlyDictionary<string, string> Table;

    // Built here, not in an initializer: the order of initializers across partial files is undefined,
    // and a static constructor runs after them all.
    static En() => Table = Strings.Gather(typeof(En));

    /// <summary>The language <see cref="T"/> speaks: set at the start and by the settings, English until then.</summary>
    public static Language Speaking { get; set; } = Language.English;

    /// <summary>The text for <paramref name="key"/> in <see cref="Speaking"/>, with each <c>{name}</c> in it filled.</summary>
    public static string T(string key, params (string Name, object Value)[] values) => In(Speaking, key, values);

    /// <summary>
    /// The text for <paramref name="key"/> in <paramref name="language"/>: its English while it has none there, the key itself
    /// while it has none at all, so a new string never breaks the game.
    /// </summary>
    public static string In(Language language, string key, params (string Name, object Value)[] values)
    {
        var text = language == Language.Russian && Ru.Table.TryGetValue(key, out var russian) ? russian
            : Table.TryGetValue(key, out var found) ? found : key;
        return values.Length == 0 ? text : NumberForms.Fill(text, values);
    }
}
