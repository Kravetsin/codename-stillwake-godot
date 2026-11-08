using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Render;

/// <summary>
/// The picture of a good the mafia sells (2026-11-08-mafia-deliveries): an upgrade's sign, a blueprint's thumbnail, any item's icon.
/// Shown in a delivery's marker at the screen's edge and in the tally of what one gave.
/// </summary>
public static class GoodIcons
{
    private static readonly Dictionary<string, Placement> Placements = Blueprints.All.ToDictionary(b => b.Id, b => b.Placement, StringComparer.Ordinal);

    /// <summary>The picture of <paramref name="good"/>, a tool's baked <paramref name="side"/> px wide.</summary>
    public static Texture2D Of(string good, int side) => Sign(good) ?? ItemIcons.Icon(good, side);

    /// <summary>The picture of <paramref name="good"/>, a tool's as its held picture.</summary>
    public static Texture2D Of(string good) => Sign(good) ?? ItemIcons.Icon(good);

    /// <summary>An upgrade's sign or a blueprint's thumbnail; none for an item, whose icon wins where a blueprint shares its id (arcLamp).</summary>
    private static Texture2D? Sign(string good) =>
        Stillwake.Content.Items.TryGet(good, out _) ? null
        : Stillwake.Content.Upgrades.Of(good) is { } upgrade ? PixelArt.Picture($"upgrade_{upgrade.Kind}")
        : Placements.TryGetValue(good, out var placement) ? BlueprintArt.Thumbnail(placement)
        : null;
}
