using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Net;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;

namespace Stillwake.Game;

/// <summary>
/// The wreck from the C# sim, drawn cell by cell: rooms in colour, space dark. A placeholder until the
/// client's features are ported (docs/plans/port.md). It plays in a session (net/README.md): its own,
/// or with <c>-- --host</c> one others join over ENet with <c>-- --join[=address]</c>.
/// Dev flags: <c>--for=seconds</c> quits after a while, <c>--auto</c> plays a short script,
/// <c>--cheat</c> makes a joiner's world differ so the host must resync it, <c>--lag</c> and
/// <c>--jitter</c> make a joiner's link bad on purpose. The hero walks on A/D, climbs on W/S, jumps on
/// Space and works doors with E; <c>--autopilot</c> plays it by a script, <c>--gym</c> in a room with a ladder.
/// </summary>
public partial class Main : Node2D
{
    private const int Port = 24600;
    private const int CellPx = (int)Rig.Cell;
    private static readonly string Build =
        typeof(Main).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    private Session? _session;
    private Registry _m = null!;
    private string _role = "alone";
    private bool _auto, _cheat;
    private Action _poll = () => { };
    private readonly List<int> _doors = [];
    private int _heroes, _start;
    private Layout _layout = Registry.Wreck;
    private int _spawn;
    private HeroView _hero = null!;
    private readonly Camera2D _camera = new() { Zoom = new Vector2(2, 2) };
    private readonly Texture2D _ladder = GD.Load<Texture2D>("res://art/ladder.png"), _floor = GD.Load<Texture2D>("res://art/floor.png");

    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        _auto = args.Contains("--auto");
        _cheat = args.Contains("--cheat");
        if (args.Contains("--gym")) _layout = HeroAutopilot.Gym;
        _spawn = _layout.CellsOf('S').First();
        var autopilot = !args.Contains("--autopilot") ? null : args.Contains("--gym") ? HeroAutopilot.GymRun() : HeroAutopilot.Wreck();
        AddChild(_hero = new HeroView(autopilot));
        AddChild(_camera);
        RenderingServer.SetDefaultClearColor(new Color(0.02f, 0.025f, 0.04f));
        var seconds = args.FirstOrDefault(a => a.StartsWith("--for="));
        if (seconds != null) GetTree().CreateTimer(int.Parse(seconds[6..])).Timeout += Quit;
        var join = args.FirstOrDefault(a => a == "--join" || a.StartsWith("--join="));
        if (args.Contains("--host")) Host();
        else if (join != null) Join(join.Length > 7 ? join[7..] : "127.0.0.1", BadNetwork.From(args));
        else
        {
            var (host, client) = LocalSession.Create(World.Create(1, _layout, Modules()), Build);
            _session = new Session(host, client);
        }
    }

    private IReadOnlyList<Sim.Module> Modules()
    {
        _m = new Registry();
        return _m.All;
    }

    private void Host()
    {
        _role = "host";
        var (host, client) = LocalSession.Create(World.Create(1, _layout, Modules()), Build);
        _session = new Session(host, client);
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
            // No farewell yet: crew.leave comes with the crew module (G-04).
            if (ids.Remove(enet, out int id)) host.Disconnect(id);
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
        if (client.World.Tick / 100 != before / 100) Log($"tick {client.World.Tick} hash {client.World.Hash():x16}");
        _heroes += client.TakeHeroes().Count;
        if (_auto) Autopilot(client, before);
        _hero.Frame(client, _m.Grid, _m.Rooms, _camera, _spawn, passedOut: false, delta * 1000);
        QueueRedraw();
    }

    /// <summary>Each peer opens a door 30 ticks into its session, host and guest each their own; both say where they are.</summary>
    private void Autopilot(Client client, int before)
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

    public override void _Draw()
    {
        if (_session == null) return;
        // In the world's pixels, where the hero walks; space is left to the clear colour.
        for (int y = 0; y < _m.Grid.Height; y++)
            for (int x = 0; x < _m.Grid.Width; x++)
            {
                int cell = y * _m.Grid.Width + x;
                var room = _m.Rooms.RoomAt(cell);
                var at = new Rect2(x * CellPx, y * CellPx, CellPx, CellPx);
                int structure = _m.Grid.StructureAt(cell);
                if (room is int id and >= 0) DrawRect(at, Color.FromHsv(id * 0.137f % 1f, 0.3f, 0.35f));
                if (structure == Structure.Ladder) DrawTextureRect(_ladder, at, false);
                else if (structure == Structure.Floor) DrawTextureRect(_floor, new Rect2(at.Position, CellPx, 4), false);
                else if (structure == Structure.Wall) DrawRect(at.Grow(-0.5f), new Color(0.45f, 0.47f, 0.52f));
                else if (structure == Structure.DoorClosed) DrawRect(at.Grow(-0.5f), new Color(0.8f, 0.48f, 0.23f));
                else if (structure == Structure.DoorOpen) DrawRect(at.Grow(-0.5f), new Color(0.8f, 0.48f, 0.23f, 0.25f));
            }
    }
}
