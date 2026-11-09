using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The pale trail a combo's blade leaves (<see cref="BladeTrail"/>, axe-combos): a wash over the band its edge swept, behind the
/// figure in the body's frame, lit like the blade. Across the edge three strips, the outer two at half, keep it from a hard rim.
/// Redrawn only while some of it shows.
/// </summary>
public partial class BladeTrailView : Node2D
{
    /// <summary>The strips across the edge, from its inner end to its outer, and how opaque each is of the band.</summary>
    private static readonly (float From, float To, float Share)[] Strips = [(0, 0.25f, 0.5f), (0.25f, 0.75f, 1), (0.75f, 1, 0.5f)];

    private static readonly Color Colour = new(Combos.TrailColour);

    private BladeTrail? _trail;
    private readonly List<Vector2> _points = [];
    private readonly List<Color> _colours = [];
    private readonly List<int> _indices = [];

    public BladeTrailView()
    {
        Lit.Set(this);
        Visible = false;
    }

    /// <summary>Shows <paramref name="trail"/>, its edges in this node's frame; none, or none of it live, hides it.</summary>
    public void Show(BladeTrail? trail)
    {
        bool was = Visible;
        _trail = trail is { Live: true } ? trail : null;
        Visible = _trail != null;
        if (Visible || was) QueueRedraw();
    }

    public override void _Draw()
    {
        if (_trail is not { } trail) return;
        _points.Clear();
        _colours.Clear();
        _indices.Clear();
        for (int i = 0; i < trail.Opacity.Count; i++)
        {
            if (trail.Opacity[i] <= 0) continue;
            var (a, b) = (trail.Edges[i], trail.Edges[i + 1]);
            foreach (var (from, to, share) in Strips)
            {
                var colour = Colour with { A = (float)trail.Opacity[i] * share };
                int at = _points.Count;
                _points.Add(Along(a, from));
                _points.Add(Along(a, to));
                _points.Add(Along(b, to));
                _points.Add(Along(b, from));
                for (int k = 0; k < 4; k++) _colours.Add(colour);
                _indices.AddRange([at, at + 1, at + 2, at, at + 2, at + 3]);
            }
        }
        if (_indices.Count > 0)
            RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), [.. _indices], [.. _points], [.. _colours]);
    }

    /// <summary>The point <paramref name="share"/> of the way along <paramref name="edge"/> from its inner end.</summary>
    private static Vector2 Along((Vec From, Vec To) edge, float share) =>
        new((float)(edge.From.X + (edge.To.X - edge.From.X) * share), (float)(edge.From.Y + (edge.To.Y - edge.From.Y) * share));
}
