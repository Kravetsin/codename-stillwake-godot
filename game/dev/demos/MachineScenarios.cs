using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Features.Furnace;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Menu;
using Stillwake.Game.Render;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

// Machines and items worked by script, mostly on the testbed (--testbed). --open and --near start the hero last, after any
// other scenario's start: --charge's or --arc's wins over theirs.

[Dev("--furnace", "a lit furnace in the wreck")]
public sealed class FurnaceScenario : DevScenario
{
    public override World? Create(IReadOnlyList<Module> modules) => FurnaceDemo.Create(modules);
}

/// <summary>The items room (<see cref="ItemsDemo"/>): a crate, a furnace and an analyzer.</summary>
[Dev("--items", "a room with a crate, a furnace and an analyzer, and a script working them (ItemsDemo)")]
public class ItemsScenario : DevScenario
{
    public override Layout? Map => ItemsDemo.Map;

    public override World Seed(World world, DevSeed seed) => ItemsDemo.Seed(world, seed.Modules);

    public override void Attach(DevContext c)
    {
        if (c.Items == null) return;
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
        c.Add(new OpenDemo(c.Ui, Kind, c.M, Args.Arg("--open-at") is { } at ? double.Parse(at, CultureInfo.InvariantCulture) : 1.0));
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

[Dev("--tool-cards", "the drill's card plain and fitted side by side (ToolCardsDemo)")]
public sealed class ToolCardsScenario : DevScenario
{
    public override void Attach(DevContext c) => c.Add(new ToolCardsDemo());
}
