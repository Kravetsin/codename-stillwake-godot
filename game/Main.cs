using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Dev;
using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Debris;
using Stillwake.Game.Features.Dev;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Features.Furnace;
using Stillwake.Game.Features.Grid;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Research;
using Stillwake.Game.Menu;
using Stillwake.Game.Net;
using Stillwake.Game.Platform;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game;

/// <summary>
/// The game: a session (net/README.md) of its own, or with <c>-- --host</c> one others join over ENet
/// with <c>-- --join[=address]</c>, or through the playtest server's relay with <c>--relay</c>, hosting as
/// <c>--host=name</c> and joining by <c>--join=name</c> (<see cref="Relay"/>); the world it plays drawn by the client's features after every
/// tick, and the hero: A/D walk, W/S climb, Space jumps, E works doors and valves, loads tanks, opens
/// crates and catches debris, the left button held works plans; the camera follows it; its items (Tab,
/// 1–9, the wheel, Q) and the lifepod's Fabricator beside them (E at the pod), the plan (B), the game menu (Esc: settings, save,
/// load, quit) and the dev panel (`).
/// Dev arguments after <c>--</c>: <c>--perf</c> the reference world, <c>--gallery</c> every picture,
/// <c>--view X,Y,ZOOM</c> where the camera looks, <c>--doors</c> toggles doors for recordings,
/// <c>--autopilot</c> plays the hero by a script, <c>--gym</c> a room with a ladder for it,
/// <c>--furnace</c> a lit furnace in the wreck, <c>--items</c> a room with a crate, a furnace and an
/// analyzer and a script working them, <c>--debris</c> the wreck with debris about, a crate caught with <c>--autopilot</c>,
/// <c>--pod</c> the wreck's lifepod opened and crafting (<see cref="PodDemo"/>), <c>--plan-demo</c> plays the plan, cables and overlays by a script (<see cref="PlanDemo"/>), <c>--menu-demo</c>
/// the items room's inventory and the game menu (<see cref="MenuDemo"/>),
/// <c>--measure</c> prints what frames cost and quits, <c>--frames</c> (or <c>--frames=vsync</c>) logs every frame
/// and prints the worst with their causes on quitting (<see cref="FrameLog"/>), <c>--for=seconds</c> quits after a while,
/// <c>--auto</c> opens a door per peer, <c>--cheat</c> makes a joiner's world differ so the host must
/// resync it, <c>--lag</c> and <c>--jitter</c> make a joiner's link bad on purpose.
/// </summary>
public partial class Main : Node2D, ITogether
{
    private const int Port = 24600;
    private static readonly string Build =
        typeof(Main).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    private Session? _session;
    /// <summary>Playing together as the menu shows it; the link it runs over, and the guests by their peer on it.</summary>
    private TogetherState _together = TogetherState.Alone;
    private Action? _stopLink;
    private readonly Dictionary<long, int> _guests = new();
    private readonly Updater _updater = new(Updates.Of(Build));
    private Registry _m = null!;
    /// <summary>The registry the views draw: a load or a resync brings a new one.</summary>
    private Registry? _shown;
    private GridView? _grid;
    private LightView? _light;
    private FurnaceView? _furnace;
    private AnalyzerScreens? _screens;
    private ShipCamera _camera = null!;
    private string[] _args = [];
    private string _role = "alone";
    private bool _cheat;
    private Action _poll = () => { };
    private readonly List<int> _doors = [];
    private int _heroes, _start;
    private double _worstSyncMs;
    private string? _gridKey;
    private Layout _layout = Registry.Wreck;
    private HeroView? _hero;
    private ItemsView? _items;
    private FabricatorPanel? _fabricator;
    private PodView? _pods;
    private DebrisView _debris = null!;
    private Hands _hands = null!;
    private PlanDemo? _demo;
    private FrameLog? _frameLog;
    /// <summary>Where this machine stands on the network, for the dev panel; none alone.</summary>
    private string? _net;

    public override void _Ready()
    {
        _args = OS.GetCmdlineUserArgs();
        _cheat = Has("--cheat");
        var seconds = _args.FirstOrDefault(a => a.StartsWith("--for="));
        if (seconds != null) GetTree().CreateTimer(int.Parse(seconds[6..])).Timeout += Quit;

        // The views of this player's hands, made first: the hero, its items and the debris share its Ui and mouse.
        _hands = new Hands();
        ItemIcons.MakeSilhouettes();
        _debris = new DebrisView(_hands.Ui);
        AddChild(new Stars());
        AddChild(_debris);
        if (Has("--debris")) AddChild(new DebrisDemo(_debris, _hands.Ui));
        AddChild(_camera = new ShipCamera());
        if (Has("--gym")) _layout = HeroAutopilot.Gym;
        if (Has("--items") || Has("--menu-demo")) _layout = ItemsDemo.Map;
        // The dev worlds are looked at, not walked: the camera moves by hand there.
        if (!Has("--perf") && !Has("--gallery"))
        {
            var script = !Has("--autopilot") ? null : Has("--gym") ? HeroAutopilot.GymRun() : HeroAutopilot.Wreck();
            AddChild(_hero = new HeroView(_hands.Ui, _hands.Mouse, script));
            AddChild(_items = new ItemsView(_hands.Ui, demo: Has("--items")));
            AddChild(_fabricator = new FabricatorPanel(_hands.Ui, peer => AsciiArt.Colour((uint)Suits.Of(_m.Crew, peer))));
            // Its own layer, above the world: read from whatever registry the session runs now.
            AddChild(new SuitGauge(() => _session is Session s ? Danger.Read(_m.Crew, _m.Atmosphere, s.Client.Id) : (Content.Crew.Suit, SuitDanger.None)));
            _camera.DevControls = false;
        }
        AddChild(new EffectsView());
        AddChild(_hands);
        if (Has("--plan-demo"))
        {
            _demo = new PlanDemo(_hands, _layout);
            _demo.Start();
        }
        var join = _args.FirstOrDefault(a => a == "--join" || a.StartsWith("--join="));
        var hostAs = _args.FirstOrDefault(a => a == "--host" || a.StartsWith("--host="));
        if (hostAs != null) Host(hostAs.Length > 7 ? hostAs[7..] : null);
        else if (join != null) Join(join.Length > 7 ? join[7..] : "127.0.0.1", BadNetwork.From(_args), Relay());
        else Alone();
        AddChild(new DevPanel(() => _session, () => _m, () => _net));
        var suits = new SuitColours(SuitsWorn, colour => _session?.Client.Send(new Sim.Crew.PickColour(colour)));
        var menu = new GameMenu(_hands.Ui,
            busy: () => (_items?.Panel.Shown ?? false) || _hands.Plan.PlanOut.Get(),
            putAway: () =>
            {
                _items?.Panel.Close();
                _hands.Plan.PutPlanAway();
            },
            suits, SaveWorld, LoadWorld, canLoad: () => _role == "alone", Quit,
            together: this,
            version: Updates.Of(Build).ToString(), updateTo: () => _updater.Available?.Tag, update: _updater.Update,
            updating: () => _updater.Status);
        AddChild(menu);
        AddChild(_updater);
        // Playing together, how long an action takes to happen: sent, stamped by the host, run here.
        AddChild(new VersionCorner(Updates.Of(Build).ToString(), () => _role == "alone" ? null : _session?.RoundTrip.Median));
        if (Has("--menu-demo") && _items != null) AddChild(new MenuDemo(_items, menu, suits));
        if (Has("--pod")) AddChild(new PodDemo(_hands.Ui, () => _session is Session s ? (s.Client, _m) : null));
        if (Has("--frames") || Has("--frames=vsync")) AddChild(_frameLog = new FrameLog(vsync: Has("--frames=vsync")));
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
        var world = Has("--gallery") ? DevWorlds.Gallery(modules)
            : Has("--furnace") ? FurnaceDemo.Create(modules)
            : World.Create(1, layout, modules);
        if (Has("--items") || Has("--menu-demo")) world = ItemsDemo.Seed(world, Modules);
        if (Has("--debris")) world = DebrisDemo.Seed(world, Modules);
        if (Has("--pod")) world = PodDemo.Seed(world, Modules);
        var (host, client) = LocalSession.Create(world, Build);
        _session = new Session(host, client);
        ShowWorld();
        AimCamera(Has("--gallery") ? null : layout);
    }

    /// <summary>
    /// The relay to meet through rather than ENet, with <c>--relay</c> (the playtest server) or
    /// <c>--relay=wss://...</c>, and its password, <c>--password=...</c> or <c>STILLWAKE_PASSWORD</c>; none without.
    /// </summary>
    private (string Url, string Password)? Relay()
    {
        var relay = _args.FirstOrDefault(a => a == "--relay" || a.StartsWith("--relay="));
        if (relay == null) return null;
        string url = relay.Length > 8 ? relay[8..] : RelayFrames.PlaytestUrl;
        string password = _args.FirstOrDefault(a => a.StartsWith("--password="))?[11..]
            ?? System.Environment.GetEnvironmentVariable("STILLWAKE_PASSWORD") ?? "";
        return (url, password);
    }

    /// <summary>Hosts a session from the start: over ENet on <see cref="Port"/>, or on the relay as <paramref name="name"/>.</summary>
    private void Host(string? name)
    {
        var world = World.Create(1, _layout, Modules());
        // The debris demo is played hosting too, so a guest sees a catch.
        if (Has("--debris")) world = DebrisDemo.Seed(world, Modules);
        var (host, client) = LocalSession.Create(world, Build);
        _session = new Session(host, client);
        ShowWorld();
        AimCamera(_layout);
        Serve(name, Relay());
    }

    /// <summary>
    /// Opens this machine's own world to guests: over ENet on <see cref="Port"/>, or on
    /// <paramref name="relay"/> as <paramref name="name"/>. Its session's host welcomes them with its world.
    /// </summary>
    private void Serve(string? name, (string Url, string Password)? relay)
    {
        if (_session is not { Host: { } host } session) return;
        var client = session.Client;
        _role = "host";
        _guests.Clear();
        ILink<ToHost, ToClient> link;
        string where = $":{Port}";
        if (relay is var (url, password))
        {
            var relayed = RelayLink<ToHost, ToClient>.Host(url, password, name);
            where = "the relay";
            _together = new TogetherState(TogetherMode.Opening, name?.ToUpperInvariant(), [], null, false);
            relayed.Hosted += hosted =>
            {
                where = $"\"{hosted}\" on the relay";
                _net = $"hosting {where} · peers 0";
                _together = _together with { Mode = TogetherMode.Hosting, Code = hosted.ToUpperInvariant() };
                Log($"hosting session {hosted} on {url}");
            };
            relayed.Failed += why =>
            {
                Log(why);
                _net = why;
                // Nobody can join a session the relay never opened: the world plays on alone.
                bool opened = _together.Mode == TogetherMode.Hosting;
                _together = TogetherState.Alone with { Problem = why, PasswordRefused = !opened };
                _role = "alone";
                _poll = () => { };
            };
            link = relayed;
        }
        else
        {
            var (enet, error) = EnetLink<ToHost, ToClient>.Host(Port);
            if (error != Error.Ok) GD.PrintErr($"[host] cannot listen on {Port}: {error}");
            link = enet;
        }
        var ids = _guests;
        _net = $"hosting {where} · peers 0";
        link.Connected += (peer, channel) =>
        {
            ids[peer] = host.Admit(channel);
            _net = $"hosting {where} · peers {ids.Count}";
            Log($"peer {ids[peer]} joined at tick {host.Tick}");
        };
        link.Disconnected += peer =>
        {
            // Its hero leaves with it, its suit colour freed.
            if (ids.Remove(peer, out int id)) host.Disconnect(id, client.World.Encode(new Sim.Crew.Leave()));
            Log($"peer {id} left");
            _net = $"hosting {where} · peers {ids.Count}";
        };
        _poll = link.Poll;
        _stopLink = () =>
        {
            link.Close();
            // Its guests' heroes leave the world as they would leaving themselves.
            foreach (int id in ids.Values) host.Disconnect(id, client.World.Encode(new Sim.Crew.Leave()));
            ids.Clear();
        };
    }

    /// <summary>
    /// Joins a session: a host's address over ENet, or a session's name on <paramref name="relay"/>. A world
    /// of this machine's own plays on until the host's arrives, and on still if joining fails.
    /// </summary>
    private void Join(string address, BadNetwork? network, (string Url, string Password)? relay)
    {
        _net = $"joining {address}";
        ILink<ToClient, ToHost> link;
        if (relay is var (url, password))
        {
            var relayed = RelayLink<ToClient, ToHost>.Join(url, password, address);
            _together = new TogetherState(TogetherMode.Joining, address.ToUpperInvariant(), [], null, false);
            relayed.Failed += why =>
            {
                Log(why);
                _net = why;
                bool refused = why.StartsWith("no session");
                _together = TogetherState.Alone with
                {
                    Problem = refused ? T("menu.noGame", ("code", address.ToUpperInvariant())) : why,
                    PasswordRefused = !refused && _together.Mode == TogetherMode.Joining,
                };
                _poll = () => { };
            };
            link = relayed;
        }
        else
        {
            var (enet, error) = EnetLink<ToClient, ToHost>.Join(address, Port);
            if (error != Error.Ok) GD.PrintErr($"[guest] cannot reach {address}: {error}");
            link = enet;
        }
        link.Connected += (_, channel) =>
        {
            Log($"connected to {address}{(network != null ? $" over {network}" : "")}");
            _net = $"joined {address}";
            Client.Join(Load, network?.Over(channel, new GodotTiming(GetTree())) ?? channel, Build,
                client =>
                {
                    _role = "guest";
                    _together = _together with { Mode = TogetherMode.Joined };
                    _session = new Session(null, client);
                    _start = client.World.Tick;
                    ShowWorld();
                    AimCamera(Registry.Wreck);
                    Log($"joined as peer {client.Id} at tick {client.World.Tick}");
                },
                why =>
                {
                    Log(why.Message);
                    _net = why.Message.Contains("runs build") ? T("menu.otherVersion") : why.Message;
                    _together = TogetherState.Alone with { Problem = _net };
                    if (_session == null) GetTree().Quit(1);
                });
        };
        link.Disconnected += _ =>
        {
            Log("the host left");
            Quit();
        };
        _poll = link.Poll;
    }

    public TogetherState Together =>
        _together.Mode is TogetherMode.Hosting or TogetherMode.Joined ? _together with { Suits = SuitsAboard() } : _together;

    /// <summary>The suit colours aboard, this player's first, as the Play together page shows them.</summary>
    private IReadOnlyList<int> SuitsAboard()
    {
        if (_session is not Session s) return [];
        return _m.Crew.Heroes.OrderBy(h => h.Id == s.Client.Id ? 0 : 1).ThenBy(h => h.Id)
            .Select(h => h.Colour >= 0 && h.Colour < Suits.All.Length ? Suits.All[h.Colour] : Suits.All[0]).ToList();
    }

    public void HostTogether(string password)
    {
        if (_role != "alone" || _together.Mode != TogetherMode.Alone) return;
        Serve(Codes.New(Random.Shared).ToLowerInvariant(), (Relay()?.Url ?? RelayFrames.PlaytestUrl, password));
    }

    public void JoinTogether(string code, string password)
    {
        if (_role != "alone" || _together.Mode != TogetherMode.Alone) return;
        Join(code.ToLowerInvariant(), null, (Relay()?.Url ?? RelayFrames.PlaytestUrl, password));
    }

    public void StopTogether()
    {
        if (_role != "host") return;
        _stopLink?.Invoke();
        _stopLink = null;
        _poll = () => { };
        _role = "alone";
        _net = null;
        _together = TogetherState.Alone;
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

    /// <summary>Save files in Godot's user folder: the menu's world and the last desync report.</summary>
    private readonly Saves _saves = new(ProjectSettings.GlobalizePath("user://saves"));
    private static readonly System.Text.Json.JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>The menu's save: the world as this machine has it, to the world slot; how it went as an i18n key.</summary>
    private string SaveWorld()
    {
        if (_session is not Session s) return "menu.nothingToSave";
        _saves.Write(Saves.World, s.Client.World.Save());
        return "menu.saved";
    }

    /// <summary>The menu's load: the world slot's world, as a session of this machine's own at the same speed.</summary>
    private string LoadWorld()
    {
        if (_role != "alone") return "menu.aloneOnly";
        if (_saves.Read(Saves.World) is not string save) return "menu.nothingSaved";
        // The registry is swapped only once the save has loaded: a broken one leaves the world as it was.
        var m = new Registry();
        var world = World.Load(save, m.All);
        _m = m;
        var (host, client) = LocalSession.Create(world, Build);
        _session = new Session(host, client) { Speed = _session?.Speed ?? 1 };
        ShowWorld();
        return "menu.loaded";
    }

    /// <summary>The views drawing the current registry's world, made again when a load brings a new one.</summary>
    private void ShowWorld()
    {
        if (_shown == _m) return;
        _grid?.QueueFree();
        _light?.QueueFree();
        _furnace?.QueueFree();
        _screens?.QueueFree();
        _pods?.QueueFree();
        AddChild(_grid = new GridView(_m.Grid));
        AddChild(_screens = new AnalyzerScreens(_m.Grid, _m.Research));
        AddChild(_pods = new PodView(_m.Grid, _m.Crafting, _m.Items, _m.Crew));
        var power = _m.Power;
        // The furnaces before the light: each frame a fire picks its picture, then its light follows it.
        var fires = new FurnaceView(_m.Grid, _m.Furnace);
        AddChild(_furnace = fires);
        AddChild(_light = new LightView(_m.Grid)
        {
            LampShare = lamp => power.Satisfaction(lamp.Id) / 1000f,
            Fire = m => fires.BlazeOf(m.Id),
        });
        MoveChild(_camera, -1);
        _shown = _m;
        _gridKey = null;
        if (_session is Session s)
        {
            SyncGrid(s.Client);
            // The first hash builds the serializer for every slice, some 10 ms: done while the world loads, not a second into play.
            s.Client.World.Hash();
        }
        SyncLight();
    }

    private void AimCamera(Layout? layout)
    {
        // Where the crew makes heroes: the pod's hatch, or the map's S; a map with neither is looked at whole.
        bool made = layout != null && (layout.CellsOf('S').Any() || layout.Machines().Any(m => m.Kind == "lifepod"));
        int start = made ? _m.Crew.Spawn : -1;
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
        _frameLog?.Start();
        _poll();
        _frameLog?.Mark("net");
        if (_session is not Session session) return;
        var client = session.Client;
        int before = client.World.Tick;
        int desyncs = session.Desyncs.Count;
        foreach (var report in session.Frame(delta * 1000))
            foreach (var refused in report.Refused)
                Log($"tick {report.Tick}: {refused.Command.GetType().Name} from {refused.Command.From} refused, {refused.Reason}");
        foreach (var report in session.Desyncs.Skip(desyncs))
        {
            Log($"desync, resynced from the host: {report}");
            // A determinism bug: the host has already mended the world, so the report is all that is left.
            _saves.Write(Saves.Desync, System.Text.Json.JsonSerializer.Serialize(report, Indented));
        }
        if (_role != "alone" && client.World.Tick / 100 != before / 100) Log($"tick {client.World.Tick} hash {client.World.Hash():x16}");
        var heard = client.TakeHeroes();
        _heroes += heard.Count;
        if (Has("--auto")) Auto(client, before);
        if (Has("--doors") && client.World.Tick != before)
            foreach (var c in Autopilot.At(client.World.Tick, _m.Grid)) client.Send(c);
        ShowWorld();
        _frameLog?.Mark("sim");
        if (client.World.Tick != before) SyncLight();
        _frameLog?.Mark("light");
        _hero?.Frame(client, _m, _camera, heard, delta * 1000);
        _frameLog?.Mark("hero");
        // After the hero, so a door it toggles shows on the frame E is pressed (0014).
        SyncGrid(client);
        _frameLog?.Mark("grid");
        if (_items != null) _items.Panel.Furnace = _m.Furnace.At;
        if (_items != null) _items.Frame(client, _m.Grid, _m.Rooms, _m.Items, _m.Research, SpotOf(_hero), session.Progress, delta * 1000);
        _fabricator?.Frame(client, _m.Crafting, _m.Research, _m.Items, _m.Grid, delta * 1000);
        _frameLog?.Mark("items");
        _debris.Frame(client, _m.Debris, session.Progress, delta * 1000);
        _frameLog?.Mark("debris");
        ShowHands(client, delta * 1000);
        _frameLog?.Mark("hands");
    }

    /// <summary>Where the hero stands for its items: what a drop is thrown from, what it picks up.</summary>
    private HeroSpot? SpotOf(HeroView? view)
    {
        if (view == null) return null;
        var hero = view.Current;
        return new HeroSpot(hero.X, hero.Y, hero.Facing, hero.BodyCell(_m.Grid.Width));
    }

    /// <summary>The plan, cables and overlays (<see cref="Hands"/>): the hero's body sways the cables.</summary>
    private void ShowHands(Client client, double elapsedMs)
    {
        _bodies.Clear();
        if (_hero != null)
        {
            // Every hero sways the cables, this one and the others where they are drawn.
            foreach (var other in _hero.Others) _bodies.Add(BodyOf(other));
            _bodies.Add(BodyOf(_hero.Current));
        }
        _demo?.Frame(client, _m.Grid, Log);
        _hands.Frame(client, _m, _bodies, elapsedMs);
    }

    private readonly List<Body> _bodies = [];

    private static Body BodyOf(Hero h)
    {
        const double unit = Hero.Unit;
        return new Body((h.X - Content.Hero.Width / 2.0) / unit, (h.X + Content.Hero.Width / 2.0) / unit,
            (h.Y - Content.Hero.Height) / unit, h.Y / unit, h.Vx / unit, h.Vy / unit);
    }

    /// <summary>The grid as this peer shows it, redrawn when that changes: a tick, or a door or valve toggle of its own sent (0014).</summary>
    private void SyncGrid(Client client)
    {
        var shown = Predicted.Grid(client, _m.Grid);
        if (shown.Key == _gridKey) return;
        _gridKey = shown.Key;
        Measure(() => _grid?.Sync(shown));
    }

    /// <summary>Shadows, starlight and lamps after a tick: the light follows the world, not this peer's toggles in flight, as in TypeScript.</summary>
    private void SyncLight() => Measure(() => _light?.Sync());

    private void Measure(Action sync)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        sync();
        _worstSyncMs = Math.Max(_worstSyncMs, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    /// <summary>The suit colour this player wears, and those other heroes wear (SuitColours.tsx's read).</summary>
    private (int? Mine, IReadOnlySet<int> Taken) SuitsWorn()
    {
        if (_session is not Session s) return (null, new HashSet<int>());
        var heroes = _m.Crew.Heroes;
        return (heroes.FirstOrDefault(h => h.Id == s.Client.Id)?.Colour,
            heroes.Where(h => h.Id != s.Client.Id).Select(h => h.Colour).ToHashSet());
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
        _frameLog?.Report();
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
