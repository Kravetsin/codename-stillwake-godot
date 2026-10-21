using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// The stone's colour by depth band (0080): one band of colour a band of rows, multiplied over the ground drawn under it —
/// walls, their edges, back walls — and nothing over it, so the growths and the light keep their own colours. The coldest
/// band a cool blue-grey, the garden as it is, the amber deep warmer, the ember band red-brown, blended from each band's middle
/// to the next (<see cref="CaveLooks.Tint"/>); none above <see cref="CaveLooks.TintFrom"/> rows under the surface's middle.
/// A quad a band across the view, drawn again only when the view moves.
/// </summary>
public partial class CaveTint : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private readonly List<(float Y, Color Colour)> _stops = [];
    private Rect2 _view;

    public CaveTint(Spec.Ground ground)
    {
        ZIndex = Layers.Edges;
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Mul, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        var bands = Spec.Flora.Bands;
        int span = ground.Height - ground.CoreRows - ground.Surface;
        Color Of(int band) => new(CaveLooks.Tint[band].R, CaveLooks.Tint[band].G, CaveLooks.Tint[band].B);
        _stops.Add(((ground.Surface + CaveLooks.TintFrom) * Cell, Colors.White));
        _stops.Add(((ground.Surface + CaveLooks.TintFrom + CaveLooks.TintOver) * Cell, Of(0)));
        for (int band = 1; band <= bands.Count; band++)
        {
            int top = bands[band - 1], bottom = band < bands.Count ? bands[band] : 1000;
            _stops.Add(((ground.Surface + span * (top + bottom) / 2000f) * Cell, Of(band)));
        }
        _stops.Add((ground.Height * Cell, Of(bands.Count)));
    }

    public override void _Process(double delta)
    {
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewportRect();
        if (view == _view) return;
        _view = view;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float left = _view.Position.X - Cell, right = _view.End.X + Cell, top = _view.Position.Y - Cell, bottom = _view.End.Y + Cell;
        for (int i = 0; i + 1 < _stops.Count; i++)
        {
            var (y0, c0) = _stops[i];
            var (y1, c1) = _stops[i + 1];
            if (y1 < top || y0 > bottom) continue;
            // Clipped to the view, its colours where the clip falls.
            float a = Math.Max(y0, top), b = Math.Min(y1, bottom);
            Color ca = c0.Lerp(c1, (a - y0) / (y1 - y0)), cb = c0.Lerp(c1, (b - y0) / (y1 - y0));
            DrawPolygon([new(left, a), new(right, a), new(right, b), new(left, b)], [ca, ca, cb, cb]);
        }
    }
}
