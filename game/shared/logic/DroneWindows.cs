using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>A window the hero opens: the drone's, the gear on the wrist, or a station's own.</summary>
public enum DroneWindowKind
{
    /// <summary>The suit's slots on the wrist: the hero's own, not the drone's.</summary>
    Gear,

    /// <summary>The bag beside the gear (Tab).</summary>
    Bag,

    /// <summary>Plan mode: the build menu and its plan.</summary>
    Plan,

    /// <summary>A station's own window, E on it: a machine's, a crate's, the fabricator's, the ship's computer. Not the drone's: no corners.</summary>
    Station,

    /// <summary>The research board.</summary>
    Board,

    /// <summary>The black market.</summary>
    Market,

    /// <summary>The journal, the handbook.</summary>
    Handbook,
}

/// <summary>
/// The drone's open windows (drone-frame): every drone window hands its place on the screen and its clock each frame, and the
/// drone's frame (<c>DroneFrame</c>) lays its corners round each one shown, however many at once. Client only.
/// </summary>
public sealed class DroneWindows
{
    /// <summary>A window as last handed: its place on the screen, px, and its clock.</summary>
    public readonly record struct Window(object Owner, (float X, float Y, float W, float H) Rect, CornerClock Clock);

    private readonly List<Window> _windows = [];

    /// <summary>Whether a window of <paramref name="kind"/> opens: before the drone is paired only the gear and a station's own do (drone-companion).</summary>
    public static bool Opens(DroneWindowKind kind, bool paired) => kind is DroneWindowKind.Gear or DroneWindowKind.Station || paired;

    /// <summary>Every window handed, in the order they first were.</summary>
    public IReadOnlyList<Window> All => _windows;

    /// <summary>Whether any is open, and how far the most out is, 0..1: what the drone at the waist goes by.</summary>
    public (bool Open, double Out) Out
    {
        get
        {
            bool open = false;
            float most = 0;
            foreach (var window in _windows)
            {
                open |= window.Clock.Opening;
                if (window.Clock.Shown) most = System.Math.Max(most, System.Math.Max(1e-3f, window.Clock.Screen));
            }
            return (open, most);
        }
    }

    /// <summary><paramref name="owner"/>'s window this frame: where it is, <paramref name="rect"/>, and its <paramref name="clock"/>, stepped by it.</summary>
    public void Hand(object owner, (float X, float Y, float W, float H) rect, CornerClock clock)
    {
        int at = _windows.FindIndex(w => ReferenceEquals(w.Owner, owner));
        if (at < 0) _windows.Add(new Window(owner, rect, clock));
        else _windows[at] = new Window(owner, rect, clock);
    }

    /// <summary>Forgets <paramref name="owner"/>'s window, as it leaves.</summary>
    public void Drop(object owner) => _windows.RemoveAll(w => ReferenceEquals(w.Owner, owner));
}
