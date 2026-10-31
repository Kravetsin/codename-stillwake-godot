using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Stillwake.Game.Dev;
using Stillwake.Game.Features.Computer;
using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Debris;
using Stillwake.Game.Features.Dev;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Features.Furnace;
using Stillwake.Game.Features.Grid;
using Stillwake.Game.Features.Handbook;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Research;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Menu;
using Stillwake.Game.Platform;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game;

/// <summary>
/// The game: started with no arguments, the title (0031: Continue, Worlds, Play together, Quit; Testbed in a debug build), each
/// world a folder of <see cref="WorldIndex"/>, saved every five minutes and on every way out; with any, a
/// session (net/README.md) of its own straight away, or with <c>-- --host</c> one others join over ENet
/// with <c>-- --join[=address]</c>, or through the playtest server's relay with <c>--relay</c>, hosting as
/// <c>--host=name</c> and joining by <c>--join=name</c> (<see cref="Relay"/>); the world it plays drawn by the client's features after every
/// tick, and the hero: A/D walk, W/S climb, Space jumps, E works doors and valves, loads tanks, opens
/// crates and catches debris, the left button held works plans; the camera follows it; its items (Tab,
/// 1–9, the wheel, Q) and the lifepod's Fabricator beside them (E at the pod), the plan (B), the Handbook (J), the game menu (Esc: settings, save,
/// load, quit) and the dev panel (`).
/// Here startup and the frame loop; sessions and saves in <c>Main.Sessions.cs</c>; every argument after <c>--</c> in
/// <see cref="Dev.DevModes"/>, wired in <c>dev/Main.Dev.cs</c>.
/// </summary>
public partial class Main : Node2D, ITogether
{
    private const int Port = 24600;
    private static readonly string Build =
        typeof(Main).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    /// <summary>What a joiner must match: the build, and how it packs commands (2026-10-26-compact-wire).</summary>
    private static readonly string SessionBuild = $"{Build}+{Stillwake.Net.Wire.Fingerprint(World.CommandTypesOf(new Registry().All))}";

    private Session? _session;
    /// <summary>Playing together as the menu shows it; the link it runs over, and the guests by their peer on it.</summary>
    private TogetherState _together = TogetherState.Alone;
    private Action? _stopLink;
    private readonly Dictionary<long, int> _guests = new();
    private readonly Updater _updater = new(Updates.Of(Build));
    /// <summary>The registry of the surface this player's hero is on (0076): what the views draw and the hands work.</summary>
    private Registry _m = null!;

    /// <summary>The seed of the surface drawn, 0 aboard.</summary>
    private uint _seed;
    /// <summary>The ship's registry, surface 0's, made with the world: a load or a resync brings a new one.</summary>
    private Registry _ship = null!;
    /// <summary>The registry the views draw: a load or a resync brings a new one.</summary>
    private Registry? _shown;
    /// <summary>The views of the surface drawn (<see cref="SurfaceViews"/>), in draw order, and those of them that fade with the plan.</summary>
    private readonly List<Node> _views = [];
    private readonly List<CanvasItem> _faded = [];
    private GridView? _grid;
    private LightView? _light;
    private ShipCamera _camera = null!;
    /// <summary>The world's stages of effects, each drawn at its layer, kept last among the world's views (effects-on-their-layer).</summary>
    private readonly StagesView _stages = new();
    private Stars _stars = null!;
    private string _role = "alone";
    private bool _cheat;
    private Action _poll = () => { };
    private readonly List<int> _doors = [];
    private Command? _toggled;
    private int _heroes, _start;
    private double _worstSyncMs;
    private string? _gridKey;
    /// <summary>The map a dev run plays: the wreck as drawn, small and with no zone, its cells those the demos and scripts name.</summary>
    private Layout _layout = Registry.WreckMap;
    private HeroView? _hero;
    /// <summary>The look this player chose (2026-10-28-looks-are-printed): null on a first run till the look editor closes; look 0 in a dev run.</summary>
    private Sim.Players.Look? _look;
    private Features.Crew.LookEditor? _lookEditor;
    private ItemsView? _items;
    private FabricatorPanel? _fabricator;
    private DebrisView _debris = null!;
    private Features.Creatures.CreaturesView _creatures = null!;
    private Hands _hands = null!;
    private FrameLog? _frameLog;
    private AllocationSampler? _allocs;
    private readonly Platform.GcPace _gcPace = new();
    /// <summary>Where this machine stands on the network, for the dev panel; none alone.</summary>
    private string? _net;
    /// <summary>A world is saved this often, in ticks: five minutes of it.</summary>
    private const int AutosaveTicks = 5 * 60 * World.TicksPerSecond;
    /// <summary>The player's worlds on disk.</summary>
    private readonly WorldIndex _index = new(ProjectSettings.GlobalizePath("user://saves/worlds"), Migrations.Oldest, World.SaveFormat, World.TicksPerSecond);
    /// <summary>The world played and kept by this machine; none for a dev world or a friend's.</summary>
    private string? _worldId;
    private int _lastSaveTick;

    /// <summary>The drop pods coming down on the surface this player sees (the descent's pictures), one for the session, kept across switches.</summary>
    private readonly Features.Landing.Arrivals _arrivals = new();
    private bool _flewLast;
    private World? _arrivalsWorld;

    /// <summary>
    /// Tells the arrivals this frame's pods: a world loaded or resynced (a new <c>World</c>) shows nothing coming down; this
    /// player's own pod comes down the frame its flight is gone and it sits in one.
    /// </summary>
    private void SeeArrivals(Client client, double delta)
    {
        int surface = client.World.SurfaceOf(client.Id);
        bool newWorld = !ReferenceEquals(client.World, _arrivalsWorld);
        _arrivalsWorld = client.World;
        bool flies = _m.Landing.FlightOf(client.Id) != null;
        int? landed = _flewLast && !flies ? _m.Landing.SeatOf(client.Id) : null;
        _flewLast = flies;
        var pods = _m.Grid.MachinesWith(Content.MachineTrait.Seats).Select(p => (p.Id, _m.Landing.RiderOf(p.Id) != null)).ToList();
        _arrivals.Frame(surface, surface == 0, pods, landed, newWorld, delta);
    }

    /// <summary>
    /// The rider's view shaken as its pod drops through the deck (the descent's pictures): a knock every third tick of the fall,
    /// a random way each, through the camera's own nudge.
    /// </summary>
    private void ShakeFalling(Client client)
    {
        if (_m.Landing.FlightOf(client.Id) is not { Up: false } own || client.World.Tick % 3 != 0) return;
        double s = Features.Landing.FlightTimeline.Seconds(own, 0);
        if (s >= Features.Landing.FlightTimeline.DropFrom && s < Features.Landing.FlightTimeline.DropTo)
            _camera.Nudge(new Vector2(Random.Shared.Next(2) * 2 - 1, Random.Shared.Next(2) * 2 - 1));
    }
    private DateTime? _savedAt;
    private string? _saveProblem;
    private TitleScreen? _title;
    /// <summary>Who plays on this machine (0032), and who is who in the world it keeps.</summary>
    private string _playerKey = "";
    private Roster _roster = Roster.Empty();
    /// <summary>What the title says once the scene is back to it: why a world was left. Kept across the reload.</summary>
    private static string? _next;

    /// <summary>Save files in Godot's user folder: the menu's world and the last desync report.</summary>
    private readonly Saves _saves = new(ProjectSettings.GlobalizePath("user://saves"));
    private static readonly System.Text.Json.JsonSerializerOptions Indented = new() { WriteIndented = true };

    public override void _Ready()
    {
        // A friend's log arrives without context: its first line says which build and system wrote it.
        GD.Print($"Stillwake {Build} on {OS.GetName()} {OS.GetVersion()}");
        _dev = new Dev.DevArgs(OS.GetCmdlineUserArgs(), DevScenarios.Modes);
        _scenarios = DevScenarios.Active(_dev);
        _playerKey = Profile.Key(_dev.All);
        _cheat = Has("--cheat");
        _look = Settings.Look() ?? (_dev.None ? null : Sim.Players.Look.Zero);
        if (PoseSheetOnly() || DressSheetOnly()) return;
        QuitAfterAWhile();
        // First of all at its layer, before every shimmer (0075).
        AddChild(Shimmer.NewScreenCopy());
        // Before everything else, so every button made after it clicks.
        var sound = new SoundView(Floats, () => _title != null, Fogged);
        AddChild(sound);

        // The views of this player's hands, made first: the hero, its items and the debris share its Ui and mouse.
        _hands = new Hands();
        // What the hands do in the inventory clicks as the buttons do (0081).
        _hands.Ui.Clicked += sound.Click;
        // The slots' size kept on this machine (0058); --slots=large shows the other for a recording, keeping nothing then.
        var slots = _dev.Value("--slots");
        _hands.Ui.SlotScale.Set(slots != null ? Slots.ScaleNamed(slots) : Settings.SlotScale());
        if (slots == null) _hands.Ui.SlotScale.Subscribe(() => Settings.Keep(_hands.Ui.SlotScale.Get()));
        Shimmer.Enabled = Settings.Shimmer();
        ItemIcons.MakeSilhouettes();
        _debris = new DebrisView(_hands.Ui);
        AddChild(_stars = new Stars());
        AddChild(_debris);
        AddChild(_camera = new ShipCamera());
        _debris.Nudged += _camera.Nudge;
        AddChild(_creatures = new Features.Creatures.CreaturesView(_hands.Ui));
        _layout = DevLayout(_layout);
        // The dev worlds are looked at, not walked: the camera moves by hand there.
        if (!Has("--perf") && !Has("--gallery"))
        {
            var script = DevScript();
            AddChild(_hero = new HeroView(_hands.Ui, _hands.Mouse, script) { Look = () => _look });
            PlaceHero(_hero);
            AddChild(_items = new ItemsView(_hands.Ui));
            // A queued station's inputs are the items feature's slots, laid out in the crafting feature's panel.
            AddChild(_fabricator = new FabricatorPanel(_hands.Ui, peer => AsciiArt.Colour((uint)Suits.Of(_m.Players, peer))) { Lend = _items.Panel.Lend, LendSockets = _items.Panel.LendSockets, ShiftOut = _items.Panel.ShiftOut });
            // The drone badge is the logistics feature's, put in the items' and crafting panels' headers where drones have work (0062).
            Features.Logistics.DroneWorld? Drones() => _session is Session s ? new(_m.Logistics, s.Client.InFlight) : null;
            void SendNow(Command c) => (_session as Session)?.Client.Send(c);
            Control? BadgeOf(int machine) => _m.Logistics.Servable(machine) ? new Features.Logistics.ServedBadge(machine, Drones, SendNow, _hands.Ui.Tip) : null;
            _items.Panel.Served = BadgeOf;
            _items.Panel.Kept = p => p.Crate is int id && _m.Logistics.Served(id) ? _m.Items.KeptAt(id, p.Slot) : null;
            _items.Panel.Supplied = (id, item) => _m.Logistics.Supplied(id, item);
            _fabricator.Served = BadgeOf;
            // A drop pod's Descend or Return key is the landing feature's, under its slot; the white of its flight over the screen.
            Control? LaunchOf(int machine) => _m.Grid.MachineById(machine) is { } pod && Content.Machines.Is(pod.Kind, Content.MachineTrait.Seats)
                ? new Features.Landing.LaunchKey(() => _session is Session s ? (_m.Landing, s.Client.Id) : null, SendNow, _hands.Ui.Tip)
                : null;
            _items.Panel.Launch = LaunchOf;
            AddChild(new Features.Landing.FlightOverlay(() => _session is Session s ? (s.Client.World.SurfaceOf(s.Client.Id), _m.Landing.FlightOf(s.Client.Id)) : null,
                () => (_session as Session)?.Progress ?? 0, () => _camera.CurrentZoom));
            // The belt's energy beside the suit's, whatever is held, so the gauge row never changes width (0081). Before the
            // suit's gauge, so the red edges and the dark of passing out cover it as they cover the rest of the HUD.
            var energy = new EnergyBar(() => _session is Session s ? Charges.BeltOf(_m.Items.BeltCharge(s.Client.Id), Equipment.Of(Content.Charge.Belt).Count(_m.Items.SlotOpen)) : null, _hands.Ui);
            AddChild(energy);
            // Its own layer, above the world: read from whatever registry the session runs now.
            var gauge = new SuitGauge(() => _session is Session s ? Danger.Read(_m.Crew, _m.Atmosphere, s.Client.Id) : (1, 1, SuitDanger.None, true), _hands.Ui);
            AddChild(gauge);
            if (_hero is { } breathing) gauge.BreathesAgain += breathing.BreathesAgain;
            AddChild(new Frost(() => _session is Session s && _hero is { Placed: true } h ? Report.Cold(_m.Crew, s.Client.Id, h.Current.BodyCell(_m.Grid.Width)) : 0));
            // Home and friends off the screen, pointed at from its edge, clear of the HUD's panels.
            var (panel, fabricator, building, overlays) = (_items.Panel, _fabricator, _hands.Menu, _hands.Overlays);
            IEnumerable<Rect2> Hud()
            {
                foreach (var covered in panel.Covers()) yield return covered;
                yield return gauge.Covers;
                yield return energy.Covers;
                yield return building.Covers;
                yield return overlays.Covers;
                if (fabricator.Covers is { } crafts) yield return crafts;
            }
            AddChild(new MarkerLayer(_hands.Ui, _camera, () => _session is Session s && _hero is { Placed: true } h ? (h, _m, s.Client.Id) : null, Hud));
            _camera.DevControls = false;
        }
        AddChild(_stages);
        AddChild(_hands);
        AddChild(_shot);
        // The world pictured for its card as the menu opens, the way out of it.
        _hands.Ui.MenuOpen.Subscribe(() =>
        {
            if (_hands.Ui.MenuOpen.Get()) TakeShot();
        });
        var join = _dev.Given("--join");
        var hostAs = _dev.Given("--host");
        // Every way out saves first: the window's close as well.
        GetTree().AutoAcceptQuit = false;
        _updater.BeforeRestart = SaveIfOwned;
        if (_dev.None) ShowTitle();
        else if (hostAs != null) Host(hostAs.Length > 0 ? hostAs : null);
        else if (join != null) Join(join.Length > 0 ? join : "127.0.0.1", BadNetwork.From(_dev.All), Relay());
        else Alone();
        var dev = new DevPanel(() => _session, () => _m, () => _net);
        AddChild(dev);
        // The Handbook (0066): each section a feature's own page; what opening it puts away is wired here, so no feature reaches into another.
        var debrisPage = new HandbookPage(_hands.Ui, () => _session != null ? _ship.Debris : null);
        // The tech tree on the tablet: put away and putting away as the computer's window.
        var tech = new TechPanel(_hands.Ui, () => _session != null && _hero != null, () => _session != null ? _m : null, () => (_session as Session)?.Client.Id ?? 0, command => _session?.Client.Send(command));
        var handbook = new HandbookPanel(_hands.Ui, () => _session != null && _hero != null, [new HandbookSection(T("handbook.debris"), debrisPage, debrisPage.Tally)])
        {
            Opening = () =>
            {
                _items?.Panel.Close();
                _hands.Ui.OpenComputer.Set(null);
                tech.Close();
                _hands.Plan.PutPlanAway();
            },
        };
        AddChild(handbook);
        // The ship's computer (its window on E): opening it puts away the inventory, the plan and the Handbook, as the Handbook does.
        var computer = new ComputerPanel(_hands.Ui, () => _session != null ? _m : null);
        _hands.Ui.OpenComputer.Subscribe(() =>
        {
            if (_hands.Ui.OpenComputer.Get() == null) return;
            _items?.Panel.Close();
            handbook.Close();
            tech.Close();
            _hands.Plan.PutPlanAway();
        });
        AddChild(computer);
        _hands.Ui.TreeOpen.Subscribe(() =>
        {
            if (!_hands.Ui.TreeOpen.Get()) return;
            _items?.Panel.Close();
            handbook.Close();
            _hands.Ui.OpenComputer.Set(null);
            _hands.Plan.PutPlanAway();
        });
        AddChild(tech);
        var suits = new SuitColours(SuitsWorn, colour => _session?.Client.Send(new Sim.Crew.PickColour(colour)));
        var lookEditor = _lookEditor = new Features.Crew.LookEditor(() => Suits.Of(_m.Players, (_session as Session)?.Client.Id ?? 0), look =>
        {
            _look = look;
            // A dev run's look is its own: the player's kept one stays.
            if (_dev.None) Settings.KeepLook(look);
            _session?.Client.Send(new Sim.Crew.ChooseLook(look));
        });
        AddChild(lookEditor);
        var menu = new GameMenu(_hands.Ui,
            busy: () => (_items?.Panel.Shown ?? false) || handbook.Shown || computer.Shown || tech.Shown || lookEditor.Shown || _hands.Plan.PlanOut.Get() || dev.Searching,
            putAway: () =>
            {
                // A search in the build menu or the dev panel goes first, what else is open staying so.
                if (dev.DropSearch() || _hands.Menu.DropSearch()) return;
                _items?.Panel.Close();
                handbook.Close();
                computer.Close();
                tech.Close();
                lookEditor.Close();
                _hands.Plan.PutPlanAway();
            },
            handbook: handbook.Open,
            tree: tech.Open,
            look: () => lookEditor.Open(_look ?? Sim.Players.Look.Zero),
            suits, SaveWorld, inWorld: () => _session != null, owns: () => _role != "guest", exitToTitle: () => ExitToTitle(null), Quit, Foot,
            version: Updates.Of(Build).ToString(), updateTo: () => _updater.Available?.Tag, update: _updater.Update,
            updating: () => _updater.Status);
        AddChild(menu);
        AddChild(_updater);
        // Playing together, how long an action takes to happen: sent, stamped by the host, run here.
        AddChild(new VersionCorner(Updates.Of(Build).ToString(), () => _role == "alone" ? null : _session?.RoundTrip.Median));
        AddDevDemos(menu, suits, debrisPage, _stages);
    }

    /// <summary>Whether this player's placed hero floats out in space: the world silent, its own touch dulled, the bed of space playing (0033, 0041).</summary>
    // In a drop pod the hero hears as in air, the vacuum outside or not: the pod holds its own.
    private bool Floats() => _session != null && _hero is { Placed: true, Floats: true, InPod: false };

    /// <summary>How deep in a planet's fog this player's placed hero is, 0 to 1 (fog-edges): the world dulls in it as in space.</summary>
    private double Fogged() => _session != null && _hero is { Placed: true } hero && Shared.FogEdges.Of(_m.Grid, _m.Rules) is { } fog
        ? Shared.FogShare.Column(fog, hero.Current.X / (double)Hero.Unit) : 0;

    private IReadOnlyList<Sim.Module> Modules()
    {
        _m = _ship = new Registry();
        return _m.All;
    }


    /// <summary>
    /// Draws the surface this player's hero is on (0076): once it is another, what was open or held of the last one is put
    /// away — an open crate, the plan, the effects flying — and <see cref="ShowWorld"/> makes the views again.
    /// </summary>
    private void Follow(Client client)
    {
        int surface = client.World.SurfaceOf(client.Id);
        var here = surface == 0 ? _ship : Registry.On(client.World, surface);
        _hands.Plan.Aboard.Set(surface == 0);
        if (here == _m) return;
        _m = here;
        // What its creatures' looks grow from, with their ids (0102): the same individuals on every peer.
        _seed = surface == 0 ? 0 : client.World.Surfaces.First(s => s.Id == surface).Seed;
        _items?.Panel.Close();
        _hands.Ui.OpenCrate.Set(null);
        _hands.Ui.OpenComputer.Set(null);
        _hands.Ui.AimedPiece.Set(null);
        _hands.Plan.PutPlanAway();
        Stages.Clear();
        Log($"on surface {surface} now");
    }

    /// <summary>The views drawing the current registry's world, made again when a load brings a new one.</summary>
    private void ShowWorld()
    {
        // A first run: the hero waits at the pod, unprinted, till its player picks a look.
        if (_session != null && _look == null && !(_lookEditor?.Shown ?? true)) _lookEditor.Open(Sim.Players.Look.Zero);
        if (_shown == _m) return;
        long made = System.Diagnostics.Stopwatch.GetTimestamp();
        foreach (var view in _views) view.QueueFree();
        _views.Clear();
        _faded.Clear();
        _light?.QueueFree();
        var context = new SurfaceContext(_m, _hands, () => (_session as Session)?.Progress ?? 0, () => _hero?.HeadLamps() ?? [], HeroBodies,
            Homes: !Has("--no-homes"), HomesMood: Arg("--homes") switch { "lit" => true, "dark" => false, _ => null },
            Peer: () => (_session as Session)?.Client.Id ?? 0, Tick: () => (_session as Session)?.Client.World.Tick ?? 0,
            Hosts: () => (_session as Session)?.Host != null, Arrivals: _arrivals);
        foreach (var each in SurfaceViews.All)
        {
            if (each.Make(context) is not { } view) continue;
            AddChild(view);
            _views.Add(view);
            if (each.Fades) _faded.Add((CanvasItem)view);
        }
        _grid = View<GridView>();
        var fires = View<FurnaceView>()!;
        var (power, hazards) = (_m.Power, _m.Hazards);
        AddChild(_light = new LightView(_m.Grid, Game.Light.SkyOf(_m.Rules), ground: _m.Rules is Content.PlanetType)
        {
            // --lit: every lamp at full power, to measure the light on the reference world; --dark: every lamp out, to see what glows.
            // A flare coming makes them falter as one short of power does (magnetic-flare).
            LampShare = Has("--lit") ? _ => 1f : Has("--dark") ? _ => 0f : lamp => (Stillwake.Content.Machines.Get(lamp.Kind).Draws == null ? 1f : power.Satisfaction(lamp.Id) / 1000f)
                * Features.Hazards.FlareLook.LampShare(Features.Hazards.FlareLook.Near(hazards.FlareLeft, (_session as Session)?.Progress ?? 0, hazards.FlareRules.Warning)),
            Fire = m => fires.BlazeOf(m.Id),
            FireAt = FurnaceView.LightAt,
            HeadLamps = () => _hero?.HeadLamps() ?? [],
            // The creatures live across surfaces: their view is made once, not with these.
            Shining = [.. _views.OfType<ILightSource>(), _creatures],
        });
        // A planet's sky behind its ground, its land's foot at the spawn's ground; space aboard (0076, 0079).
        _stars.Show(_m.Rules.Sky != null ? PlanetLooks.Of[_m.Rules.Id].Backdrop : null, (_m.Crew.Spawn / _m.Grid.Width + 1) * ShipCamera.Cell);
        // Last of the world's views: at a shared z, a stage's effects over its layer's objects.
        MoveChild(_stages, -1);
        MoveChild(_camera, -1);
        _shown = _m;
        double views = System.Diagnostics.Stopwatch.GetElapsedTime(made).TotalMilliseconds;
        _gridKey = null;
        if (_session is Session s)
        {
            SyncGrid(s.Client);
            // The first hash builds the serializer for every slice, some 10 ms: done while the world loads, not a second into play.
            s.Client.World.Hash();
        }
        double grid = System.Diagnostics.Stopwatch.GetElapsedTime(made).TotalMilliseconds - views;
        SyncLight();
        // What a first look at a world or a surface costs (0077: a planet is built in nearly every cell).
        double all = System.Diagnostics.Stopwatch.GetElapsedTime(made).TotalMilliseconds;
        // What the load left behind collected now, in the pause the load already makes: else the first few collections in play
        // find it alive and copy it, 4 to 7 ms each (GcPace).
        GC.Collect();
        double collected = System.Diagnostics.Stopwatch.GetElapsedTime(made).TotalMilliseconds - all;
        Log($"views of {_m.Grid.Width}×{_m.Grid.Height} made in {all:F0} ms: {views:F0} views, {grid:F0} the grid drawn, {all - views - grid:F0} the light; {collected:F0} ms collecting");
    }

    private void AimCamera(Layout? layout)
    {
        // Where the crew makes heroes: the pod's hatch, or the map's S; a map with neither is looked at whole.
        bool made = layout != null && (layout.CellsOf('S').Any() || layout.Machines().Any(m => Stillwake.Content.Machines.Get(m.Kind).Fixed));
        int start = made ? _m.Crew.Spawn : -1;
        if (Has("--perf")) _camera.Fit(new Rect2(0, 0, _m.Grid.Width, _m.Grid.Height));
        else if (start >= 0) _camera.Follow(new Vector2(start % _m.Grid.Width + 0.5f, start / _m.Grid.Width));
        else _camera.Follow(new Vector2(_m.Grid.Width, _m.Grid.Height) / 2);
        // --zoom 3: the camera held at that zoom, wherever it follows the hero.
        if (Arg("--zoom") is { } zoom) _camera.SetZoom(float.Parse(zoom, System.Globalization.CultureInfo.InvariantCulture));
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
        // The youngest garbage collected small and often, rather than a long pause every few seconds (GcPace).
        if (_gcPace.Due(GC.GetTotalAllocatedBytes(), GC.CollectionCount(0))) GC.Collect(0, GCCollectionMode.Forced, blocking: true);
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
        Follow(client);
        Snap(client);
        _stars.Seconds = (client.World.Tick + session.Progress) / World.TicksPerSecond;
        if (_allocs != null) _allocs.Counting = client.World.Tick > 5 * World.TicksPerSecond;
        if (_role != "alone" && client.World.Tick / 100 != before / 100) Log($"tick {client.World.Tick} hash {client.World.Hash():x16}");
        if (_worldId != null && _role != "guest" && client.World.Tick - _lastSaveTick >= AutosaveTicks) SaveNow();
        var heard = client.TakeHeroes();
        _heroes += heard.Count;
        DevTick(client, before);
        ShowWorld();
        SeeArrivals(client, delta);
        _frameLog?.Mark("sim");
        if (client.World.Tick != before) SyncLight();
        if (client.World.Tick != before) ShakeFalling(client);
        _frameLog?.Mark("light");
        _hero?.Frame(client, _m, _camera, heard, session.Progress, session.Host != null, delta * 1000);
        _frameLog?.Mark("hero");
        // After the hero, so a door it toggles shows on the frame E is pressed (0014).
        SyncGrid(client);
        _frameLog?.Mark("grid");
        if (_items != null) _items.Panel.Furnace = _m.Furnace.At;
        if (_items != null) _items.Panel.Trade = _m.Trade;
        if (_items != null) (_items.Panel.Charger, _items.Panel.ChargeRate) = (_m.Power.Charges, _m.Power.ChargeRate);
        if (_items != null) _items.Panel.Draw = _m.Power.Draw;
        if (_items != null) _items.Panel.Crafting = _m.Crafting;
        if (_items != null) _items.Frame(client, _m.Grid, _m.Rooms, _m.Items, _m.Research, _m.Debris, SpotOf(_hero), session.Progress, delta * 1000);
        if (_fabricator != null) _fabricator.Draw = _m.Power.Draw;
        _fabricator?.Frame(client, _m.Crafting, _m.Research, _m.Items, _m.Grid, delta * 1000);
        _frameLog?.Mark("items");
        _debris.Frame(client, _m.Debris, _camera.View(), session.Progress, delta * 1000);
        _frameLog?.Mark("debris");
        // After the hero, so a strike it sent this frame flinches its creature at once (0102).
        _creatures.Frame(client, _m.Creatures, _m.Debris, _m.Grid, _m.Rooms, _m.Rules, _camera.View(), session.Progress, delta * 1000, CreatureLooks.PaletteOf.GetValueOrDefault(_m.Rules.Id, CreatureLooks.Palette), _seed);
        _frameLog?.Mark("creatures");
        ShowHands(client, delta * 1000);
        _frameLog?.Mark("hands");
    }

    /// <summary>Where the hero stands for its items: what a drop is thrown from, what it picks up.</summary>
    private HeroSpot? SpotOf(HeroView? view)
    {
        if (view == null) return null;
        var hero = view.Current;
        return new HeroSpot(hero.X, hero.Y, hero.Facing, hero.BodyCell(_m.Grid.Width), view.Floats, view.PassedOut);
    }

    /// <summary>The plan, cables and overlays (<see cref="Hands"/>): the hero's body sways the cables.</summary>
    private void ShowHands(Client client, double elapsedMs)
    {
        _bodies.Clear();
        if (_hero != null)
        {
            // Every hero sways the cables, this one and the others where they are drawn.
            foreach (var other in _hero.Others) _bodies.Add(BodyOf(other.Hero));
            _bodies.Add(BodyOf(_hero.Current));
        }
        foreach (var scenario in _scenarios) scenario.Frame(client, _m, Log);
        _hands.Frame(client, _m, _bodies, elapsedMs);
        // The open plan draws its focused layer whole and the other faded, aboard only (0110).
        var fade = Features.Building.Tools.AlphasOf(_hands.Plan.Shown);
        _grid?.Fade(fade);
        foreach (var view in _faded) view.Modulate = new Color(1, 1, 1, fade.Front);
    }

    /// <summary>The surface's view of type <typeparamref name="T"/>; none where this surface has none (lava aboard).</summary>
    private T? View<T>() where T : Node
    {
        foreach (var view in _views)
            if (view is T found) return found;
        return null;
    }

    private readonly List<Body> _bodies = [];

    /// <summary>The middle of every hero's body on this surface, in pixels of the world: the resonator's arc leaps to it (0091).</summary>
    private IReadOnlyList<Godot.Vector2> HeroBodies()
    {
        _heroBodies.Clear();
        if (_hero == null) return _heroBodies;
        foreach (var other in _hero.Others) _heroBodies.Add(MiddleOf(other.Hero));
        _heroBodies.Add(MiddleOf(_hero.Current));
        return _heroBodies;
    }

    private readonly List<Godot.Vector2> _heroBodies = [];

    private static Godot.Vector2 MiddleOf(Hero h) =>
        new((float)(h.X / (double)Hero.Unit * ShipCamera.Cell), (float)((h.Y - Content.Hero.Height / 2.0) / Hero.Unit * ShipCamera.Cell));

    private static Body BodyOf(Hero h)
    {
        const double unit = Hero.Unit;
        return new Body((h.X - Content.Hero.Width / 2.0) / unit, (h.X + Content.Hero.Width / 2.0) / unit,
            (h.Y - Content.Hero.Height) / unit, h.Y / unit, h.Vx / unit, h.Vy / unit);
    }

    /// <summary>The grid as this peer shows it, redrawn when that changes: a tick, or a door or valve toggle of its own sent (0014).</summary>
    private void SyncGrid(Client client)
    {
        var shown = Predicted.Grid(client, _m.Grid, _m.Pipes);
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
        var worn = _m.Players.Worn;
        return (_m.Players.ColourOf(s.Client.Id), worn.Where(w => w.Peer != s.Client.Id).Select(w => w.Colour).ToHashSet());
    }


    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest) Quit();
    }

    private void Quit()
    {
        SaveIfOwned();
        _frameLog?.Report();
        if (_allocs != null) GD.Print(_allocs.Report());
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
