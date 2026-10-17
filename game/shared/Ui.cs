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
    /// hero leaves the left button to the card; the right button works plans whatever it holds (0030).
    /// </summary>
    public Store<Holding> Hand { get; } = new(Holding.Free);

    /// <summary>
    /// The item in the chosen hotbar slot while the hand is free, else none: set by <c>items</c>, which
    /// owns the hotbar; <c>crew</c> swings it with the left button when it is the fire axe (0034), and aims
    /// and fires it when it is the harpoon (0044).
    /// </summary>
    public Store<string?> Wielded { get; } = new(null);

    /// <summary>Whether the air overlay is wanted: O switches it, outside building too, for air keeps the crew alive.</summary>
    public Store<bool> AirOn { get; } = new(false);

    /// <summary>
    /// Which overlay shows, by its feature's id, or none (0030): never kept, worked out on each read by
    /// what <c>building</c> sets here — the open build tab's overlay, else air if <see cref="AirOn"/>.
    /// With the build menu closed it is air or nothing, so no mode outlives the menu. It says what the
    /// middle button removes and Q picks.
    /// </summary>
    public string? OverlayOn => OverlayShown();

    /// <summary>What <see cref="OverlayOn"/> reads: set once by whoever holds the build menu's state.</summary>
    public System.Func<string?> OverlayShown { get; set; } = () => null;

    /// <summary>What an overlay says of the cell under the pointer, set by that overlay while it is on.</summary>
    public Store<IReadOnlyList<string>?> WorldTip { get; } = new(null);

    /// <summary>A blueprint's cost under the pointer, set by <c>building</c>: delivered against needed.</summary>
    public Store<IReadOnlyList<CostLine>?> CostTip { get; } = new(null);

    /// <summary>
    /// What a panel says of what the pointer is on, a slot, a card, a craft: set by the feature that owns
    /// it, through <c>TipSpots</c>, and drawn by the pointer over the world's own words.
    /// </summary>
    public Store<Tip?> Tip { get; } = new(null);

    /// <summary>
    /// Why what the hand would do under the pointer is refused, an i18n key: set each frame by the
    /// feature whose card it is (a ghost, a cable dragged), drawn in the world beside the pointer.
    /// </summary>
    public Store<string?> Reason { get; } = new(null);

    /// <summary>
    /// The crate, furnace or analyzer this player has open: <c>crew</c> opens it on E within reach and
    /// closes it out of reach, <c>items</c> shows it beside the inventory and closes it on Tab or Esc.
    /// </summary>
    public Store<int?> OpenCrate { get; } = new(null);

    /// <summary>
    /// Where the open inventory's grid ends on the screen, its right edge and its top; null while it is
    /// closed. Set by <c>items</c> every frame; <c>crafting</c> docks its crafts beside it.
    /// </summary>
    public Store<(float Right, float Top)?> InventoryEdge { get; } = new(null);

    /// <summary>
    /// Where the open build menu's top edge is on the screen; null while it is closed. Set by
    /// <c>building</c> every frame; <c>crew</c> lifts the prompt for E above it.
    /// </summary>
    public float? MenuTop { get; set; }

    /// <summary>
    /// Every hero's chest as drawn this frame, in the world's pixels, this machine's own first. Set by
    /// <c>crew</c> every frame; <c>building</c> throws a plan's sparks from its side facing the nearest.
    /// </summary>
    public IReadOnlyList<(float X, float Y)> Heroes { get; set; } = [];

    /// <summary>
    /// The piece of debris the hands would grab or chop, and whether the sim would refuse it: <c>crew</c> sets it while
    /// the pointer is on one within reach, <c>debris</c> outlines that piece.
    /// </summary>
    public Store<(int Id, bool Refused)?> AimedPiece { get; } = new(null);

    /// <summary>
    /// Whether the game menu is open: set by the menu. The world runs on behind it, but the hero takes
    /// no keys and the hands no pointer, so nothing in the world answers a player who is not there.
    /// </summary>
    public Store<bool> MenuOpen { get; } = new(false);

    /// <summary>
    /// This player just placed plans in these cells: <c>building</c> says so as it sends them, and
    /// <c>crew</c> has the hero aim the planner at them and beam them in. Told once a placing.
    /// </summary>
    public event Action<IReadOnlyList<int>>? Planned;

    public void Plan(IReadOnlyList<int> cells) => Planned?.Invoke(cells);

    /// <summary>
    /// This player asked to breathe a canister into the suit (0047): <c>items</c> says so on a right click on
    /// one in the inventory; <c>crew</c> sends it, as it does for one in hand, and the hiss is heard.
    /// </summary>
    public event Action? Breathed;

    public void Breathe() => Breathed?.Invoke();
}
