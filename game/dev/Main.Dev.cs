// Main's dev arguments (DevModes): the maps, scripts, demos and worlds they start, and what they do each frame.
using System;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Debris;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Features.Furnace;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Menu;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game;

public partial class Main
{
    /// <summary>The arguments after <c>--</c> (<see cref="Dev.DevModes"/>).</summary>
    private Dev.DevArgs _dev = new([]);

    private bool Has(string flag) => _dev.Has(flag);

    private string? Arg(string flag) => _dev.Arg(flag);

    /// <summary>
    /// <c>--arc-drill</c>, with <c>--asteroid</c>, <c>--planet</c> or <c>--ground</c>: a drill with the arc module fitted in the hero's
    /// hands at the start (0098), the one way a module gets into a tool till the tool bench.
    /// </summary>
    private string? ArcDrill => Has("--arc-drill") ? Content.Modules.Arc.Item : null;

    /// <summary><c>--open=&lt;kind&gt;</c>: the machine of that kind the hero opens at the start (<see cref="Dev.OpenDemo"/>).</summary>
    private string? Opens => _dev.Value("--open");

    /// <summary><c>--charger=arc|plain</c>: the testbed's charging station filling an empty accumulator, fitted or not (<see cref="Dev.ChargerArcDemo"/>).</summary>
    private string? ChargerRace => _dev.Value("--charger");

    /// <summary><c>--near=&lt;kind&gt;</c>: the machine of that kind the hero starts beside, opening nothing: for recording the room round it.</summary>
    private string? Near => _dev.Value("--near");

    /// <summary><c>--pose-sheet=&lt;dir&gt;</c>: the hero's holds drawn alone and saved, nothing else made; whether it was given.</summary>
    private bool PoseSheetOnly()
    {
        if (_dev.Value("--pose-sheet") is not { } sheet) return false;
        string item = _dev.Value("--pose-item") ?? Content.Weapons.Drill.Item;
        AddChild(new Dev.PoseSheet(sheet, item));
        return true;
    }

    /// <summary><c>--for=seconds</c>: quits after a while.</summary>
    private void QuitAfterAWhile()
    {
        var seconds = _dev.Value("--for");
        if (seconds != null) GetTree().CreateTimer(int.Parse(seconds)).Timeout += Quit;
    }

    /// <summary>The debris and wreck demos, made right after the debris view they work.</summary>
    private void AddDebrisDemos()
    {
        if (Has("--debris")) AddChild(new DebrisDemo(_debris, _hands.Ui));
        if (Has("--wreck") || Has("--wrecks") || Has("--harpoon") || Has("--grapple"))
            AddChild(new WreckDemo(_debris, _hands.Ui, all: Has("--wrecks"), harpoon: Has("--harpoon"), grapple: Has("--grapple")));
    }

    /// <summary>The map a dev run plays, the last argument naming one winning: the wreck as drawn with none.</summary>
    private Layout DevLayout(Layout layout)
    {
        if (Has("--world") || Has("--fly") || Has("--asteroid") || Has("--sortie")) layout = Registry.Wreck;
        if (Has("--gym")) layout = HeroAutopilot.Gym;
        if (Has("--poses")) layout = HeroAutopilot.Hall;
        if (Has("--ladders") && !Has("--strip")) layout = HeroAutopilot.Ladders;
        if (Has("--ledges")) layout = HeroAutopilot.Ledges;
        if (Has("--hills")) layout = Dev.HillsDemo.Map;
        if (Has("--floor")) layout = Dev.FloorDemo.Map;
        if (Has("--segments")) layout = Dev.Autopilot.Segments;
        if (Has("--hatches")) layout = Dev.Autopilot.Hatches;
        if (Has("--lone-hatch") || Has("--lone-hatch=open")) layout = Dev.Autopilot.LoneHatch(Has("--lone-hatch=open"));
        if (Has("--items") || Has("--menu-demo")) layout = ItemsDemo.Map;
        if (Has("--testbed") || Has("--jetpack") || Has("--charge") || Has("--dust")) layout = Dev.Testbed.Map;
        if (Has("--wreck") || Has("--wrecks") || Has("--harpoon")) layout = WreckDemo.Map;
        if (Has("--grapple")) layout = WreckDemo.Hall;
        return layout;
    }

    /// <summary>The script the hero is played by, the first argument naming one winning; none without.</summary>
    private HeroAutopilot? DevScript() =>
        Has("--poses") ? HeroAutopilot.PosesRun(Has("--host") ? 900 : 40)
        : Has("--ledges") && Has("--autopilot") ? HeroAutopilot.LedgesRun()
        : Has("--ladders") && Has("--autopilot") ? (Has("--strip") ? HeroAutopilot.StripLaddersRun() : HeroAutopilot.LaddersRun())
        : Has("--aside") ? HeroAutopilot.Aside()
        : Has("--jetpack") ? HeroAutopilot.Jetpack()
        : Has("--dust") ? HeroAutopilot.Dust()
        : Has("--feel") ? HeroAutopilot.Feel(Has("--hills"), Has("--strip"))
        : Has("--fly") && !Has("--manual") ? HeroAutopilot.Fly()
        : Has("--ground=spire-climb") && Has("--autopilot") ? HeroAutopilot.SpireClimb()
        : !Has("--autopilot") ? null
        : Has("--gym") ? HeroAutopilot.GymRun()
        : Has("--lone-hatch=open") ? HeroAutopilot.LoneHatchRun()
        : Has("--hills") ? HeroAutopilot.HillsRun(Dev.HillsDemo.HighPlateau)
        : Has("--strip") ? HeroAutopilot.PlanetRun()
        : Has("--ground=sprint") ? HeroAutopilot.Sprint(int.Parse(Arg("--sprint") ?? "300"))
        : HeroAutopilot.Wreck();

    /// <summary>The poses demo beside the hero, and where a dev run starts it, the first argument naming a place winning.</summary>
    private void PlaceHero(HeroView hero, HeroAutopilot? script)
    {
        if (Has("--poses")) AddChild(new Dev.PosesDemo(script!));
        // --edge=N: the hero starts N cells past the field's left line on the world's middle row, to see the cold (0050).
        if (_dev.Value("--edge") is { } edge)
            hero.Start = m => m.Grid.Height / 2 * m.Grid.Width + Math.Max(0, m.Grid.Zone.Left - Content.Field.Margin - int.Parse(edge));
        else if (Has("--fly") || Has("--asteroid")) hero.Start = FarDemo.StartOf;
        else if (Has("--sortie")) hero.Start = SortieDemo.StartOf;
        else if (Has("--charge")) hero.Start = Dev.ChargeDemo.StartOf;
        else if (Has("--arc")) hero.Start = m => Dev.OpenDemo.StartOf(m, "smelter");
        else if (Has("--bench") && Opens == null) hero.Start = m => Dev.OpenDemo.StartOf(m, BenchLook.Kind);
        else if (Has("--die")) hero.Start = Dev.DieDemo.StartOf;
        else if (Has("--breathe") || Has("--breathe-air")) hero.Start = Dev.BreatheDemo.StartOf;
        else if ((Opens ?? Near) is { } opened) hero.Start = m => Dev.OpenDemo.StartOf(m, opened);
    }

    /// <summary>The scripts that work the plan's hands, started once the hands are made.</summary>
    private void StartHandDemos()
    {
        if (Has("--plan-demo"))
        {
            _demo = new Dev.PlanDemo(_hands, _layout);
            _demo.Start();
        }
        if (Arg("--layer-demo") is { } layers)
        {
            _layerDemo = new Dev.LayerDemo(_hands, layers);
            _layerDemo.Start();
        }
        if (Has("--shimmer-demo"))
        {
            _shimmerDemo = new Dev.ShimmerDemo(_hands);
            _shimmerDemo.Start();
        }
    }

    /// <summary>The demos made last, over every panel and menu they work, and the measuring ones.</summary>
    private void AddDevDemos(GameMenu menu, SuitColours suits, HandbookPage debrisPage, EffectsView effects)
    {
        if (Has("--menu-demo") && _items != null) AddChild(new MenuDemo(_items, menu, suits));
        if (Has("--handbook")) AddChild(new Dev.HandbookDemo(debrisPage));
        if (Has("--jetpack") && _items != null) AddChild(new Dev.JetpackDemo(_items));
        if (Has("--charge") && _items != null) AddChild(new Dev.ChargeDemo(_items, _hands.Ui, () => _m));
        if (Has("--arc") && _items != null) AddChild(new Dev.ArcDemo(_items, _hands.Ui, () => _m));
        if (Has("--bench") && Opens == null && _items != null) AddChild(new Dev.BenchDemo(_items, _hands.Ui, () => _m, () => _fabricator));
        if (ChargerRace is { } race)
        {
            // Over the panels, which are on canvas layers of their own.
            var over = new CanvasLayer { Layer = 100 };
            over.AddChild(new Dev.ChargerArcDemo(() => _m, () => (_session as Session)?.Client.World.Tick, race == "arc", Arg("--shots")));
            AddChild(over);
        }
        if (Arg("--marks") is { } marked && Opens is { } marking && _items != null)
            AddChild(new Dev.MarksDemo(_items, marking, marked, int.Parse(Arg("--into") ?? "0"), () => _m));
        if ((Has("--breathe") || Has("--breathe-air")) && _items != null) AddChild(new Dev.BreatheDemo(_items, () => _m, canister: Has("--breathe")));
        if (Opens is { } open) AddChild(new Dev.OpenDemo(_hands.Ui, open, () => _m, Arg("--open-at") is { } at ? double.Parse(at, System.Globalization.CultureInfo.InvariantCulture) : 1.0));
        if (_dev.Value("--card") is { } card) AddChild(new Dev.CardDemo(_hands, card, pick: Has("--pick")));
        if (Has("--tool-cards")) AddChild(new Dev.ToolCardsDemo());
        if (Has("--asteroid") && !Has("--manual")) AddChild(new AsteroidDemo(_debris, _hands.Ui, () => _session is Session s ? (s.Client, _m) : null));
        if (Has("--sortie") && !Has("--manual")) AddChild(new SortieDemo(_debris, _hands.Ui, () => _session is Session s ? (s.Client, _m) : null));
        if (Has("--fly")) AddChild(new FarDemo(_debris, () => _session is Session s ? (s.Client, _m) : null));
        if (Has("--pod")) AddChild(new PodDemo(_hands.Ui, () => _session is Session s ? (s.Client, _m) : null, () => _fabricator));
        if (Has("--store")) AddChild(new Features.Power.StoreDemo(() => _session is Session s ? (s.Client, _m) : null));
        if (Has("--fx-bench")) AddChild(new Dev.FxBench());
        if (Has("--frames") || Has("--frames=vsync")) AddChild(_frameLog = new Dev.FrameLog(vsync: Has("--frames=vsync")));
        if (Has("--allocs")) _allocs = new Dev.AllocationSampler();
        if (_dev.Value("--sweep") is { } sweep)
            AddChild(new Dev.CameraSweep(_camera, () => _m != _ship ? new Vector2(_m.Crew.Spawn % _m.Grid.Width, _m.Crew.Spawn / _m.Grid.Width) : null, int.Parse(sweep)));
        if (Has("--measure"))
            AddChild(new Dev.PerfReadout(() => $"sync-worst={_worstSyncMs:F2}ms lamps={_light?.LampCount} lights={_light?.LightCount} effects={effects.TakeCostMs():F3}ms glims={Glims.Shared.Live.Count} cave-lights={_caves?.Lights.Count} lava-lights={_lava?.Lights.Count} glints={_caves?.Glinting} caves={_caves?.TakeCostMs():F3}ms home-lights={_homes?.Lights.Count} homes={_homes?.TakeCostMs():F3}ms spire-lights={_spires?.Lights.Count} spires={_spires?.TakeCostMs():F3}ms"));
    }

    private static (int, int) Size(string? text, (int, int) or) => text?.Split('x') is [var w, var h] ? (int.Parse(w), int.Parse(h)) : or;

    /// <summary>The reference world, at <c>--size</c> and built over <c>--built</c> of it.</summary>
    private Layout PerfLayout()
    {
        var (w, h) = Size(Arg("--size"), (256, 128));
        var (bw, bh) = Size(Arg("--built"), (w, h));
        return Dev.DevWorlds.Perf(w, h, bw, bh);
    }

    /// <summary>A dev run on its own: the world its arguments make, seeded by every demo they name, played and kept nowhere.</summary>
    private void Alone()
    {
        var modules = Modules();
        var layout = Has("--perf") ? PerfLayout() : _layout;
        var world = Has("--gallery") ? Dev.DevWorlds.Gallery(modules)
            : Has("--furnace") ? FurnaceDemo.Create(modules)
            : Has("--store") ? Features.Power.StoreWorld.Create(modules)
            : Has("--testbed") || Has("--jetpack") || Has("--charge") || Has("--dust") ? Dev.Testbed.Create(modules)
            : World.Create(1, layout, modules);
        if (Has("--items") || Has("--menu-demo")) world = ItemsDemo.Seed(world, Modules);
        if (Has("--debris")) world = DebrisDemo.Seed(world, Modules);
        if (Has("--wreck") || Has("--wrecks") || Has("--harpoon")) world = WreckDemo.Seed(world, Modules, all: Has("--wrecks"), harpoon: Has("--harpoon"));
        if (Has("--grapple")) world = WreckDemo.SeedGrapple(world, Modules);
        if (Has("--pod")) world = PodDemo.Seed(world, Modules);
        if (Has("--hills")) world = Dev.HillsDemo.Seed(world, Modules);
        if (Has("--floor")) world = Dev.FloorDemo.Seed(world, Modules);
        if (Has("--poses") || Arg("--layer-demo") != null) world = Dev.GroundDemo.Seed(world, Modules);
        if (Has("--jetpack")) world = Dev.JetpackDemo.Seed(world, Modules);
        if (Has("--charge")) world = Dev.ChargeDemo.Seed(world, Modules);
        if (Has("--arc")) world = Dev.ArcDemo.Seed(world, Modules);
        if (Has("--bench")) world = Dev.BenchDemo.Seed(world, Modules);
        if (ChargerRace is { } race) world = Dev.ChargerArcDemo.Seed(world, Modules, race == "arc");
        if (Has("--marks")) world = Dev.MarksDemo.Seed(world, Modules, Opens);
        if (Has("--asteroid")) world = AsteroidDemo.Seed(world, _m, Modules, ArcDrill);
        if (Has("--sortie")) world = SortieDemo.Seed(world, _m, Modules);
        if (Arg("--bow") is { } bow) FarDemo.Bow = int.Parse(bow);
        if (Has("--fly")) world = FarDemo.Seed(world, _m, Modules, int.Parse(_dev.Value("--far") ?? "0"));
        if (Has("--die")) world = Dev.DieDemo.Seed(world, Modules);
        if (Has("--breathe") || Has("--breathe-air")) world = Dev.BreatheDemo.Seed(world, Modules, suit: Has("--breathe") ? 8 : 6);
        if (Has("--handbook")) world = Dev.HandbookDemo.Seed(world, Modules);
        // --fauna=fluff (a species or its look): only that kind let out; --fauna-trace logs their strides a second.
        string? kind = _dev.Value("--fauna");
        bool fauna = Has("--fauna") || kind != null;
        if (Has("--fauna-trace") || Has("--fauna-trace=frames")) (_fauna.Trace, _fauna.TraceFrames) = (Log, Has("--fauna-trace=frames"));
        if (_dev.Value("--ground") is { } ground)
        {
            world = Dev.GroundDemo.Seed(world, Modules, ArcDrill);
            _ground = new Dev.GroundDemo(ground) { TakeAt = Arg("--take-at") is { } take ? (int)(double.Parse(take, System.Globalization.CultureInfo.InvariantCulture) * World.TicksPerSecond) : null, Offset = int.Parse(Arg("--ground-offset") ?? "0") };
            if (ground is "drill" or "poses") AddChild(new Dev.GroundHands(_ground) { Precise = Has("--precise") });
            // With --arc-drill the fitted drill is the first slot and the axe the third (GroundDemo.Seed).
            else if (Dev.GroundDemo.Hits(ground))
                AddChild(new Dev.GroundHands(_ground, slot: Has("--axe") ? (ArcDrill != null ? Key.Key3 : Key.Key1) : ArcDrill != null ? Key.Key1 : Key.Key2) { Precise = Has("--precise") });
            else if (Dev.GroundDemo.Caves(ground)) AddChild(new Dev.GroundHands(_ground, dig: false));
        }
        // --planet: the hero carries the axe and the drill and wears the helmet lamp, to try the caves (0077).
        else if (Has("--planet") || Has("--strip") && fauna) world = Dev.GroundDemo.Seed(world, Modules, ArcDrill);
        // --fauna, with --planet: creatures let out by the hero, hunted by script unless --manual (0102).
        if (fauna)
        {
            string? ram = _dev.Given("--fauna-ram") is { } r ? r.Length > 0 ? r : "hit" : null;
            _faunaDemo = new Dev.FaunaDemo(drill: Has("--fauna-drill"), course: Has("--fauna-course"), kind: kind, herd: Has("--fauna-herd"), ram: ram) { Kill = Has("--fauna-kill") };
            if (!Has("--manual")) AddChild(new Dev.FaunaHands(_faunaDemo, () => _hero?.Current, still: Has("--fauna-drill"), quick: Has("--fauna-course")));
        }
        PlayDev(world, Has("--gallery") ? null : Has("--store") ? Features.Power.StoreWorld.Map : layout);
    }

    /// <summary>The title's Testbed button, in a debug build: the testbed played as <c>--testbed</c> plays it.</summary>
    private void PlayTestbed()
    {
        if (_together.Mode is TogetherMode.Joining or TogetherMode.Opening) return;
        CloseTitle();
        PlayDev(Dev.Testbed.Create(Modules()), Dev.Testbed.Map);
    }

    /// <summary>Plays a dev world as a session of this machine's own, kept nowhere: no world id, so never saved.</summary>
    private void PlayDev(World world, Layout? aimAt)
    {
        _roster = Platform.Roster.Empty();
        var (host, client) = LocalSession.Create(world, Build, _roster.SeatOf(_playerKey));
        _session = new Session(host, client);
        // --planet: a planet opened and the hero beamed down to it at once (0077); --strip: the test strip (0076).
        if (Has("--planet") || Has("--strip"))
        {
            client.Send(new OpenSurface((Has("--strip") ? Content.Surfaces.TestStrip : Content.PlanetTypes.First).Id));
            client.Send(new Transfer(world.Surfaces.Count));
        }
        ShowWorld();
        AimCamera(aimAt);
    }

    /// <summary>--beam-back, with --planet or --strip: back aboard two seconds in, to see the switch both ways.</summary>
    private void BeamBack(Client client, int before)
    {
        if (Has("--beam-back") && before < 2 * World.TicksPerSecond && client.World.Tick >= 2 * World.TicksPerSecond) client.Send(new Transfer(0));
    }

    /// <summary>--snap FILE: the screen saved to FILE as a PNG once the world is --snap-at seconds old (6 by default), then quit.</summary>
    private void Snap(Client client)
    {
        if (Arg("--snap") is { } snap && client.World.Tick >= World.TicksPerSecond * int.Parse(Arg("--snap-at") ?? "6") && !_snapped)
        {
            _snapped = true;
            GetViewport().GetTexture().GetImage().SavePng(snap);
            Log($"snapped {snap}");
            Quit();
        }
    }

    /// <summary>Whether <c>--snap</c> has saved its picture.</summary>
    private bool _snapped;

    /// <summary>The commands the dev scripts send on a tick: doors, hatches and toggles for recordings.</summary>
    private void DevTick(Client client, int before)
    {
        if (Has("--auto")) Auto(client, before);
        if (Has("--hatches") && client.World.Tick != before)
            foreach (var c in Dev.Autopilot.Plans(before, client.World.Tick, _m.Grid.Width)) client.Send(c);
        if (Has("--doors") && client.World.Tick != before)
            foreach (var c in Dev.Autopilot.At(client.World.Tick, _m.Grid)) client.Send(c);
        // --toggle: the first doorway opened and shut on alternate ticks, so one is always in flight, as at a busy airlock.
        if (Has("--toggle") && client.World.Tick != before && (_toggled ??= Dev.Autopilot.At(20, _m.Grid).FirstOrDefault()) is SetDoor toggle)
            client.Send(toggle with { Open = client.World.Tick % 2 == 0 });
    }

    /// <summary>Each peer opens a door 30 ticks into its session, host and guest each their own; both say where they are.</summary>
    private void Auto(Client client, int before)
    {
        if (_doors.Count == 0)
            for (int cell = 0; cell < _m.Grid.Width * _m.Grid.Height; cell++)
                if (Doorways.IsDoor(_m.Grid.StructureAt(cell)) && Doorways.IsClosed(_m.Grid.StructureAt(cell))) _doors.Add(cell);
        int at = _start + 30;
        if (before < at && client.World.Tick >= at && _doors.Count > 1)
        {
            client.Send(new SetDoor(_doors[_role == "guest" ? 1 : 0], true));
            Log($"sent SetDoor({_doors[_role == "guest" ? 1 : 0]}) at tick {client.World.Tick}, {client.InFlight.Count} in flight");
        }
        if (client.World.Tick / 20 != before / 20) client.SendHero($"{{\"tick\":{client.World.Tick}}}");
    }
}
