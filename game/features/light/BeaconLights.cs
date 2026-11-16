using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// A derelict's beacons blinking outside its hull (derelict-looks, <see cref="Beacons"/>): a halo over each glass while it flashes,
/// and its light through <see cref="LightView"/>, each only while its mast or boom stands, or for the bow's strobe its hull's wall.
/// No Godot light (0029). Phases run on this client's clock.
/// </summary>
public partial class BeaconLights : Node2D, ILightSource
{
    private const int Cell = ShipCamera.Cell;

    private readonly IGridQueries _grid;
    private readonly List<(Beacon Beacon, int Cell, Sprite2D Halo)> _beacons = [];
    private readonly List<LightSource> _lights = [];
    private double _timeMs;

    /// <param name="vessel">The vessel the surface holds, its frame's top-left at <c>Vessels.Margin</c> every way.</param>
    public BeaconLights(IGridQueries grid, Vessel vessel)
    {
        _grid = grid;
        int m = Content.Vessels.Margin;
        var offset = new Vector2(m, m) * Cell;
        foreach (var beacon in Beacons.Of(vessel))
        {
            var halo = new Sprite2D
            {
                Texture = PixelArt.Glow,
                Position = offset + new Vector2(beacon.X, beacon.Y),
                Modulate = new Color(beacon.Look.Halo),
                Scale = Vector2.One * beacon.Look.HaloCells * Cell / PixelArt.Glow.GetWidth(),
                ZIndex = Layers.Glows,
                Material = Additive,
                Visible = false,
            };
            AddChild(halo);
            _beacons.Add((beacon, (m + beacon.Cell.Y) * grid.Width + m + beacon.Cell.X, halo));
        }
    }

    public IReadOnlyList<LightSource> Lights => _lights;

    public override void _Process(double delta)
    {
        _timeMs += delta * 1000;
        _lights.Clear();
        for (int i = 0; i < _beacons.Count; i++)
        {
            var (beacon, cell, halo) = _beacons[i];
            float shines = Stands(beacon, cell) ? Beacons.Shines(beacon.Look, _timeMs, Beacons.PhaseOf(i)) : 0;
            halo.Visible = shines > 0;
            if (shines <= 0) continue;
            var look = beacon.Look;
            _lights.Add(new LightSource(halo.Position.X, halo.Position.Y, look.Colour, shines * look.Strength, look.Reach, look.Keep, Game.Light.LampMost));
        }
    }

    /// <summary>Whether what keeps <paramref name="beacon"/> lit still stands at <paramref name="cell"/>: its machine, or the hull's wall.</summary>
    private bool Stands(Beacon beacon, int cell) =>
        beacon.Kind is { } kind ? _grid.MachineAt(cell) is { } m && m.Kind == kind && m.Cell == cell : _grid.StructureAt(cell) != Content.Structure.Empty;

    /// <summary>A halo's: added over what lies under it, unlit, for it gives light.</summary>
    private static readonly CanvasItemMaterial Additive = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };
}
