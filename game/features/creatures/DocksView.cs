using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A vessel's guards' docks (derelict-enemies), from <see cref="CreaturesModule.Docks"/>, listed once as its world opens: a spider's
/// capsule G1 on its room's back wall, shut, its hazard lamp's amber standby pip blinking with a little light; a robot's stand S1 on
/// its room's floor, its yoke and clamp in front. A capsule is drawn with the back-wall objects; a stand before the machines, for it
/// stands over what furniture its floor holds; both behind bodies and under the unseen rooms' fog.
/// Asleep only, for now (the plan's G-1): the guard inside is not drawn (<see cref="CreaturesView"/>).
/// </summary>
public partial class DocksView : Node2D, ILightSource
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>Each dock's picture's top-left in the world's pixels, and whether it is a capsule; a capsule's picture holds the row over its box.</summary>
    private readonly (Vector2 Corner, bool Capsule)[] _docks;
    private readonly Sketch _back = new(Layers.Pipes, lit: true), _stands = new(Layers.Fittings, lit: true), _pips = new(Layers.Pipes);
    private readonly List<LightSource> _lights = [];
    private double _timeMs;

    /// <summary>Where in the capsule's picture its hazard lamp's pip is, in pixels from its top-left (guards.js: the face's upper right).</summary>
    private static readonly Vector2 Pip = new(31.03f, 12.97f);

    public DocksView(IReadOnlyList<Shipyard.Dock> docks)
    {
        int m = Content.Vessels.Margin;
        _docks = new (Vector2, bool)[docks.Count];
        for (int i = 0; i < docks.Count; i++)
        {
            var d = docks[i];
            bool capsule = d.Rule == Content.Vessels.Aboard.Capsule;
            _docks[i] = (new Vector2(m + d.X, m + d.Y - (capsule ? 1 : 0)) * Cell, capsule);
        }
        Lit.Set(_back);
        Lit.Set(_stands);
        AddChild(_back);
        AddChild(_stands);
        AddChild(_pips);
        _back.Paint = g => Paint(g, capsules: true);
        _stands.Paint = g => Paint(g, capsules: false);
        _pips.Paint = PaintPips;
    }

    /// <summary>The docks of a vessel world's guards; null for a surface with none.</summary>
    public static DocksView? Of(CreaturesModule creatures) => creatures.Docks.Count > 0 ? new DocksView(creatures.Docks) : null;

    public IReadOnlyList<LightSource> Lights => _lights;

    public override void _Process(double delta)
    {
        _timeMs += delta * 1000;
        _lights.Clear();
        var view = GetCanvasTransform().AffineInverse() * GetViewportRect();
        var pip = L.GuardPip;
        for (int i = 0; i < _docks.Length; i++)
        {
            if (!_docks[i].Capsule || !On(i)) continue;
            var at = _docks[i].Corner + Pip;
            if (view.HasPoint(at)) _lights.Add(new LightSource(at.X, at.Y, pip.Colour, pip.Strength, pip.Reach, pip.Keep, 1));
        }
        _pips.QueueRedraw();
    }

    /// <summary>Whether capsule <paramref name="i"/>'s pip is lit now, on this client's clock.</summary>
    private bool On(int i) => (_timeMs + i * L.GuardPipStaggerMs) % L.GuardPipEveryMs < L.GuardPipOnMs;

    private void Paint(Sketch g, bool capsules)
    {
        foreach (var (corner, capsule) in _docks)
        {
            if (capsule != capsules) continue;
            if (capsule) g.DrawTexture(PixelArt.Picture("guard_capsule"), corner);
            else
            {
                g.DrawTexture(PixelArt.Picture("guard_stand"), corner);
                g.DrawTexture(PixelArt.Picture("guard_stand_front"), corner);
            }
        }
    }

    private void PaintPips(Sketch g)
    {
        var colour = new Color(L.GuardPip.Halo);
        for (int i = 0; i < _docks.Length; i++)
            if (_docks[i].Capsule && On(i)) g.DrawCircle(_docks[i].Corner + Pip, L.GuardPipSize / 2, colour);
    }
}
