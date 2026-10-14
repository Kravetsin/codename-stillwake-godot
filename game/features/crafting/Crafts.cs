using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Crafting;

/// <summary>What the crafts panel shows, without Godot: which crafts, what each costs against what is carried, how far a unit is.</summary>
public static class Crafts
{
    /// <summary>Units a shift-click queues; a plain click queues one.</summary>
    public const int Many = 5;

    /// <summary>The crafts the world knows, in content's order: every one in creative mode.</summary>
    public static IReadOnlyList<Recipe> Shown(IResearchQueries research) =>
        research.Recipes().Where(r => r.Placement == null && research.Knows(r.Id)).ToList();

    /// <summary>What one unit of <paramref name="craft"/> costs, item by item, against what is <paramref name="carried"/>.</summary>
    public static IReadOnlyList<CostLine> Lines(Recipe craft, IReadOnlyList<(string Item, int Count)> carried) =>
        CostLine.Of(craft.Cost, carried);

    /// <summary>Whether a line is short: never in creative mode, where nothing is taken.</summary>
    public static bool Short(CostLine line, bool creative) => !creative && line.Have < line.Need;

    /// <summary>How many units a click queues.</summary>
    public static int CountFor(bool shift) => shift ? Many : 1;

    /// <summary>How far the unit at work is, from 0 to 1: full while it waits for room.</summary>
    public static double Share(string recipe, int progress) =>
        Content.Crafts.TryGet(recipe, out var craft) ? (double)progress / craft.Ticks : 0;
}
