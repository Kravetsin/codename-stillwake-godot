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
using Stillwake.Game.Features.Handbook;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Research;
using Stillwake.Game.Features.Sound;
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
/// The game: started with no arguments, the title (0031: Continue, Worlds, Play together, Quit; Testbed in a debug build), each
/// world a folder of <see cref="WorldIndex"/>, saved every five minutes and on every way out; with any, a
/// session (net/README.md) of its own straight away, or with <c>-- --host</c> one others join over ENet
/// with <c>-- --join[=address]</c>, or through the playtest server's relay with <c>--relay</c>, hosting as
/// <c>--host=name</c> and joining by <c>--join=name</c> (<see cref="Relay"/>); the world it plays drawn by the client's features after every
/// tick, and the hero: A/D walk, W/S climb, Space jumps, E works doors and valves, loads tanks, opens
/// crates and catches debris, the left button held works plans; the camera follows it; its items (Tab,
/// 1–9, the wheel, Q) and the lifepod's Fabricator beside them (E at the pod), the plan (B), the Handbook (J), the game menu (Esc: settings, save,
/// load, quit) and the dev panel (`).
/// Dev arguments after <c>--</c>, played on the wreck as drawn (<see cref="Registry.WreckMap"/>) unless they say otherwise: <c>--world</c> a new
/// world as the title makes one, the wreck in the middle of the big world, kept nowhere; <c>--perf</c> the reference world (<c>--lit</c> with every lamp lit; <c>--size WxH</c> a world that
/// large, its tiles over <c>--built WxH</c> of it from the top-left, all of it by default), <c>--gallery</c> every picture,
/// <c>--view X,Y,ZOOM</c> where the camera looks, <c>--doors</c> opens and shuts doors for recordings,
/// <c>--autopilot</c> plays the hero by a script, <c>--gym</c> a room with a ladder for it, <c>--hills</c> ground of slopes to walk (<see cref="HillsDemo"/>), <c>--slopes</c> walls cut to shapes planned with R, lit, and a corridor under a sloped ceiling (<see cref="SlopesDemo"/>), <c>--aside</c> steps the hero
/// off the spawn, so a window recording another peer's hero there sees it whole,
/// <c>--furnace</c> a lit furnace in the wreck, <c>--testbed</c> a ready ship with every machine at work (<see cref="Testbed"/>; with <c>--open=&lt;kind&gt;</c> the hero opens that machine, <c>crate@1</c> the second crate, after <c>--open-at SECONDS</c>, or with <c>--near=&lt;kind&gt;</c> only starts beside it, <see cref="OpenDemo"/>; with <c>--card=&lt;id&gt;</c> the plan is out on that build card, its tooltip shown, or with <c>--pick</c> it in hand, <see cref="CardDemo"/>), <c>--jetpack</c> it with a
/// jetpack put on and flown out of the airlock (<see cref="JetpackDemo"/>), <c>--items</c> a room with a crate, a furnace and an
/// analyzer and a script working them, <c>--debris</c> the wreck with debris about, a crate caught with <c>--autopilot</c>,
/// <c>--wreck</c> a hull section chopped with the fire axe (<see cref="WreckDemo"/>), <c>--wrecks</c> every kind of wreckage chopped in a row, <c>--harpoon</c> a crate and a section shot and reeled in, <c>--asteroid</c> two asteroids drilled and chopped out in the band (<see cref="AsteroidDemo"/>; <c>--manual</c> to play it by hand), <c>--sortie</c> the hero far out among one of each derelict and kind of asteroid, an engine block drilled (<see cref="SortieDemo"/>; <c>--manual</c> to try them by hand), <c>--grapple</c> the hero pulled up to a hall's ceiling and its wall, <c>--pod</c> the wreck's lifepod opened and crafting (<see cref="PodDemo"/>), <c>--plan-demo</c> plays the plan, cables and overlays by a script (<see cref="PlanDemo"/>), <c>--menu-demo</c>
/// the items room's inventory and the game menu (<see cref="MenuDemo"/>), <c>--handbook</c> the Handbook opened on a world that has found ten kinds (<see cref="HandbookDemo"/>),
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
    private Features.Power.ChargerLights? _chargers;
    private Features.Atmosphere.TankLooks? _tanks;
    private ShipCamera _camera = null!;
    private Stars _stars = null!;
    private string[] _args = [];
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
    private ItemsView? _items;
    private FabricatorPanel? _fabricator;
    private PodView? _pods;
    private StationView? _stations;
    private Features.Logistics.LogisticsView? _drones;
    private DebrisView _debris = null!;
    private Hands _hands = null!;
    private PlanDemo? _demo;
    private FrameLog? _frameLog;
    /// <summary>Where this machine stands on the network, for the dev panel; none alone.</summary>
    private string? _net;
    /// <summary>A world is saved this often, in ticks: five minutes of it.</summary>
    private const int AutosaveTicks = 5 * 60 * World.TicksPerSecond;
    /// <summary>The player's worlds on disk.</summary>
    private readonly WorldIndex _index = new(ProjectSettings.GlobalizePath("user://saves/worlds"), Migrations.Oldest, World.SaveFormat, World.TicksPerSecond);
    /// <summary>The world played and kept by this machine; none for a dev world or a friend's.</summary>
    private string? _worldId;
    private int _lastSaveTick;
    private DateTime? _savedAt;
    private string? _saveProblem;
    private TitleScreen? _title;
    /// <summary>Who plays on this machine (0032), and who is who in the world it keeps.</summary>
    private string _playerKey = "";
    private Roster _roster = Roster.Empty();
    /// <summary>What the title says once the scene is back to it: why a world was left. Kept across the reload.</summary>
    private static string? _next;

    public override void _Ready()
    {
        // A friend's log arrives without context: its first line says which build and system wrote it.
        GD.Print($"Stillwake {Build} on {OS.GetName()} {OS.GetVersion()}");
        _args = OS.GetCmdlineUserArgs();
        _playerKey = Profile.Key(_args);
        _cheat = Has("--cheat");
        var seconds = _args.FirstOrDefault(a => a.StartsWith("--for="));
        if (seconds != null) GetTree().CreateTimer(int.Parse(seconds[6..])).Timeout += Quit;
        // Before everything else, so every button made after it clicks.
        AddChild(new SoundView(Floats, () => _title != null));

        // The views of this player's hands, made first: the hero, its items and the debris share its Ui and mouse.
        _hands = new Hands();
        // The slots' size kept on this machine (0058); --slots=large shows the other for a recording, keeping nothing then.
        var slots = _args.FirstOrDefault(a => a.StartsWith("--slots=", StringComparison.Ordinal));
        _hands.Ui.SlotScale.Set(slots != null ? Slots.ScaleNamed(slots[8..]) : Settings.SlotScale());
        if (slots == null) _hands.Ui.SlotScale.Subscribe(() => Settings.Keep(_hands.Ui.SlotScale.Get()));
        ItemIcons.MakeSilhouettes();
        _debris = new DebrisView(_hands.Ui);
        AddChild(_stars = new Stars());
        AddChild(_debris);
        if (Has("--debris")) AddChild(new DebrisDemo(_debris, _hands.Ui));
        if (Has("--wreck") || Has("--wrecks") || Has("--harpoon") || Has("--grapple"))
            AddChild(new WreckDemo(_debris, _hands.Ui, all: Has("--wrecks"), harpoon: Has("--harpoon"), grapple: Has("--grapple")));
        AddChild(_camera = new ShipCamera());
        if (Has("--world") || Has("--fly") || Has("--asteroid") || Has("--sortie")) _layout = Registry.Wreck;
        if (Has("--gym")) _layout = HeroAutopilot.Gym;
        if (Has("--hills")) _layout = HillsDemo.Map;
        if (Has("--slopes")) _layout = SlopesDemo.Map;
        if (Has("--items") || Has("--menu-demo")) _layout = ItemsDemo.Map;
        if (Has("--testbed") || Has("--jetpack") || Has("--charge")) _layout = Testbed.Map;
        if (Has("--wreck") || Has("--wrecks") || Has("--harpoon")) _layout = WreckDemo.Map;
        if (Has("--grapple")) _layout = WreckDemo.Hall;
        // The dev worlds are looked at, not walked: the camera moves by hand there.
        if (!Has("--perf") && !Has("--gallery"))
        {
            var script = Has("--aside") ? HeroAutopilot.Aside() : Has("--jetpack") ? HeroAutopilot.Jetpack() : Has("--fly") && !Has("--manual") ? HeroAutopilot.Fly() : !Has("--autopilot") ? null : Has("--gym") ? HeroAutopilot.GymRun() : Has("--hills") ? HeroAutopilot.HillsRun(HillsDemo.HighPlateau) : Has("--slopes") ? HeroAutopilot.SlopesRun() : HeroAutopilot.Wreck();
            AddChild(_hero = new HeroView(_hands.Ui, _hands.Mouse, script));
            // --edge=N: the hero starts N cells past the field's left line on the world's middle row, to see the cold (0050).
            if (_args.FirstOrDefault(a => a.StartsWith("--edge=")) is { } edge)
                _hero.Start = m => m.Grid.Height / 2 * m.Grid.Width + Math.Max(0, m.Grid.Zone.Left - Content.Field.Margin - int.Parse(edge[7..]));
            else if (Has("--fly") || Has("--asteroid")) _hero.Start = FarDemo.StartOf;
            else if (Has("--sortie")) _hero.Start = SortieDemo.StartOf;
            else if (Has("--charge")) _hero.Start = ChargeDemo.StartOf;
            else if (Has("--die")) _hero.Start = DieDemo.StartOf;
            else if ((Opens ?? Near) is { } opened) _hero.Start = m => OpenDemo.StartOf(m, opened);
            AddChild(_items = new ItemsView(_hands.Ui, demo: Has("--items")));
            // A queued station's inputs are the items feature's slots, laid out in the crafting feature's panel.
            AddChild(_fabricator = new FabricatorPanel(_hands.Ui, peer => AsciiArt.Colour((uint)Suits.Of(_m.Crew, peer))) { Lend = _items.Panel.Lend });
            // The drone badge is the logistics feature's, put in the items' and crafting panels' headers where drones have work (0062).
            Features.Logistics.DroneWorld? Drones() => _session is Session s ? new(_m.Logistics, s.Client.InFlight) : null;
            void SendNow(Command c) => (_session as Session)?.Client.Send(c);
            Control? BadgeOf(int machine) => _m.Logistics.Servable(machine) ? new Features.Logistics.ServedBadge(machine, Drones, SendNow, _hands.Ui.Tip) : null;
            _items.Panel.Served = BadgeOf;
            _items.Panel.Kept = p => p.Crate is int id && _m.Logistics.Served(id) ? _m.Items.KeptAt(id, p.Slot) : null;
            _items.Panel.Supplied = (id, item) => _m.Logistics.Supplied(id, item);
            _fabricator.Served = BadgeOf;
            // Its own layer, above the world: read from whatever registry the session runs now.
            var gauge = new SuitGauge(() => _session is Session s ? Danger.Read(_m.Crew, _m.Atmosphere, s.Client.Id) : (Content.Crew.Suit, SuitDanger.None), _hands.Ui);
            AddChild(gauge);
            // --electric=fireAxe: that item counts as electric, so the energy bar shows before any tool is (0053).
            var electric = _args.FirstOrDefault(a => a.StartsWith("--electric="))?[11..];
            Func<string, bool>? isElectric = electric == null ? null : item => item == electric || Content.Items.Get(item).Draw != null;
            AddChild(new EnergyBar(() => _session is Session s ? Charges.EnergyOf(_hands.Ui.Wielded.Get(), _m.Items.BeltCharge(s.Client.Id), isElectric) : null, _hands.Ui));
            AddChild(new Frost(() => _session != null && _hero is { Placed: true } h ? Report.Cold(_m.Crew, h.Current.BodyCell(_m.Grid.Width)) : 0));
            // Home and friends off the screen, pointed at from its edge, clear of the HUD's panels.
            var (panel, fabricator, building, overlays) = (_items.Panel, _fabricator, _hands.Menu, _hands.Overlays);
            IEnumerable<Rect2> Hud()
            {
                foreach (var covered in panel.Covers()) yield return covered;
                yield return gauge.Covers;
                yield return building.Covers;
                yield return overlays.Covers;
                if (fabricator.Covers is { } crafts) yield return crafts;
            }
            AddChild(new MarkerLayer(_hands.Ui, _camera, () => _session is Session s && _hero is { Placed: true } h ? (h, _m, s.Client.Id) : null, Hud));
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
        // Every way out saves first: the window's close as well.
        GetTree().AutoAcceptQuit = false;
        _updater.BeforeRestart = SaveIfOwned;
        if (_args.Length == 0) ShowTitle();
        else if (hostAs != null) Host(hostAs.Length > 7 ? hostAs[7..] : null);
        else if (join != null) Join(join.Length > 7 ? join[7..] : "127.0.0.1", BadNetwork.From(_args), Relay());
        else Alone();
        AddChild(new DevPanel(() => _session, () => _m, () => _net));
        // The Handbook (0066): each section a feature's own page; what opening it puts away is wired here, so no feature reaches into another.
        var debrisPage = new HandbookPage(_hands.Ui, () => _session != null ? _m.Debris : null);
        var handbook = new HandbookPanel(_hands.Ui, () => _session != null && _hero != null, [new HandbookSection(T("handbook.debris"), HandbookPage.SectionIcon, debrisPage)])
        {
            Opening = () =>
            {
                _items?.Panel.Close();
                _hands.Plan.PutPlanAway();
            },
        };
        AddChild(handbook);
        var suits = new SuitColours(SuitsWorn, colour => _session?.Client.Send(new Sim.Crew.PickColour(colour)));
        var menu = new GameMenu(_hands.Ui,
            busy: () => (_items?.Panel.Shown ?? false) || handbook.Shown || _hands.Plan.PlanOut.Get(),
            putAway: () =>
            {
                // A search in the build menu goes first, the menu staying open.
                if (_hands.Menu.DropSearch()) return;
                _items?.Panel.Close();
                handbook.Close();
                _hands.Plan.PutPlanAway();
            },
            handbook: handbook.Open,
            suits, SaveWorld, inWorld: () => _session != null, owns: () => _role != "guest", exitToTitle: () => ExitToTitle(null), Quit, Foot,
            version: Updates.Of(Build).ToString(), updateTo: () => _updater.Available?.Tag, update: _updater.Update,
            updating: () => _updater.Status);
        AddChild(menu);
        AddChild(_updater);
        // Playing together, how long an action takes to happen: sent, stamped by the host, run here.
        AddChild(new VersionCorner(Updates.Of(Build).ToString(), () => _role == "alone" ? null : _session?.RoundTrip.Median));
        if (Has("--menu-demo") && _items != null) AddChild(new MenuDemo(_items, menu, suits));
        if (Has("--handbook")) AddChild(new HandbookDemo(debrisPage));
        if (Has("--jetpack") && _items != null) AddChild(new JetpackDemo(_items));
        if (Has("--charge") && _items != null) AddChild(new ChargeDemo(_items, _hands.Ui, () => _m));
        if (Opens is { } open) AddChild(new OpenDemo(_hands.Ui, open, () => _m, Arg("--open-at") is { } at ? double.Parse(at, System.Globalization.CultureInfo.InvariantCulture) : 1.0));
        if (_args.FirstOrDefault(a => a.StartsWith("--card=", StringComparison.Ordinal)) is { } card) AddChild(new CardDemo(_hands, card[7..], pick: Has("--pick")));
        if (Has("--slopes")) AddChild(new SlopesDemo(_hands));
        if (Has("--asteroid") && !Has("--manual")) AddChild(new AsteroidDemo(_debris, _hands.Ui, () => _session is Session s ? (s.Client, _m) : null));
        if (Has("--sortie") && !Has("--manual")) AddChild(new SortieDemo(_debris, _hands.Ui, () => _session is Session s ? (s.Client, _m) : null));
        if (Has("--fly")) AddChild(new FarDemo(_debris, () => _session is Session s ? (s.Client, _m) : null));
        if (Has("--pod")) AddChild(new PodDemo(_hands.Ui, () => _session is Session s ? (s.Client, _m) : null, () => _fabricator));
        if (Has("--frames") || Has("--frames=vsync")) AddChild(_frameLog = new FrameLog(vsync: Has("--frames=vsync")));
        if (Has("--measure"))
            AddChild(new PerfReadout(() => $"sync-worst={_worstSyncMs:F2}ms lamps={_light?.LampCount} lights={_light?.LightCount}"));
    }

    /// <summary>Whether this player's placed hero floats out in space: the world silent, its own touch dulled, the bed of space playing (0033, 0041).</summary>
    private bool Floats() => _session != null && _hero is { Placed: true, Floats: true };

    private bool Has(string flag) => _args.Contains(flag);

    /// <summary><c>--open=&lt;kind&gt;</c>: the machine of that kind the hero opens at the start (<see cref="OpenDemo"/>).</summary>
    private string? Opens => _args.FirstOrDefault(a => a.StartsWith("--open=", StringComparison.Ordinal))?[7..];

    /// <summary><c>--near=&lt;kind&gt;</c>: the machine of that kind the hero starts beside, opening nothing: for recording the room round it.</summary>
    private string? Near => _args.FirstOrDefault(a => a.StartsWith("--near=", StringComparison.Ordinal))?[7..];
    private static (int, int) Size(string? text, (int, int) or) => text?.Split('x') is [var w, var h] ? (int.Parse(w), int.Parse(h)) : or;

    /// <summary>The reference world, at <c>--size</c> and built over <c>--built</c> of it.</summary>
    private Layout PerfLayout()
    {
        var (w, h) = Size(Arg("--size"), (256, 128));
        var (bw, bh) = Size(Arg("--built"), (w, h));
        return DevWorlds.Perf(w, h, bw, bh);
    }
    private string? Arg(string flag) => _args.SkipWhile(a => a != flag).Skip(1).FirstOrDefault();

    private IReadOnlyList<Sim.Module> Modules()
    {
        _m = new Registry();
        return _m.All;
    }

    private void Alone()
    {
        var modules = Modules();
        var layout = Has("--perf") ? PerfLayout() : _layout;
        var world = Has("--gallery") ? DevWorlds.Gallery(modules)
            : Has("--furnace") ? FurnaceDemo.Create(modules)
            : Has("--testbed") || Has("--jetpack") || Has("--charge") ? Testbed.Create(modules)
            : World.Create(1, layout, modules);
        if (Has("--items") || Has("--menu-demo")) world = ItemsDemo.Seed(world, Modules);
        if (Has("--debris")) world = DebrisDemo.Seed(world, Modules);
        if (Has("--wreck") || Has("--wrecks") || Has("--harpoon")) world = WreckDemo.Seed(world, Modules, all: Has("--wrecks"), harpoon: Has("--harpoon"));
        if (Has("--grapple")) world = WreckDemo.SeedGrapple(world, Modules);
        if (Has("--pod")) world = PodDemo.Seed(world, Modules);
        if (Has("--hills")) world = HillsDemo.Seed(world, Modules);
        if (Has("--slopes")) world = SlopesDemo.Seed(world, Modules);
        if (Has("--jetpack")) world = JetpackDemo.Seed(world, Modules);
        if (Has("--charge")) world = ChargeDemo.Seed(world, Modules);
        if (Has("--asteroid")) world = AsteroidDemo.Seed(world, _m, Modules);
        if (Has("--sortie")) world = SortieDemo.Seed(world, _m, Modules);
        if (Arg("--bow") is { } bow) FarDemo.Bow = int.Parse(bow);
        if (Has("--fly")) world = FarDemo.Seed(world, _m, Modules, int.Parse(_args.FirstOrDefault(a => a.StartsWith("--far="))?[6..] ?? "0"));
        if (Has("--die")) world = DieDemo.Seed(world, Modules);
        if (Has("--handbook")) world = HandbookDemo.Seed(world, Modules);
        PlayDev(world, Has("--gallery") ? null : layout);
    }

    /// <summary>The title's Testbed button, in a debug build: the testbed played as <c>--testbed</c> plays it.</summary>
    private void PlayTestbed()
    {
        if (_together.Mode is TogetherMode.Joining or TogetherMode.Opening) return;
        CloseTitle();
        PlayDev(Testbed.Create(Modules()), Testbed.Map);
    }

    /// <summary>Plays a dev world as a session of this machine's own, kept nowhere: no world id, so never saved.</summary>
    private void PlayDev(World world, Layout? aimAt)
    {
        _roster = Roster.Empty();
        var (host, client) = LocalSession.Create(world, Build, _roster.SeatOf(_playerKey));
        _session = new Session(host, client);
        ShowWorld();
        AimCamera(aimAt);
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
        if (Has("--perf")) _layout = PerfLayout();
        var world = World.Create(1, _layout, Modules());
        // The debris and wreck demos are played hosting too, so a guest sees a catch, or the host chop.
        if (Has("--debris")) world = DebrisDemo.Seed(world, Modules);
        if (Has("--wreck") || Has("--wrecks") || Has("--harpoon")) world = WreckDemo.Seed(world, Modules, all: Has("--wrecks"), harpoon: Has("--harpoon"));
        _roster = Roster.Empty();
        var (host, client) = LocalSession.Create(world, Build, _roster.SeatOf(_playerKey));
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
        var keys = new Dictionary<long, string>();
        link.Connected += (peer, channel) =>
        {
            // Seated only once it says who it is (0032): the same seat in this world every time.
            channel.OnMessage(message =>
            {
                if (message is not HelloMessage hello || ids.ContainsKey(peer)) return;
                if (hello.Player == _playerKey || keys.ContainsValue(hello.Player))
                {
                    Log($"peer refused: {hello.Player} is aboard already");
                    channel.Send(new RefusedMessage(T("menu.aboard")));
                    return;
                }
                int seat = _roster.SeatOf(hello.Player);
                SaveRoster();
                keys[peer] = hello.Player;
                ids[peer] = host.Admit(channel, seat);
                _net = $"hosting {where} · peers {ids.Count}";
                Log($"peer {seat} joined at tick {host.Tick}");
            });
        };
        link.Disconnected += peer =>
        {
            keys.Remove(peer);
            // Its hero leaves with it, its suit colour freed; its inventory stays in its seat.
            if (!ids.Remove(peer, out int id)) return;
            host.Disconnect(id, client.World.Encode(new Sim.Crew.Leave()));
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
            _stopLink = link.Close;
            Client.Join(Load, network?.Over(channel, new GodotTiming(GetTree())) ?? channel, Build,
                client =>
                {
                    CloseTitle();
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
                    _stopLink?.Invoke();
                    _stopLink = null;
                    _poll = () => { };
                },
                player: _playerKey);
        };
        link.Disconnected += _ =>
        {
            Log("the host left");
            if (_session != null) ExitToTitle(T("menu.hostLeft"));
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
        // From the title: the world picked in Worlds, else Continue's, else a new one.
        if (_session == null)
        {
            if ((_title?.Worlds.Picked ?? _index.Continue()?.Id) is string id) Play(id);
            else NewWorld("");
            if (_session == null) return;
        }
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
            var grid = root["slices"]!["grid"]!;
            var structure = Runs.Cells(grid["structure"]!);
            int door = Array.IndexOf(structure, Structure.DoorClosed);
            structure[door] = Structure.DoorOpen;
            grid["structure"] = Runs.Node(structure);
            save = root.ToJsonString();
            Log($"cheated: door {door} open here only");
        }
        return World.Load(save, Modules());
    }

    /// <summary>Save files in Godot's user folder: the menu's world and the last desync report.</summary>
    private readonly Saves _saves = new(ProjectSettings.GlobalizePath("user://saves"));
    private static readonly System.Text.Json.JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>The menu's save: the world as this machine has it, to its folder; how it went as an i18n key.</summary>
    private string SaveWorld()
    {
        if (_session == null || _worldId == null || _role == "guest") return "menu.nothingToSave";
        return SaveNow() ? "menu.saved" : "menu.saveFailed";
    }

    /// <summary>Writes the world played to its folder, the save before kept; false, and why kept for the menu's foot, when it could not.</summary>
    private bool SaveNow()
    {
        if (_session is not Session s || _worldId is not string id) return false;
        try
        {
            _index.Write(id, s.Client.World.Save());
            (_lastSaveTick, _savedAt, _saveProblem) = (s.Client.World.Tick, DateTime.Now, null);
            return true;
        }
        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
        {
            Log($"not saved: {e.Message}");
            (_lastSaveTick, _saveProblem) = (s.Client.World.Tick, e.Message);
            return false;
        }
    }

    /// <summary>Keeps who is who in the world played, as soon as a seat is given: a crash then forgets nobody.</summary>
    private void SaveRoster()
    {
        if (_worldId is not string id) return;
        try
        {
            _index.WritePlayers(id, _roster.ToJson());
        }
        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
        {
            Log($"the roster not kept: {e.Message}");
        }
    }

    /// <summary>Saves the world when this machine keeps it: before every way out.</summary>
    private void SaveIfOwned()
    {
        if (_role != "guest") SaveNow();
    }

    /// <summary>The menu's foot: the world, when it was saved, and the code while hosting.</summary>
    private string Foot()
    {
        var parts = new List<string>();
        if (_worldId != null) parts.Add(_index.NameOf(_worldId));
        if (_saveProblem != null) parts.Add(T("menu.notSaved", ("why", _saveProblem)));
        else if (_savedAt is DateTime at)
        {
            int minutes = (int)(DateTime.Now - at).TotalMinutes;
            parts.Add(minutes < 1 ? T("menu.savedNow") : T("menu.savedAgo", ("minutes", minutes)));
        }
        if (_together.Mode == TogetherMode.Hosting) parts.Add(T("menu.hostingCode", ("code", _together.Code ?? "")));
        else if (_together.Problem != null && _role != "guest") parts.Add(_together.Problem);
        return string.Join(" · ", parts);
    }

    /// <summary>The title, loading nothing: what it says first, if the scene came back to it with a reason.</summary>
    private void ShowTitle()
    {
        AdoptOldSave();
        var worlds = new WorldsPanel(_index.List, Play, id => _index.Delete(id), NewWorld,
            busy: () => _together.Mode is TogetherMode.Joining or TogetherMode.Opening);
        AddChild(_title = new TitleScreen(_index.Continue, Continue, worlds, this, Quit,
            Updates.Of(Build).ToString(), () => _updater.Available?.Tag, _updater.Update, () => _updater.Status,
            testbed: OS.IsDebugBuild() ? PlayTestbed : null));
        if (_next is { } said) _title.Say(said);
        _next = null;
    }

    /// <summary>The save slot of v0.0.3 and before, taken into the worlds once, so an update loses no ship.</summary>
    private void AdoptOldSave()
    {
        try
        {
            if (_index.Adopt(ProjectSettings.GlobalizePath("user://saves/world.json"), T("title.oldSave"), DateTime.Now) is string id)
                Log($"the old save slot is world {id} now");
        }
        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
        {
            Log($"the old save slot not taken in: {e.Message}");
        }
    }

    private void CloseTitle()
    {
        _title?.QueueFree();
        _title = null;
    }

    /// <summary>Continue: the last world played, or a new one on a first run.</summary>
    private void Continue()
    {
        if (_together.Mode is TogetherMode.Joining or TogetherMode.Opening) return;
        if (_index.Continue() is { } world) Play(world.Id);
        else NewWorld("");
    }

    /// <summary>Plays world <paramref name="id"/>, its save brought up to this build's format; a failure stays on the title, saying why.</summary>
    private void Play(string id)
    {
        try
        {
            string save = _index.Read(id) ?? throw new System.IO.FileNotFoundException(T("worlds.noSave"));
            // The registry is swapped only once the save has loaded: a broken one leaves nothing half made.
            var m = new Registry();
            var world = World.Load(Migrations.Upgrade(save), m.All);
            _m = m;
            Enter(world, id, Roster.Read(_index.ReadPlayers(id), _playerKey));
        }
        catch (Exception e) when (e is System.IO.IOException or System.Text.Json.JsonException or System.IO.InvalidDataException or KeyNotFoundException or InvalidOperationException)
        {
            Log($"world {id} not loaded: {e.Message}");
            _title?.Worlds.Say(T("worlds.failed", ("why", e.Message)));
            _title?.Say(T("worlds.failed", ("why", e.Message)));
        }
    }

    /// <summary>A new world in the wreck, named <paramref name="name"/>, saved at once and played.</summary>
    private void NewWorld(string name)
    {
        string id = _index.Create(name, DateTime.Now);
        var world = World.Create(1, Registry.Wreck, Modules());
        _index.Write(id, world.Save());
        Enter(world, id, Roster.Empty());
    }

    /// <summary>Plays <paramref name="world"/> as a session of this machine's own, kept as <paramref name="id"/>.</summary>
    private void Enter(World world, string id, Roster roster)
    {
        _roster = roster;
        int seat = roster.SeatOf(_playerKey);
        var (host, client) = LocalSession.Create(world, Build, seat);
        _session = new Session(host, client);
        // Saved with friends aboard, the world still holds their heroes: they come back when their players do.
        foreach (var hero in _m.Crew.Heroes.Where(h => h.Id != seat).ToList()) host.Release(hero.Id, client.World.Encode(new Sim.Crew.Leave()));
        (_worldId, _lastSaveTick, _savedAt, _saveProblem) = (id, world.Tick, DateTime.Now, null);
        SaveRoster();
        CloseTitle();
        ShowWorld();
        AimCamera(Registry.Wreck);
    }

    /// <summary>
    /// Leaves the world for the title: saved if this machine keeps it, the link closed and the session dropped,
    /// what is drawn of it let go, and the scene loaded afresh once the frame ends, every view with it;
    /// <paramref name="why"/> said on the title.
    /// </summary>
    private void ExitToTitle(string? why)
    {
        SaveIfOwned();
        _stopLink?.Invoke();
        _stopLink = null;
        _session = null;
        Particles.Shared.Clear();
        _next = why;
        // A reload takes the scene out of the tree at once: deferred, the frame that asked (a click, the host gone) ends whole.
        GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
    }

    /// <summary>The views drawing the current registry's world, made again when a load brings a new one.</summary>
    private void ShowWorld()
    {
        if (_shown == _m) return;
        _grid?.QueueFree();
        _light?.QueueFree();
        _furnace?.QueueFree();
        _screens?.QueueFree();
        _chargers?.QueueFree();
        _tanks?.QueueFree();
        _pods?.QueueFree();
        _stations?.QueueFree();
        _drones?.QueueFree();
        AddChild(_grid = new GridView(_m.Grid));
        AddChild(_screens = new AnalyzerScreens(_m.Grid, _m.Research, _m.Power));
        AddChild(_chargers = new Features.Power.ChargerLights(_m.Grid, _m.Items, _m.Power));
        AddChild(_tanks = new Features.Atmosphere.TankLooks(_m.Grid, _m.Atmosphere));
        AddChild(_pods = new PodView(_m.Grid, _m.Crafting, _m.Items, _m.Crew));
        AddChild(_stations = new StationView(_m.Grid, _m.Crafting));
        var ui = _hands.Ui;
        AddChild(_drones = new Features.Logistics.LogisticsView(_m.Grid, _m.Items, _m.Power, _m.Logistics, () => (_session as Session)?.Progress ?? 0,
            () => ui.InventoryFoot.Get() != null && ui.OpenCrate.Get() is int open && _m.Grid.MachineById(open)?.Kind == Content.Logistics.Bay ? open : null,
            () => ui.Hand.Get() == Holding.Card && _hands.Plan.Selected.Get() is Features.Building.PlaceTool { Placement.Kind: var kind } && kind == Content.Logistics.Bay ? _hands.Mouse.Pointer() : null));
        var power = _m.Power;
        // The fires before the light: each frame a fire picks its picture, then its light follows it.
        var fires = new FurnaceView(_m.Grid, _m.Furnace);
        AddChild(_furnace = fires);
        AddChild(_light = new LightView(_m.Grid)
        {
            // --lit: every lamp at full power, to measure the light on the reference world.
            LampShare = Has("--lit") ? _ => 1f : lamp => power.Satisfaction(lamp.Id) / 1000f,
            Fire = m => fires.BlazeOf(m.Id),
            FireAt = FurnaceView.LightAt,
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
        _stars.Seconds = (client.World.Tick + session.Progress) / World.TicksPerSecond;
        if (_role != "alone" && client.World.Tick / 100 != before / 100) Log($"tick {client.World.Tick} hash {client.World.Hash():x16}");
        if (_worldId != null && _role != "guest" && client.World.Tick - _lastSaveTick >= AutosaveTicks) SaveNow();
        var heard = client.TakeHeroes();
        _heroes += heard.Count;
        if (Has("--auto")) Auto(client, before);
        if (Has("--doors") && client.World.Tick != before)
            foreach (var c in Autopilot.At(client.World.Tick, _m.Grid)) client.Send(c);
        // --toggle: the first doorway opened and shut on alternate ticks, so one is always in flight, as at a busy airlock.
        if (Has("--toggle") && client.World.Tick != before && (_toggled ??= Autopilot.At(20, _m.Grid).FirstOrDefault()) is SetDoor toggle)
            client.Send(toggle with { Open = client.World.Tick % 2 == 0 });
        ShowWorld();
        _frameLog?.Mark("sim");
        if (client.World.Tick != before) SyncLight();
        _frameLog?.Mark("light");
        _hero?.Frame(client, _m, _camera, heard, session.Progress, delta * 1000);
        _frameLog?.Mark("hero");
        // After the hero, so a door it toggles shows on the frame E is pressed (0014).
        SyncGrid(client);
        _frameLog?.Mark("grid");
        if (_items != null) _items.Panel.Furnace = _m.Furnace.At;
        if (_items != null) _items.Panel.Charger = _m.Power.Charges;
        if (_items != null) _items.Panel.Crafting = _m.Crafting;
        if (_items != null) _items.Frame(client, _m.Grid, _m.Rooms, _m.Items, _m.Research, _m.Debris, SpotOf(_hero), session.Progress, delta * 1000);
        _fabricator?.Frame(client, _m.Crafting, _m.Research, _m.Items, _m.Grid, delta * 1000);
        _frameLog?.Mark("items");
        _debris.Frame(client, _m.Debris, _camera.View(), session.Progress, delta * 1000);
        _frameLog?.Mark("debris");
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
            client.Send(new SetDoor(_doors[_role == "guest" ? 1 : 0], true));
            Log($"sent SetDoor({_doors[_role == "guest" ? 1 : 0]}) at tick {client.World.Tick}, {client.InFlight.Count} in flight");
        }
        if (client.World.Tick / 20 != before / 20) client.SendHero($"{{\"tick\":{client.World.Tick}}}");
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest) Quit();
    }

    private void Quit()
    {
        SaveIfOwned();
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
