using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Effects;

/// <summary>
/// The world's stages of effects drawn (effects-on-their-layer): an <see cref="EffectsView"/> for each, at its layer's z,
/// made the frame after the stage first is. Kept last among the world's views (<c>Main.ShowWorld</c>), so at a shared z a
/// stage draws over its layer's objects. Leaving the tree, it forgets every stage: the scene that drew them is going.
/// </summary>
public partial class StagesView : Node2D
{
    private readonly Dictionary<Stage, EffectsView> _views = [];

    /// <summary>Its views' cost a frame, summed, on average since last asked, in ms: for <c>--measure</c>.</summary>
    public double TakeCostMs()
    {
        double ms = 0;
        foreach (var view in _views.Values) ms += view.TakeCostMs();
        return ms;
    }

    public override void _Process(double delta)
    {
        foreach (var stage in Stages.WorldStages)
        {
            if (_views.ContainsKey(stage)) continue;
            var view = new EffectsView(stage) { ZAsRelative = false, ZIndex = stage.Z!.Value };
            _views[stage] = view;
            AddChild(view);
        }
    }

    public override void _ExitTree() => Stages.Reset();
}
