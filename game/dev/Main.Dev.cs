// Main's dev arguments (DevModes): the scenarios they start (game/dev/demos/), and the tools that are not scenarios —
// measuring, the view, networking — with what they do each frame.
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Debris;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Menu;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game;

public partial class Main
{
    /// <summary>The arguments after <c>--</c> (<see cref="Dev.DevModes"/>, and the scenarios' own).</summary>
    private Dev.DevArgs _dev = new([]);

    /// <summary>The scenarios the arguments start (<see cref="Dev.DevScenarios"/>), in their order.</summary>
    private List<Dev.DevScenario> _scenarios = [];

    private bool Has(string flag) => _dev.Has(flag);

    private string? Arg(string flag) => _dev.Arg(flag);

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

    /// <summary>The map a dev run plays: the first scenario's that gives one, else the wreck, alone or in the big world.</summary>
    private Layout DevLayout(Layout layout) =>
        _scenarios.Select(s => s.Map).FirstOrDefault(m => m != null)
        ?? (Has("--hatches") ? Dev.Autopilot.Hatches : Has("--world") ? Registry.Wreck : layout);

    /// <summary>The script the hero is played by: the first scenario's that gives one, else the wreck's with <c>--autopilot</c>.</summary>
    private HeroAutopilot? DevScript() =>
        _scenarios.Select(s => s.Script).FirstOrDefault(s => s != null)
        ?? (Has("--aside") ? HeroAutopilot.Aside() : Has("--autopilot") ? HeroAutopilot.Wreck() : null);

    /// <summary>Where a dev run starts the hero: <c>--edge</c>, else the first scenario's start; the spawn with neither.</summary>
    private void PlaceHero(HeroView hero)
    {
        // --edge=N: the hero starts N cells past the field's left line on the world's middle row, to see the cold (0050).
        if (_dev.Value("--edge") is { } edge)
            hero.Start = m => m.Grid.Height / 2 * m.Grid.Width + Math.Max(0, m.Grid.Zone.Left - Content.Field.Margin - int.Parse(edge));
        else if (_scenarios.Select(s => s.Start).FirstOrDefault(s => s != null) is { } start) hero.Start = start;
    }

    /// <summary>The scenarios' nodes, made last, over every panel and menu they work, and the measuring tools.</summary>
    private void AddDevDemos(GameMenu menu, SuitColours suits, HandbookPage debrisPage, EffectsView effects)
    {
        var context = new Dev.DevContext
        {
            Ui = _hands.Ui,
            Hands = _hands,
            Debris = _debris,
            Items = _items,
            Fabricator = _fabricator,
            Menu = menu,
            Suits = suits,
            DebrisPage = debrisPage,
            Layout = _layout,
            Hero = () => _hero?.Current,
            World = () => _session is Session s ? (s.Client, _m) : null,
            M = () => _m,
            Add = node => AddChild(node),
        };
        foreach (var scenario in _scenarios) scenario.Attach(context);
        if (Has("--fx-bench")) AddChild(new Dev.FxBench());
        if (Has("--frames") || Has("--frames=vsync")) AddChild(_frameLog = new Dev.FrameLog(vsync: Has("--frames=vsync")));
        if (Has("--allocs")) _allocs = new Dev.AllocationSampler();
        if (_dev.Value("--sweep") is { } sweep)
            AddChild(new Dev.CameraSweep(_camera, () => _m != _ship ? new Vector2(_m.Crew.Spawn % _m.Grid.Width, _m.Crew.Spawn / _m.Grid.Width) : null, int.Parse(sweep)));
        if (Has("--measure"))
            AddChild(new Dev.PerfReadout(() =>
            {
                var (caves, lava, homes, spires) = (View<Features.Flora.CaveView>(), View<Features.Grid.LavaView>(), View<Features.Ruins.HomesView>(), View<Features.Ruins.SpiresView>());
                return $"sync-worst={_worstSyncMs:F2}ms lamps={_light?.LampCount} lights={_light?.LightCount} effects={effects.TakeCostMs():F3}ms glims={Glims.Shared.Live.Count} cave-lights={caves?.Lights.Count} lava-lights={lava?.Lights.Count} glints={caves?.Glinting} caves={caves?.TakeCostMs():F3}ms home-lights={homes?.Lights.Count} homes={homes?.TakeCostMs():F3}ms spire-lights={spires?.Lights.Count} spires={spires?.TakeCostMs():F3}ms";
            }));
    }

    private static (int, int) Size(string? text, (int, int) or) => text?.Split('x') is [var w, var h] ? (int.Parse(w), int.Parse(h)) : or;

    /// <summary>The reference world, at <c>--size</c> and built over <c>--built</c> of it.</summary>
    private Layout PerfLayout()
    {
        var (w, h) = Size(Arg("--size"), (256, 128));
        var (bw, bh) = Size(Arg("--built"), (w, h));
        return Dev.DevWorlds.Perf(w, h, bw, bh);
    }

    /// <summary>A dev run on its own: the world its scenarios make and seed, played and kept nowhere.</summary>
    private void Alone()
    {
        var modules = Modules();
        var layout = Has("--perf") ? PerfLayout() : _layout;
        var world = _scenarios.Select(s => s.Create(modules)).FirstOrDefault(w => w != null)
            ?? (Has("--gallery") ? Dev.DevWorlds.Gallery(modules) : World.Create(1, layout, modules));
        foreach (var scenario in _scenarios) world = scenario.Seed(world, new Dev.DevSeed(Modules, _m));
        // --creatures-trace logs the creatures' strides a second, =frames every frame.
        if (Has("--creatures-trace") || Has("--creatures-trace=frames")) (_creatures.Trace, _creatures.TraceFrames) = (Log, Has("--creatures-trace=frames"));
        PlayDev(world, Has("--gallery") ? null : layout);
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
        foreach (var scenario in _scenarios) scenario.Started(client);
        ShowWorld();
        AimCamera(aimAt);
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
        if (client.World.Tick / 20 != before / 20) client.SendHero([]);
    }
}
