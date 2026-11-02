using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Water;

namespace Stillwake.Game.Features.Water;

/// <summary>
/// Water without a word (water): each cistern's water up its sight glass, each electrolyser's bubbles rising while it works and
/// its lamp red while it stands (<see cref="WaterShows"/>). Drawn unlit over their pictures, for the glass and the lamp glow.
/// </summary>
public partial class WaterLooks : Node2D
{
    /// <summary>What they hold and do is read this often: a cistern empties a unit a tick, an electrolyser stalls rarely.</summary>
    private const double RefreshMs = 250;

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly IGridQueries _grid;
    private readonly IWaterQueries _water;
    /// <summary>Each machine's overlay, by machine id, and for an electrolyser whether it works.</summary>
    private readonly Dictionary<int, (Sprite2D Over, bool Works)> _shown = new();
    private readonly HashSet<int> _seen = new();
    private readonly List<int> _gone = new();
    private double _sinceRead = RefreshMs, _ms;

    public WaterLooks(IGridQueries grid, IWaterQueries water)
    {
        _grid = grid;
        _water = water;
        ZIndex = Layers.Screens;
    }

    public override void _Process(double delta)
    {
        _ms += delta * 1000;
        if ((_sinceRead += delta * 1000) >= RefreshMs)
        {
            _sinceRead = 0;
            Read();
        }
        int frame = WaterShows.WorkFrame(_ms);
        foreach (var (over, works) in _shown.Values)
            if (works) over.Texture = PixelArt.Picture($"electrolyser_work_{frame}");
    }

    private void Read()
    {
        _seen.Clear();
        foreach (var m in _grid.MachinesWith(Stillwake.Content.MachineTrait.HoldsWater))
            Show(m, PixelArt.Picture($"cistern_fill_{WaterShows.CisternFrame(_water.WaterIn(m.Id))}"), false);
        foreach (var m in _grid.MachinesWith(Stillwake.Content.MachineTrait.Electrolyses))
        {
            bool works = _water.Stalled(m.Id) == null;
            Show(m, PixelArt.Picture(works ? "electrolyser_work_0" : "electrolyser_stalled"), works);
        }
        _gone.Clear();
        foreach (int id in _shown.Keys)
            if (!_seen.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            _shown[id].Over.QueueFree();
            _shown.Remove(id);
        }
    }

    private void Show(Machine m, Texture2D picture, bool works)
    {
        _seen.Add(m.Id);
        if (!_shown.TryGetValue(m.Id, out var shown))
        {
            shown = (new Sprite2D { Centered = false, Material = Unlit }, works);
            AddChild(shown.Over);
        }
        shown.Over.Position = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * ShipCamera.Cell;
        shown.Over.Texture = picture;
        _shown[m.Id] = (shown.Over, works);
    }
}
