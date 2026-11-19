using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// Which drone window the drone's soot frames (drone-frame): one at a time, the newest opened. Every drone window hands its
/// outline and whether it is open each frame; one opening while another is open takes the soot, and the one it took from is told
/// (<see cref="Taken"/>) and closes. Closed, the owner keeps its outline for the soot flying home. Client only.
/// </summary>
public sealed class SootClaims
{
    /// <summary>Each window that has handed anything, and whether it was open as it last did.</summary>
    private readonly List<(object Window, bool Open)> _heard = [];

    /// <summary>The window the soot frames, or last framed; null before any opened.</summary>
    public object? Owner { get; private set; }

    /// <summary>Its outline on the screen, its corners in order, screen px.</summary>
    public IReadOnlyList<(float X, float Y)> Outline { get; private set; } = [];

    /// <summary>Whether it is open.</summary>
    public bool Open { get; private set; }

    /// <summary>A window the soot was taken from while it was open: it closes.</summary>
    public event Action<object>? Taken;

    /// <summary>Window <paramref name="window"/>'s <paramref name="outline"/> this frame, and whether it is <paramref name="open"/>.</summary>
    public void Hand(object window, IReadOnlyList<(float X, float Y)> outline, bool open)
    {
        int at = _heard.FindIndex(h => ReferenceEquals(h.Window, window));
        bool opening = open && (at < 0 || !_heard[at].Open);
        if (at < 0) _heard.Add((window, open));
        else _heard[at] = (window, open);
        if (ReferenceEquals(window, Owner))
        {
            (Outline, Open) = (outline, open);
            return;
        }
        if (!opening) return;
        var from = Owner;
        bool fromOpen = Open;
        (Owner, Outline, Open) = (window, outline, true);
        if (from != null && fromOpen) Taken?.Invoke(from);
    }
}
