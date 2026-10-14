using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Overlays;

/// <summary>An overlay: its feature's id, its name's i18n key and the key that switches it, by its place on the keyboard.</summary>
public sealed record Overlay(string Id, string Name, string Key);

/// <summary>
/// Which overlay is on, switched by keys (overlays/switch.ts): an overlay's key turns it on, and off
/// again; another's key turns that one on instead. Held keys repeat, so a repeat does nothing. Kept in
/// <paramref name="on"/>, which others may set too.
/// </summary>
public sealed class OverlaySwitch(IReadOnlyList<Overlay> overlays, Store<string?> on)
{
    /// <summary>The overlays of the game, in the order their features draw: air, pipes, power.</summary>
    public static readonly IReadOnlyList<Overlay> All =
    [
        new("atmosphere", "overlay.air", "O"),
        new("pipes", "overlay.pipes", "P"),
        new("power", "overlay.power", "L"),
    ];

    public IReadOnlyList<Overlay> Overlays => overlays;

    public string? Active => on.Get();

    /// <summary>A key pressed, by the letter on its place; <paramref name="repeat"/> when it is held and repeats.</summary>
    public void Press(string key, bool repeat = false)
    {
        var overlay = overlays.FirstOrDefault(o => o.Key == key);
        if (overlay == null || repeat) return;
        on.Set(on.Get() == overlay.Id ? null : overlay.Id);
    }
}
