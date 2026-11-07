using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// A wreck's cracks drawn over its picture (<see cref="WreckCracks"/>, damage-looks): a child of the picture, lit as it is, the
/// picture clipping it to its own pixels. Redrawn only when its damage changes, or the view's zoom does, for its edges' fade.
/// </summary>
public partial class WreckInk : Node2D
{
    private readonly Ink _ink = new();
    private readonly InkCanvas _canvas = new();
    private IReadOnlyList<Blow> _blows = [];
    private CrackNet? _net;
    private double _share;
    private int _seed;
    private float _pixel;

    public WreckInk() => UseParentMaterial = true;

    /// <summary>Shows <paramref name="blows"/> and <paramref name="net"/> with <paramref name="share"/> of its hit points lost, laid out by <paramref name="seed"/>.</summary>
    public void Show(IReadOnlyList<Blow> blows, CrackNet? net, double share, int seed)
    {
        (_blows, _net, _share, _seed) = (blows, net, share, seed);
        if (GetParent() is CanvasItem picture)
            picture.ClipChildren = blows.Count > 0 ? ClipChildrenMode.AndDraw : ClipChildrenMode.Disabled;
        QueueRedraw();
    }

    /// <summary>Redrawn if the view has zoomed since: its edges fade over a pixel of the screen.</summary>
    public void Zoomed()
    {
        float pixel = InkCanvas.PixelOf(this);
        if (_blows.Count > 0 && Mathf.Abs(pixel - _pixel) > 0.1f * _pixel) QueueRedraw();
    }

    public override void _Draw()
    {
        _pixel = InkCanvas.PixelOf(this);
        _ink.Clear();
        _ink.Feather = _pixel;
        WreckCracks.Draw(_ink, _blows, _net, _share, _seed);
        _canvas.Draw(this, _ink);
    }
}
