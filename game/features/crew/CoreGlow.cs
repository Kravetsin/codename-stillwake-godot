using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The alien core smouldering where it lies (drone-companion): an ember halo over it, breathing, and its dim light, so a hero finds
/// it in the dark of the landing. Gone with the core for a player who has their drone, or once it is lost. No Godot light (0029).
/// </summary>
public partial class CoreGlow : Node2D, ILightSource
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The core's middle in its 2-cell picture, in world px from its top-left (drone.js's <c>machine_alienCore</c>).</summary>
    private static readonly Vector2 Middle = new(8, 14.4f);
    /// <summary>One breath, in ms; how bright the light at its fullest, how far it reaches in cells; the halo's width in cells.</summary>
    private const double BreathMs = 2600;
    private const float Strength = 0.45f, HaloCells = 2.5f;
    private const int Reach = 7;
    private static readonly (float R, float G, float B) Ember = (1f, 0.54f, 0.23f);

    private readonly Func<bool> _gone;
    private readonly List<Sprite2D> _halos = [];
    private readonly List<LightSource> _lights = [];
    private double _timeMs;

    /// <param name="gone">Whether this player sees the core no more: paired, or the core lost.</param>
    public CoreGlow(IGridQueries grid, Func<bool> gone)
    {
        _gone = gone;
        foreach (var core in grid.MachinesWith(Content.MachineTrait.Wakes))
        {
            var halo = new Sprite2D
            {
                Texture = PixelArt.Glow,
                Position = new Vector2(core.Cell % grid.Width, core.Cell / grid.Width) * Cell + Middle,
                Modulate = new Color(Ember.R, Ember.G, Ember.B),
                Scale = Vector2.One * HaloCells * Cell / PixelArt.Glow.GetWidth(),
                ZIndex = Layers.Glows,
                Material = Additive,
            };
            AddChild(halo);
            _halos.Add(halo);
        }
    }

    public IReadOnlyList<LightSource> Lights => _lights;

    public override void _Process(double delta)
    {
        _timeMs += delta * 1000;
        _lights.Clear();
        bool shown = _halos.Count > 0 && !_gone();
        float breath = (float)(0.7 + 0.3 * Math.Sin(_timeMs / BreathMs * Math.Tau));
        foreach (var halo in _halos)
        {
            halo.Visible = shown;
            if (!shown) continue;
            halo.SelfModulate = new Color(1, 1, 1, 0.55f * breath);
            _lights.Add(new LightSource(halo.Position.X, halo.Position.Y, Ember, Strength * breath, Reach, L.LampKeep, L.LampMost));
        }
    }

    /// <summary>A halo's: added over what lies under it, unlit, for it gives light.</summary>
    private static readonly CanvasItemMaterial Additive = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };
}
