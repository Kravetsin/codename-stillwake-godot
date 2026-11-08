using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Furnace;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Menu;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

// Machines and items worked by script, mostly on the testbed (--testbed). --open and --near start the hero last, after any
// other scenario's start: --charge's or --arc's wins over theirs.

[Dev("--furnace", "a lit furnace in the wreck")]
public sealed class FurnaceScenario : DevScenario
{
    public override World? Create(IReadOnlyList<Module> modules) => FurnaceDemo.Create(modules);
}

/// <summary>The items room (<see cref="ItemsDemo"/>): a crate and a furnace.</summary>
[Dev("--items", "a room with a crate and a furnace, and a script working them (ItemsDemo)")]
public class ItemsScenario : DevScenario
{
    public override Layout? Map => ItemsDemo.Map;

    public override World Seed(World world, DevSeed seed) => ItemsDemo.Seed(world, seed.Modules);

    public override void Attach(DevContext c)
    {
        // --manual: the room alone, in creative mode, for a hand to play (the stash's crate already holding a canister).
        if (c.Items == null || Args.Has("--manual")) return;
        var pointer = new CanvasLayer { Layer = ScreenLayers.Pointer };
        pointer.AddChild(new ItemsDemo(c.Items));
        c.Add(pointer);
    }
}

[Dev("--menu-demo", "the items room's inventory and the game menu (MenuDemo)")]
public sealed class MenuScenario : ItemsScenario
{
    // With --items too, the room is seeded once, by its scenario.
    public override World Seed(World world, DevSeed seed) => Args.Has("--items") ? world : base.Seed(world, seed);

    public override void Attach(DevContext c)
    {
        if (c.Items != null) c.Add(new MenuDemo(c.Items, c.Menu, c.Suits));
    }
}

[Dev("--open", "with --testbed, the hero opens the machine of that kind (crate@1 the second) (OpenDemo)", Shape = DevShape.Equals, Order = 20)]
public sealed class OpenScenario : DevScenario
{
    private string Kind => Args.Value("--open")!;

    public override Func<Registry, int>? Start => m => OpenDemo.StartOf(m, Kind);

    public override void Attach(DevContext c) =>
        c.Add(new OpenDemo(c.Ui, Kind, c.M, Args.Arg("--open-at") is { } at ? double.Parse(at, CultureInfo.InvariantCulture) : 1.0, Args.Value("--program")));

    /// <summary>On the radar, two contacts found at once, where places draws them, so a recording shows them, the first selected.</summary>
    public override void Started(Client client)
    {
        if (Args.Value("--program") != Content.Programs.Radar) return;
        client.Send(new Sim.Places.Find("shuttle"));
        client.Send(new Sim.Places.Find("freighter"));
    }
}

[Dev("--near", "with --testbed, the hero starts beside the machine of that kind, opening nothing", Shape = DevShape.Equals, Order = 20)]
public sealed class NearScenario : DevScenario
{
    public override Func<Registry, int>? Start => m => OpenDemo.StartOf(m, Args.Value("--near")!);
}

[Dev("--marks", "with --open, that machine emptied and the item carried over its slot (MarksDemo)", Shape = DevShape.Next)]
public sealed class MarksScenario : DevScenario
{
    private string? Opens => Args.Value("--open");

    public override World Seed(World world, DevSeed seed) => MarksDemo.Seed(world, seed.Modules, Opens);

    public override void Attach(DevContext c)
    {
        if (Opens is { } kind && c.Items != null)
            c.Add(new MarksDemo(c.Items, kind, Args.Arg("--marks")!, int.Parse(Args.Arg("--into") ?? "0"), c.M));
    }
}

[Dev("--card", "with --testbed, the plan out on that build card, its tooltip shown (CardDemo)", Shape = DevShape.Equals)]
public sealed class CardScenario : DevScenario
{
    public override void Attach(DevContext c) => c.Add(new CardDemo(c.Hands, Args.Value("--card")!, pick: Args.Has("--pick")));
}

[Dev("--hints", "with --testbed, the plan's hints in turn: nothing in hand, wiring, piping, the valve, a plan under the pointer, a machine (HintsDemo)")]
public sealed class HintsScenario : DevScenario
{
    public override void Attach(DevContext c) => c.Add(new HintsDemo(c.Hands, c.M, command => c.World()?.Client.Send(command)));
}

[Dev("--tip", "with --testbed, the pointer held where a tip speaks: air (the overlay on, an aired room) or a machine kind (TipDemo)", Shape = DevShape.Equals)]
public sealed class TipScenario : DevScenario
{
    public override void Attach(DevContext c) => c.Add(new TipDemo(c.Hands, c.M, Args.Value("--tip")!));
}

[Dev("--charger", "arc or plain: the testbed's charging station filling an empty accumulator, fitted or not (ChargerArcDemo)", Shape = DevShape.Equals)]
public sealed class ChargerScenario : DevScenario
{
    private bool Arc => Args.Value("--charger") == "arc";

    public override World Seed(World world, DevSeed seed) => ChargerArcDemo.Seed(world, seed.Modules, Arc);

    public override void Attach(DevContext c)
    {
        // Over the panels, which are on canvas layers of their own.
        var over = new CanvasLayer { Layer = 100 };
        over.AddChild(new ChargerArcDemo(c.M, () => c.World()?.Client.World.Tick, Arc, Args.Arg("--shots")));
        c.Add(over);
    }
}

[Dev("--arc", "an arc smelter worked (ArcDemo)")]
public sealed class ArcScenario : DevScenario
{
    public override World Seed(World world, DevSeed seed) => ArcDemo.Seed(world, seed.Modules);

    public override Func<Registry, int>? Start => m => OpenDemo.StartOf(m, "smelter");

    public override void Attach(DevContext c)
    {
        if (c.Items != null) c.Add(new ArcDemo(c.Items, c.Ui, c.M));
    }
}

[Dev("--bench", "the arc bench fitting a tool (BenchDemo)")]
public sealed class BenchScenario : DevScenario
{
    // With --open the bench is only seeded: the hero opens what --open names.
    private bool Scripted => Args.Value("--open") == null;

    public override World Seed(World world, DevSeed seed) => BenchDemo.Seed(world, seed.Modules);

    public override Func<Registry, int>? Start => Scripted ? m => OpenDemo.StartOf(m, BenchLook.Kind) : null;

    public override void Attach(DevContext c)
    {
        if (Scripted && c.Items != null) c.Add(new BenchDemo(c.Items, c.Ui, c.M, () => c.Fabricator));
    }
}

[Dev("--pod", "the wreck's lifepod opened and crafting (PodDemo)")]
public sealed class PodScenario : DevScenario
{
    public override World Seed(World world, DevSeed seed) => PodDemo.Seed(world, seed.Modules);

    public override void Attach(DevContext c) => c.Add(new PodDemo(c.Ui, c.World, () => c.Fabricator));
}

[Dev("--handbook", "the Handbook opened on a world that has found ten kinds (HandbookDemo)")]
public sealed class HandbookScenario : DevScenario
{
    public override World Seed(World world, DevSeed seed) => HandbookDemo.Seed(world, seed.Modules);

    public override void Attach(DevContext c) => c.Add(new HandbookDemo(c.DebrisPage));
}

[Dev("--tree", "the black market on the tablet, in the wreck with credits to spend (TreeDemo)")]
public sealed class TreeScenario : DevScenario
{
    public override World Seed(World world, DevSeed seed) => TreeDemo.Seed(world, seed.Modules);

    public override void Attach(DevContext c) => c.Add(new TreeDemo(c.M));
}

[Dev("--board", "the board on the tablet, blank or with =first, =near, =burst, =circuit, =late, =bought or =search seeded (BoardDemo); with --manual, played by hand", Shape = DevShape.Either)]
public sealed class BoardScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed) => BoardDemo.Seed(world, seed.Modules, Args.Given("--board"));

    public override void Attach(DevContext c)
    {
        if (!Args.Has("--manual")) c.Add(new BoardDemo(c.M, Args.Given("--board")));
    }
}

[Dev("--flash", "the flash along the tablet's edge as notes start waiting: a find, three together flashing once, one more, again every 12 s (FlashDemo)")]
public sealed class FlashScenario : DevScenario
{
    public override World Seed(World world, DevSeed seed) => BoardDemo.Seed(world, seed.Modules, "late");

    public override void Attach(DevContext c) => c.Add(new FlashDemo(c.M));
}

[Dev("--tool-cards", "every hand tool's card, the drill's fitted too, the nailer's and the railgun's, every tool in a slot under them (ToolCardsDemo)")]
public sealed class ToolCardsScenario : DevScenario
{
    public override void Attach(DevContext c) => c.Add(new ToolCardsDemo());
}

[Dev("--salvage", "a bay's salvage drone flying out of an aired room for the pieces drifting in its zone, canisters in its fuel slot (SalvageDemo)")]
public sealed class SalvageScenario : DevScenario
{
    public override Layout? Map => Features.Logistics.SalvageDemo.Map;

    public override World? Create(IReadOnlyList<Module> modules) => Features.Logistics.SalvageDemo.Create(modules);

    private string _was = "";

    /// <summary>Logs the salvage drones out, the canisters left and the pieces out whenever they change.</summary>
    public override void Frame(Stillwake.Net.Client client, Registry m, Action<string> log)
    {
        var bay = m.Grid.MachinesOf(Stillwake.Content.Logistics.Bay).FirstOrDefault();
        if (bay == null) return;
        int fuel = m.Items.Crate(bay.Id)[Stillwake.Content.Machines.Get(bay.Kind).SlotTakes!.Value.Slot]?.Count ?? 0;
        string now = $"{m.Logistics.Drones.Count} out, {fuel} canisters, {m.Debris.Pieces.Count} pieces";
        if (now == _was) return;
        log($"salvage: tick {client.World.Tick}, {now}");
        _was = now;
    }
}
