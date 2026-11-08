using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Render;

/// <summary>
/// The picture of a good the mafia sells (2026-11-08-mafia-deliveries): an upgrade's sign, a blueprint's thumbnail, any other
/// item's icon; the seized container its own picture. Shown over its delivery drifting in and in its marker at the screen's edge.
/// </summary>
public static class GoodIcons
{
    private static readonly Dictionary<string, Placement> Placements = Blueprints.All.ToDictionary(b => b.Id, b => b.Placement, StringComparer.Ordinal);

    /// <summary>The picture of <paramref name="good"/>, a tool's baked <paramref name="side"/> px wide.</summary>
    public static Texture2D Of(string good, int side) =>
        Stillwake.Content.Upgrades.Of(good) is { } upgrade ? PixelArt.Picture($"upgrade_{upgrade.Kind}")
        : DebrisLooks.NameOf(good, 0) is { } piece ? PixelArt.Picture(piece)
        : Placements.TryGetValue(good, out var placement) ? BlueprintArt.Thumbnail(placement)
        : ItemIcons.Icon(good, side);
}
