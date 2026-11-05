using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Shared;

/// <summary>A mouse button as a hint draws it: a press, a drag or a hold, by its sign, never by a word (build-hints).</summary>
public enum Mouse
{
    Left,
    Right,
    Middle,
    LeftDrag,
    RightDrag,
    LeftHold,
}

/// <summary>One thing a hint says to press, drawn before its words: a key cap or a sign.</summary>
public abstract record Press;

/// <summary>The key an action is bound to, its cap showing the key's name now (<see cref="Binds.NameOf"/>).</summary>
public sealed record KeyPress(Bind Act) : Press;

/// <summary>A key no bind moves (Ctrl, a hotbar digit), its cap showing <paramref name="Name"/>.</summary>
public sealed record CapPress(string Name) : Press;

/// <summary>A mouse button, by its sign.</summary>
public sealed record MousePress(Mouse Button) : Press;

/// <summary>An item, by its icon: the tool to take in hand.</summary>
public sealed record ItemPress(string Item) : Press;

/// <summary>
/// A line of a hint: what to press, then an i18n key of one to three words saying what it does; <paramref name="Short"/>
/// draws it red, for what is missing.
/// </summary>
public sealed record KeyHint(IReadOnlyList<Press> Presses, string Says, bool Short = false)
{
    /// <summary>The line's content as one string, equal for equal lines: what a view compares to know the lines changed.</summary>
    public string Id => $"{string.Join("+", Presses.Select(p => p.ToString()))}:{Says}{(Short ? "!" : "")}";
}
