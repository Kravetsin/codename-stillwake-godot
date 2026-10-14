using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Shared;

/// <summary>
/// A value others watch (session/store.ts): <see cref="Set"/> tells every listener once it changes,
/// in the order they subscribed, and nothing when it is set to what it is.
/// </summary>
public sealed class Store<T>(T initial)
{
    private T _value = initial;
    private readonly List<Action> _listeners = [];

    public T Get() => _value;

    public void Set(T value)
    {
        if (EqualityComparer<T>.Default.Equals(_value, value)) return;
        _value = value;
        // A listener may set the store again: each hears every change, in order.
        foreach (var listener in _listeners.ToArray()) listener();
    }

    /// <summary>Calls <paramref name="listener"/> after every change from now on.</summary>
    public void Subscribe(Action listener) => _listeners.Add(listener);
}

/// <summary>Stores of lists, told only when what they hold changes: a tip set every frame redraws nothing.</summary>
public static class ListStore
{
    public static void SetLines<T>(this Store<IReadOnlyList<T>?> store, IReadOnlyList<T>? lines)
    {
        if (lines is { Count: 0 }) lines = null;
        var now = store.Get();
        if (now == null ? lines == null : lines != null && now.SequenceEqual(lines)) return;
        store.Set(lines);
    }
}

/// <summary>What the hand holds (0022), one thing at a time.</summary>
public enum Holding
{
    /// <summary>Nothing, or a hotbar item.</summary>
    Free,
    /// <summary>The plan out with no card chosen.</summary>
    Plan,
    /// <summary>A build card.</summary>
    Card,
    /// <summary>The cable card.</summary>
    Cable,
}

/// <summary>A line of a cost: how much there is of an item, toward how much is needed.</summary>
public sealed record CostLine(string Item, int Have, int Need)
{
    /// <summary><paramref name="need"/> laid against <paramref name="have"/>, item by item in the order <paramref name="need"/> names them.</summary>
    public static List<CostLine> Of(IReadOnlyList<(string Item, int Count)> need, IReadOnlyList<(string Item, int Count)> have)
    {
        var lines = new List<CostLine>();
        foreach (var (item, n) in need)
        {
            if (n <= 0) continue;
            int count = 0;
            foreach (var (i, c) in have) if (i == item) count += c;
            lines.Add(new CostLine(item, count, n));
        }
        return lines;
    }
}

/// <summary>
/// What the features of this machine's player share (session/ui.ts), one of each for the one player at
/// this keyboard: what the hand holds, which overlay is on, and the words beside the pointer.
/// </summary>
public sealed class Ui
{
    /// <summary>
    /// What the hand holds: <c>building</c> sets it with the card; a hotbar slot picked or the inventory
    /// opened frees it, and <c>building</c> then puts the plan away. With anything but a free hand, the
    /// hero leaves the left button to the card.
    /// </summary>
    public Store<Holding> Hand { get; } = new(Holding.Free);

    /// <summary>
    /// Which overlay is on, by its feature's id, or none: switched by its key, and by <c>building</c>
    /// while a card that needs one is out. It only shows, and says what the right button removes and
    /// the middle one picks (0022).
    /// </summary>
    public Store<string?> OverlayOn { get; } = new(null);

    /// <summary>What an overlay says of the cell under the pointer, set by that overlay while it is on.</summary>
    public Store<IReadOnlyList<string>?> WorldTip { get; } = new(null);

    /// <summary>A blueprint's cost under the pointer, set by <c>building</c>: delivered against needed.</summary>
    public Store<IReadOnlyList<CostLine>?> CostTip { get; } = new(null);

    /// <summary>
    /// Why what the hand would do under the pointer is refused, an i18n key: set each frame by the
    /// feature whose card it is (a ghost, a cable dragged), drawn in the world beside the pointer.
    /// </summary>
    public Store<string?> Reason { get; } = new(null);
}
