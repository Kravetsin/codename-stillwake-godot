using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// The pointer's arrow and what stands in for it (pointer-looks): a stack carried, a builder's frame, a card's ghost. Each says
/// every frame whether it stands in; one place hides the arrow while any does, so none of them shows it under another.
/// </summary>
public sealed class PointerArrow
{
    private readonly SortedSet<string> _by = new(StringComparer.Ordinal);

    /// <summary>Whether <paramref name="who"/> stands in for the arrow now, said every frame it may change.</summary>
    public void StandIn(string who, bool stands)
    {
        if (stands) _by.Add(who);
        else _by.Remove(who);
    }

    /// <summary>Whether the arrow is hidden: something stands in for it, and no menu is open over the game.</summary>
    public bool Hidden(bool menuOpen) => _by.Count > 0 && !menuOpen;
}
