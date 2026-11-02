using System.Collections.Generic;

namespace Stillwake.Game.I18n;

/// <summary>
/// Russian text, by the same keys as <see cref="En"/>, a feature's in its <c>logic/&lt;Name&gt;.Ru.cs</c>. A number's word is
/// written <c>{cells|клетка|клетки|клеток}</c> (<see cref="NumberForms"/>).
/// </summary>
public static partial class Ru
{
    /// <summary>Every key of every <see cref="Strings"/> field of this class, the files of the features included.</summary>
    public static readonly IReadOnlyDictionary<string, string> Table;

    // As in En: a static constructor runs after every initializer of every partial file.
    static Ru() => Table = Strings.Gather(typeof(Ru));
}
