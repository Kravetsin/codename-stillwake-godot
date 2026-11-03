using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A point that lights what is round it a little, as an ember does, while it is shown: the planner's
/// screen in a hero's hand, the hero's tracker and a lit suit's bands. The light feature draws every one in the tree each frame (0029).
/// </summary>
public partial class Glow : Node2D
{
    private static readonly HashSet<Glow> InTree = new();

    /// <summary>Every glow in the tree, shown or not.</summary>
    public static IReadOnlyCollection<Glow> All => InTree;

    /// <summary>The light it gives, its strength in the colour.</summary>
    public Color Colour { get; set; }

    /// <summary>How many cells out it lights, and what of its light it keeps a cell: a planner's screen's unless set.</summary>
    public int Reach { get; init; } = Light.ScreenReach;

    /// <inheritdoc cref="Reach"/>
    public float Keep { get; init; } = Light.ScreenKeep;

    public override void _EnterTree() => InTree.Add(this);

    public override void _ExitTree() => InTree.Remove(this);
}
