using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Dev;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Grid;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Net;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game;

/// <summary>
/// The game: a session (net/README.md) of its own, or with <c>-- --host</c> one others join over ENet
/// with <c>-- --join[=address]</c>; the world it plays drawn by the client's features after every
/// tick, and the hero: A/D walk, W/S climb, Space jumps, E works doors and valves; the camera follows it.
/// Dev arguments after <c>--</c>: <c>--perf</c> the reference world, <c>--gallery</c> every picture,
/// <c>--view X,Y,ZOOM</c> where the camera looks, <c>--doors</c> toggles doors for recordings,
/// <c>--autopilot</c> plays the hero by a script, <c>--gym</c> a room with a ladder for it,
/// <c>--measure</c> prints what frames cost and quits, <c>--for=seconds</c> quits after a while,
/// <c>--auto</c> opens a door per peer, <c>--cheat</c> makes a joiner's world differ so the host must
/// resync it, <c>--lag</c> and <c>--jitter</c> make a joiner's link bad on purpose.
/// </summary>
public partial class Main : Node2D
{
    private const int Port = 24600;
    private static readonly string Build =
        typeof(Main).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    private Session? _session;
    private Registry _m = null!;
    /// <summary>The registry the views draw: a load or a resync brings a new one.</summary>
    private Registry? _shown;
    private GridView? _grid;
    private LightView? _light;
    private ShipCamera _camera = null!;
    private string[] _args = [];
    private string _role = "alone";
    private bool _cheat;
    private Action _poll = () => { };
    private readonly List<int> _doors = [];
    private int _heroes, _start;
    private double _worstSyncMs;
    private Layout _layout = Registry.Wreck;
    private HeroView? _hero;

    public override void _Ready()
    {
        _args = OS.GetCmdlineUserArgs();
        _cheat = Has("--cheat");
        var seconds = _args.FirstOrDefault(a => a.StartsWith("--for="));
        if (seconds != null) GetTree().CreateTimer(int.Parse(seconds[6..])).Timeout += Quit;

        AddChild(new Stars());
        AddChild(_camera = new ShipCamera());
        if (Has("--gym")) _layout = HeroAutopilot.Gym;
        // The dev worlds are looked at, not walked: the camera moves by hand there.
        if (!Has("--perf") && !Has("--gallery"))
        {
            var script = !Has("--autopilot") ? null : Has("--gym") ? HeroAutopilot.GymRun() : HeroAutopilot.Wreck();
            AddChild(_hero = new HeroView(script));
            // Its own layer, above the world: read from whatever registry the session runs now.
            AddChild(new SuitGauge(() => _session is Session s ? Danger.Read(_m.Crew, _m.Atmosphere, s.Client.Id) : (Content.Crew.Suit, SuitDanger.None)));
            _camera.DevControls = false;
        }
        var join = _args.FirstOrDefault(a => a == "--join" || a.StartsWith("--join="));
        if (Has("--host")) Host();
        else if (join != null) Join(join.Length > 7 ? join[7..] : "127.0.0.1", BadNetwork.From(_args));
        else Alone();
        if (Has("--measure"))
            AddChild(new PerfReadout(() => $"sync-worst={_worstSyncMs:F2}ms lamps={_light?.LampCount} occluders={_light?.OccluderCount}"));
    }

    private bool Has(string flag) => _args.Contains(flag);
    private string? Arg(string flag) => _args.SkipWhile(a => a != flag).Skip(1).FirstOrDefault();

    private IReadOnlyList<Sim.Module> Modules()
    {
        _m = new Registry();
        return _m.All;
    }

    private void Alone()
    {
        var modules = Modules();
        var layout = Has("--perf") ? DevWorlds.Perf() : _layout;
        var world = Has("--gallery") ? DevWorlds.Gallery(modules) : World.Create(1, layout, modules);
        var (host, client) = LocalSession.Create(world, Build);
        _session = new Session(host, client);
        ShowWorld();
        AimCamera(Has("--gallery") ? null : layout);
    }

    private void Host()
    {
        _role = "host";
        var (host, client) = LocalSession.Create(World.Create(1, _layout, Modules()), Build);
        _session = new Session(host, client);
        ShowWorld();
        AimCamera(_layout);
        var (link, error) = EnetLink<ToHost, ToClient>.Host(Port);
        if (error != Error.Ok) GD.PrintErr($"[host] cannot listen on {Port}: {error}");
        var ids = new Dictionary<long, int>();
        link.Connected += (enet, channel) =>
        {
            ids[enet] = host.Admit(channel);
            Log($"peer {ids[enet]} joined at tick {host.Tick}");
        };
        link.Disconnected += enet =>
        {
            // Its hero leaves with it, its suit colour freed.
            if (ids.Remove(enet, out int id)) host.Disconnect(id, client.World.Encode(new Sim.Crew.Leave()));
            Log($"peer {id} left");
        };
        _poll = link.Poll;
    }

    private void Join(string address, BadNetwork? network)
    {
        _role = "guest";
        var (link, error) = EnetLink<ToClient, ToHost>.Join(address, Port);
        if (error != Error.Ok) GD.PrintErr($"[guest] cannot reach {address}: {error}");
        link.Connected += (_, channel) =>
        {
            Log($"connected to {address}{(network != null ? $" over {network}" : "")}");
            Client.Join(Load, network?.Over(channel, new GodotTiming(GetTree())) ?? channel, Build,
                client =>
                {
                    _session = new Session(null, client);
                    _start = client.World.Tick;
                    ShowWorld();
                    AimCamera(Registry.Wreck);
                    Log($"joined as peer {client.Id} at tick {client.World.Tick}");
                },
                why =>
                {
                    Log(why.Message);
                    GetTree().Quit(1);
                });
        };
        link.Disconnected += _ =>
        {
            Log("the host left");
            Quit();
        };
        _poll = link.Poll;
    }

    /// <summary>A world from the host's save: a joiner's first, or a resync's.</summary>
    private World Load(string save)
    {
        if (_cheat)
        {
            // Once: a door this peer alone sees open, as a determinism bug would leave it.
            _cheat = false;
            var root = JsonNode.Parse(save)!;
            var structure = root["slices"]!["grid"]!["structure"]!.AsArray();
            int door = structure.Select(n => (int)n!).ToList().IndexOf(Structure.DoorClosed);
            structure[door] = Structure.DoorOpen;
            save = root.ToJsonString();
            Log($"cheated: door {door} open here only");
        }
        return World.Load(save, Modules());
    }

    /// <summary>The views drawing the current registry's world, made again when a load brings a new one.</summary>
    private void ShowWorld()
    {
        if (_shown == _m) return;
        _grid?.QueueFree();
        _light?.QueueFree();
        AddChild(_grid = new GridView(_m.Grid));
        var power = _m.Power;
        AddChild(_light = new LightView(_m.Grid) { LampShare = lamp => power.Satisfaction(lamp.Id) / 1000f });
        if (_hero != null) MoveChild(_hero, -1);
        MoveChild(_camera, -1);
        _shown = _m;
        Sync();
    }

    private void AimCamera(Layout? layout)
    {
        var start = layout?.CellsOf('S').FirstOrDefault(-1) ?? -1;
        if (Has("--perf")) _camera.Fit(new Rect2(0, 0, _m.Grid.Width, _m.Grid.Height));
        else if (start >= 0) _camera.Follow(new Vector2(start % _m.Grid.Width + 0.5f, start / _m.Grid.Width));
        else _camera.Follow(new Vector2(_m.Grid.Width, _m.Grid.Height) / 2);
        if (Arg("--view") is { } view)
        {
            var v = view.Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            _camera.Follow(new Vector2(v[0], v[1]));
            if (v.Length > 2) _camera.SetZoom(v[2]);
        }
    }

    public override void _Process(double delta)
    {
        _poll();
        if (_session is not Session session) return;
        var client = session.Client;
        int before = client.World.Tick;
        int desyncs = session.Desyncs.Count;
        foreach (var report in session.Frame(delta * 1000))
            foreach (var refused in report.Refused)
                Log($"tick {report.Tick}: {refused.Command.GetType().Name} from {refused.Command.From} refused, {refused.Reason}");
        foreach (var report in session.Desyncs.Skip(desyncs)) Log($"desync, resynced from the host: {report}");
        if (_role != "alone" && client.World.Tick / 100 != before / 100) Log($"tick {client.World.Tick} hash {client.World.Hash():x16}");
        _heroes += client.TakeHeroes().Count;
        if (Has("--auto")) Auto(client, before);
        if (Has("--doors") && client.World.Tick != before)
            foreach (var c in Autopilot.At(client.World.Tick, _m.Grid)) client.Send(c);
        ShowWorld();
        if (client.World.Tick != before) Sync();
        _hero?.Frame(client, _m.Grid, _m.Rooms, _m.Atmosphere, _m.Crew, _camera, delta * 1000);
    }

    private void Sync()
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        _grid?.Sync();
        _light?.Sync();
        _worstSyncMs = Math.Max(_worstSyncMs, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    /// <summary>Each peer opens a door 30 ticks into its session, host and guest each their own; both say where they are.</summary>
    private void Auto(Client client, int before)
    {
        if (_doors.Count == 0)
            for (int cell = 0; cell < _m.Grid.Width * _m.Grid.Height; cell++)
                if (_m.Grid.StructureAt(cell) == Structure.DoorClosed) _doors.Add(cell);
        int at = _start + 30;
        if (before < at && client.World.Tick >= at && _doors.Count > 1)
        {
            client.Send(new ToggleDoor(_doors[_role == "guest" ? 1 : 0]));
            Log($"sent ToggleDoor({_doors[_role == "guest" ? 1 : 0]}) at tick {client.World.Tick}, {client.InFlight.Count} in flight");
        }
        if (client.World.Tick / 20 != before / 20) client.SendHero($"{{\"tick\":{client.World.Tick}}}");
    }

    private void Quit()
    {
        if (_session is Session s)
        {
            var c = s.Client;
            Log($"quitting at tick {c.World.Tick} hash {c.World.Hash():x16}, last checked hash {c.LastHash:x16}, " +
                $"desyncs {s.Desyncs.Count}, in flight {c.InFlight.Count}, round trip {s.RoundTrip.Median:0} ms" +
                (s.Reserve is double reserve ? $", reserve {reserve:0} ms" : "") + $", heroes heard {_heroes}");
        }
        GetTree().Quit();
    }

    private void Log(string text) => GD.Print($"[{_role}] {text}");
}
