using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Dev;

/// <summary>How a dev argument is written.</summary>
public enum DevShape
{
    /// <summary>Alone: <c>--testbed</c>.</summary>
    Flag,
    /// <summary>Its value the next argument: <c>--zoom 3</c>.</summary>
    Next,
    /// <summary>Its value after <c>=</c>: <c>--card=door</c>.</summary>
    Equals,
    /// <summary>Alone or with a value after <c>=</c>: <c>--host</c>, <c>--host=name</c>.</summary>
    Either,
}

/// <summary>One argument the game takes after <c>--</c>: its name, how it is written and what it does.</summary>
public sealed record DevMode(string Name, DevShape Shape, string Doc);

/// <summary>
/// Every argument the game takes after <c>--</c> (<c>godot --path game -- --testbed</c>), one entry each with what it does.
/// With none the title shows; with any, the game plays on its own straight away, on the wreck as drawn
/// (<c>Registry.WreckMap</c>) unless one says otherwise. Wired in <c>game/dev/Main.Dev.cs</c>, sessions in
/// <c>game/Main.Sessions.cs</c>; <c>grep -rn '"--name' game</c> finds where one is read.
/// </summary>
public static class DevModes
{
    public static readonly IReadOnlyList<DevMode> All =
    [
        // Playing together.
        new("--host", DevShape.Either, "hosts a session over ENet, or on the relay as =name with --relay"),
        new("--join", DevShape.Either, "joins a session: =address over ENet (127.0.0.1 bare), or =name on the relay"),
        new("--relay", DevShape.Either, "meets through the playtest server's relay, or =wss://… another"),
        new("--password", DevShape.Equals, "the relay's password; else STILLWAKE_PASSWORD"),
        new("--player", DevShape.Equals, "who plays on this machine (0032), instead of its profile's key"),
        new("--lag", DevShape.Equals, "a joiner's link this many ms late, on purpose"),
        new("--jitter", DevShape.Equals, "a joiner's link this many ms uneven, on purpose"),
        new("--cheat", DevShape.Flag, "a joiner's world differs, a door open there only, so the host must resync it"),
        new("--auto", DevShape.Flag, "each peer opens a door 30 ticks in and says where it is"),
        // Worlds.
        new("--world", DevShape.Flag, "a new world as the title makes one, the wreck in the big world's middle, kept nowhere"),
        new("--perf", DevShape.Flag, "the reference world, looked at whole"),
        new("--size", DevShape.Next, "with --perf, a world WxH"),
        new("--built", DevShape.Next, "with --perf, tiles over WxH of it from the top-left; all of it by default"),
        new("--lit", DevShape.Flag, "every lamp at full power, to measure the light"),
        new("--dark", DevShape.Flag, "every lamp out, to see what glows"),
        new("--gallery", DevShape.Flag, "every picture"),
        new("--testbed", DevShape.Flag, "a ready ship with every machine at work (Testbed)"),
        new("--furnace", DevShape.Flag, "a lit furnace in the wreck"),
        new("--store", DevShape.Flag, "a capacitor bank filling from a panel, then emptying into an arc smelter (StoreDemo)"),
        new("--items", DevShape.Flag, "a room with a crate, a furnace and an analyzer, and a script working them (ItemsDemo)"),
        new("--planet", DevShape.Flag, "a planet grown from its seed, the hero beamed down with the axe, the drill and the helmet lamp (0077)"),
        new("--strip", DevShape.Flag, "the test strip (0076); with --autopilot played by a script (HeroAutopilot.PlanetRun)"),
        new("--beam-back", DevShape.Flag, "with --planet or --strip, back aboard two seconds in"),
        new("--no-homes", DevShape.Flag, "a planet's homes and spires left undrawn, to measure what they cost"),
        new("--homes", DevShape.Next, "the homes' mood, lit or dark"),
        // The hero.
        new("--autopilot", DevShape.Flag, "plays the hero by a script, the map's own (HeroAutopilot)"),
        new("--manual", DevShape.Flag, "with a scripted demo, plays it by hand"),
        new("--aside", DevShape.Flag, "steps the hero off the spawn, so another peer's window sees it whole there"),
        new("--edge", DevShape.Equals, "the hero starts N cells past the field's left line on the middle row, to see the cold (0050)"),
        new("--gym", DevShape.Flag, "a room with a ladder for the autopilot"),
        new("--ladders", DevShape.Flag, "a tall ladder up through a hatch beside a laddered pit, or the strip's shaft with --strip (0092)"),
        new("--ledges", DevShape.Flag, "a ledge a jump steps up onto and one it falls short of (0092)"),
        new("--hills", DevShape.Flag, "ground of one-cell steps to walk (HillsDemo)"),
        new("--floor", DevShape.Flag, "stacks falling through a machine onto a dark floor and a step (FloorDemo)"),
        new("--poses", DevShape.Flag, "the hall walked by a script, axe and drill carried, for the figure's motion (PosesDemo, 0090)"),
        new("--feel", DevShape.Flag, "the hero's jumps and runs traced step by step (HeroAutopilot.Feel, 0089)"),
        new("--fly", DevShape.Flag, "the hero flown far out (FarDemo)"),
        new("--far", DevShape.Equals, "with --fly, N pieces of the far field out round the start at once, to measure the view"),
        new("--bow", DevShape.Next, "with --fly, the start N columns ahead of the bow, in the stream (FarDemo.Bow)"),
        new("--dust", DevShape.Flag, "the testbed's hero sprinting and jumping: its boots' dust and landings (HeroAutopilot.Dust, 0075)"),
        new("--jetpack", DevShape.Flag, "the testbed's hero with a jetpack put on, flown out of the airlock (JetpackDemo)"),
        new("--die", DevShape.Flag, "a quick death to see the body it leaves (DieDemo, 0061)"),
        new("--breathe", DevShape.Flag, "a canister breathed and the pod's air breathed again (BreatheDemo)"),
        new("--breathe-air", DevShape.Flag, "the pod's air breathed only (BreatheDemo)"),
        new("--pose-sheet", DevShape.Equals, "the hero's holds drawn alone and saved to that folder, nothing else made (PoseSheet)"),
        new("--pose-item", DevShape.Equals, "with --pose-sheet, the item held; the drill by default"),
        // Ship and doors.
        new("--doors", DevShape.Flag, "opens and shuts doors for recordings (Autopilot.At)"),
        new("--toggle", DevShape.Flag, "the first doorway opened and shut on alternate ticks, one always in flight"),
        new("--segments", DevShape.Flag, "a door and a hatch of segments"),
        new("--hatches", DevShape.Flag, "a built hatch over a ladder and plans of doorways"),
        new("--lone-hatch", DevShape.Either, "a hatch between two rooms with no ladder (Autopilot.LoneHatch); =open with it open"),
        // Machines and items.
        new("--open", DevShape.Equals, "with --testbed, the hero opens the machine of that kind (crate@1 the second) (OpenDemo)"),
        new("--open-at", DevShape.Next, "with --open, after that many seconds"),
        new("--near", DevShape.Equals, "with --testbed, the hero starts beside the machine of that kind, opening nothing"),
        new("--marks", DevShape.Next, "with --open, that machine emptied and the item carried over its slot (MarksDemo)"),
        new("--into", DevShape.Next, "with --marks, the slot"),
        new("--card", DevShape.Equals, "with --testbed, the plan out on that build card, its tooltip shown (CardDemo)"),
        new("--pick", DevShape.Flag, "with --card, the card in hand"),
        new("--charge", DevShape.Flag, "the testbed's charger filling a battery (ChargeDemo)"),
        new("--charger", DevShape.Equals, "arc or plain: the testbed's charging station filling an empty accumulator, fitted or not (ChargerArcDemo)"),
        new("--shots", DevShape.Next, "with --charger, the screen saved to that folder at a few moments, then quits"),
        new("--arc", DevShape.Flag, "an arc smelter worked (ArcDemo)"),
        new("--bench", DevShape.Flag, "the arc bench fitting a tool (BenchDemo)"),
        new("--pod", DevShape.Flag, "the wreck's lifepod opened and crafting (PodDemo)"),
        new("--menu-demo", DevShape.Flag, "the items room's inventory and the game menu (MenuDemo)"),
        new("--handbook", DevShape.Flag, "the Handbook opened on a world that has found ten kinds (HandbookDemo)"),
        new("--tool-cards", DevShape.Flag, "the drill's card plain and fitted side by side (ToolCardsDemo)"),
        new("--slots", DevShape.Equals, "the slots' size shown for a recording, keeping nothing (Slots.ScaleNamed)"),
        // The plan.
        new("--plan-demo", DevShape.Flag, "plays the plan, cables and overlays by a script (PlanDemo)"),
        new("--layer-demo", DevShape.Next, "plan|remove|fade|axe|aim|strip|pipe|valve: the plan's layers and hand tools by script (LayerDemo, 0107–0109)"),
        new("--shimmer-demo", DevShape.Flag, "with --testbed --near=furnace, aims at the furnace, plans, marks and welds over it (ShimmerDemo)"),
        // Debris and wrecks.
        new("--debris", DevShape.Flag, "the wreck with debris about, a crate caught with --autopilot (DebrisDemo)"),
        new("--wreck", DevShape.Flag, "a hull section chopped with the fire axe (WreckDemo)"),
        new("--wrecks", DevShape.Flag, "every kind of wreckage chopped in a row (WreckDemo)"),
        new("--harpoon", DevShape.Flag, "a crate and a section shot and reeled in (WreckDemo)"),
        new("--grapple", DevShape.Flag, "the hero pulled up to a hall's ceiling and its wall (WreckDemo)"),
        new("--asteroid", DevShape.Flag, "two asteroids drilled and chopped out in the band (AsteroidDemo)"),
        new("--sortie", DevShape.Flag, "the hero far out among one of each derelict and asteroid, an engine block drilled (SortieDemo)"),
        new("--arc-drill", DevShape.Flag, "with --asteroid, --planet or --ground, the drill in hand has the arc module fitted (0098)"),
        // The ground.
        new("--ground", DevShape.Equals, "a planet's ground looked at: acacia, hit-soil|-stone|-alloy|-tree|-crystal, band0…3, hall, cave, shaft, sprint, spire-climb, drill, poses (GroundDemo)"),
        new("--axe", DevShape.Flag, "with --ground=hit-…, the axe instead of the drill (0094)"),
        new("--precise", DevShape.Flag, "with --ground, Ctrl held: one cell struck, not the brush (GroundHands)"),
        new("--take-at", DevShape.Next, "with --ground at a spire, the second its nearest relic is struck from (0096)"),
        new("--ground-offset", DevShape.Next, "with --ground, the hero stood N columns right of its mode's spot"),
        new("--sprint", DevShape.Next, "with --ground=sprint --autopilot, cells run; 300 by default"),
        new("--sweep", DevShape.Equals, "the camera run along the row that deep under the spawn (CameraSweep)"),
        new("--fauna", DevShape.Either, "creatures of every kind let out by the hero, or =kind one kind, hunted by script (FaunaHands, 0102)"),
        new("--fauna-drill", DevShape.Flag, "with --fauna, one loper under the still hero's drill"),
        new("--fauna-course", DevShape.Flag, "with --strip --fauna, a loper struck to run the strip's course"),
        new("--fauna-herd", DevShape.Flag, "with --fauna, five a side of the hero: a herd"),
        new("--fauna-kill", DevShape.Flag, "with --fauna, every creature near killed a while after, for recording deaths"),
        new("--fauna-ram", DevShape.Either, "with --strip --fauna=kind, it rams the hero: =hit, dodge or strike; hit by default (0103)"),
        new("--fauna-trace", DevShape.Either, "logs the creatures' strides a second, =frames every frame"),
        // The camera and measuring.
        new("--zoom", DevShape.Next, "the camera held at that zoom"),
        new("--view", DevShape.Next, "X,Y[,ZOOM]: where the camera looks"),
        new("--snap", DevShape.Next, "the screen saved to that PNG once the world is --snap-at seconds old, then quits"),
        new("--snap-at", DevShape.Next, "with --snap, the seconds; 6 by default"),
        new("--for", DevShape.Equals, "quits after that many seconds"),
        new("--measure", DevShape.Flag, "prints what frames cost (PerfReadout)"),
        new("--frames", DevShape.Either, "logs every frame and prints the worst with their causes on quitting, =vsync with it (FrameLog)"),
        new("--allocs", DevShape.Flag, "with --frames, what was allocated, by type (AllocationSampler)"),
        new("--fx-bench", DevShape.Flag, "a busy scene of luminous effects round the view's middle to measure (FxBench)"),
    ];

    private static readonly Dictionary<string, DevMode> ByName = All.ToDictionary(m => m.Name, StringComparer.Ordinal);

    /// <summary>The argument named <paramref name="name"/>, without its value; null for none.</summary>
    public static DevMode? Find(string name) => ByName.GetValueOrDefault(name);
}
