using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Fuel;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Fuel;

/// <summary>
/// Fuel without a word (fuel-liquid): the biofuel of each store with a sight glass in its picture (a fuel tank: <c>fuelTank_level</c>)
/// up that glass by any level (<see cref="GlassLevel"/>). Stores with none (the pod, the generator, the engine) show nothing here.
/// </summary>
public partial class FuelLooks : Node2D
{
    /// <summary>What the stores hold is read this often: a line pours a litre in some 2.5 seconds.</summary>
    private const double RefreshMs = 250;

    private readonly IGridQueries _grid;
    private readonly IFuelQueries _fuel;
    private readonly Dictionary<int, GlassLevel> _glasses = new();
    /// <summary>Whether a kind has a sight glass, found once a kind.</summary>
    private readonly Dictionary<string, bool> _glassed = new();
    private readonly HashSet<int> _seen = new();
    private readonly List<int> _gone = new();
    private double _sinceRead = RefreshMs;

    /// <summary>Whether a machine is held out of the grid's drawing, a shuttle just come and drawn arriving: its glass not shown yet.</summary>
    public System.Func<int, bool> HeldBack { get; init; } = _ => false;

    public FuelLooks(IGridQueries grid, IFuelQueries fuel)
    {
        _grid = grid;
        _fuel = fuel;
        ZIndex = Layers.Screens;
    }

    public override void _Process(double delta)
    {
        if ((_sinceRead += delta * 1000) < RefreshMs) return;
        _sinceRead = 0;
        _seen.Clear();
        foreach (var m in _grid.MachinesWith(Stillwake.Content.MachineTrait.HoldsFuel))
        {
            if (!_glassed.TryGetValue(m.Kind, out bool glassed)) _glassed[m.Kind] = glassed = GlassLevel.Has(m.Kind);
            if (!glassed || HeldBack(m.Id)) continue;
            _seen.Add(m.Id);
            if (!_glasses.TryGetValue(m.Id, out var glass)) AddChild(_glasses[m.Id] = glass = new GlassLevel(m.Kind));
            glass.Position = new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * ShipCamera.Cell;
            glass.Show(_fuel.FuelIn(m.Id), Stillwake.Content.Machines.Get(m.Kind).FuelCapacity);
        }
        _gone.Clear();
        foreach (int id in _glasses.Keys)
            if (!_seen.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            _glasses[id].QueueFree();
            _glasses.Remove(id);
        }
    }
}
