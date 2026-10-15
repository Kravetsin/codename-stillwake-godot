using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A point that lights what is round it a little, as an ember does, while it is shown: the planner's
/// screen in a hero's hand. The light feature draws every one in the tree each frame (0029).
/// </summary>
public partial class Glow : Node2D
{
    private static readonly HashSet<Glow> InTree = new();

    /// <summary>Every glow in the tree, shown or not.</summary>
    public static IReadOnlyCollection<Glow> All => InTree;

    /// <summary>The light it gives, its strength in the colour.</summary>
    public Color Colour { get; set; }

    public override void _EnterTree() => InTree.Add(this);

    public override void _ExitTree() => InTree.Remove(this);
}
