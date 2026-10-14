using System.Linq;
using Godot;
using Stillwake.Game.Dev;
using Stillwake.Game.Features.Grid;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim;

namespace Stillwake.Game;

/// <summary>
/// The wreck from the C# sim, stepped at the physics rate, 20 a second, and drawn by the client's
/// features after every tick. Until the hero is ported the camera is moved by hand.
/// Dev arguments after <c>--</c>: <c>--perf</c> the reference world, <c>--gallery</c> every picture,
/// <c>--view X,Y,ZOOM</c> where the camera looks, <c>--autopilot</c> toggles doors for recordings,
/// <c>--measure</c> prints what frames cost and quits.
/// </summary>
public partial class Main : Node2D
{
    private World _world = null!;
    private Registry _m = null!;
    private GridView _grid = null!;
    private LightView _light = null!;
    private ShipCamera _camera = null!;
    private string[] _args = [];
    private double _worstSyncMs;

    public override void _Ready()
    {
        _args = OS.GetCmdlineUserArgs();
        _m = new Registry();
        bool perf = Has("--perf");
        Layout? layout = null;
        if (Has("--gallery")) _world = DevWorlds.Gallery(_m.All);
        else _world = World.Create(1, layout = perf ? DevWorlds.Perf() : Registry.Wreck, _m.All);

        AddChild(new Stars());
        AddChild(_grid = new GridView(_m.Grid));
        AddChild(_light = new LightView(_m.Grid));
        AddChild(_camera = new ShipCamera());
        Sync();
        _worstSyncMs = 0;

        var start = layout?.CellsOf('S').FirstOrDefault(-1) ?? -1;
        if (perf) _camera.Fit(new Rect2(0, 0, _m.Grid.Width, _m.Grid.Height));
        else if (start >= 0) _camera.Follow(new Vector2(start % _m.Grid.Width + 0.5f, start / _m.Grid.Width));
        else _camera.Follow(new Vector2(_m.Grid.Width, _m.Grid.Height) / 2);
        if (Arg("--view") is { } view)
        {
            var v = view.Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            _camera.Follow(new Vector2(v[0], v[1]));
            if (v.Length > 2) _camera.SetZoom(v[2]);
        }
        if (Has("--measure"))
            AddChild(new PerfReadout(() => $"sync-worst={_worstSyncMs:F2}ms lamps={_light.LampCount} occluders={_light.OccluderCount}"));
    }

    private bool Has(string flag) => _args.Contains(flag);
    private string? Arg(string flag) => _args.SkipWhile(a => a != flag).Skip(1).FirstOrDefault();

    public override void _PhysicsProcess(double delta)
    {
        var commands = Has("--autopilot") ? Autopilot.At(_world.Tick, _m.Grid) : [];
        _world.Step(commands);
        Sync();
    }

    private void Sync()
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        _grid.Sync();
        _light.Sync();
        _worstSyncMs = System.Math.Max(_worstSyncMs, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
}
