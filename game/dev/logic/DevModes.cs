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
/// <c>game/Main.Sessions.cs</c>; <c>grep -rn '"--name' game</c> finds where one is read. A scenario's own are on its class
/// (<c>[Dev("--store", …)]</c> in <c>game/dev/demos/</c>), not here.
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
        new("--beam-back", DevShape.Flag, "with --planet or --strip, back aboard two seconds in"),
        new("--no-homes", DevShape.Flag, "a planet's homes and spires left undrawn, to measure what they cost"),
        new("--homes", DevShape.Next, "the homes' mood, lit or dark"),
        // The hero.
        new("--autopilot", DevShape.Flag, "plays the hero by a script, the map's own (HeroAutopilot)"),
        new("--manual", DevShape.Flag, "with a scripted demo, plays it by hand"),
        new("--aside", DevShape.Flag, "steps the hero off the spawn, so another peer's window sees it whole there"),
        new("--edge", DevShape.Equals, "the hero starts N cells past the field's left line on the middle row, to see the cold (0050)"),
        new("--far", DevShape.Equals, "with --fly, N pieces of the far field out round the start at once, to measure the view"),
        new("--bow", DevShape.Next, "with --fly, the start N columns ahead of the bow, in the stream (FarDemo.Bow)"),
        new("--pose-sheet", DevShape.Equals, "the hero's holds drawn alone and saved to that folder, nothing else made (PoseSheet)"),
        new("--pose-item", DevShape.Equals, "with --pose-sheet, the item held; the drill by default"),
        // Ship and doors.
        new("--doors", DevShape.Flag, "opens and shuts doors for recordings (Autopilot.At)"),
        new("--toggle", DevShape.Flag, "the first doorway opened and shut on alternate ticks, one always in flight"),
        new("--hatches", DevShape.Flag, "a built hatch over a ladder and plans of doorways"),
        // Machines and items.
        new("--open-at", DevShape.Next, "with --open, after that many seconds"),
        new("--program", DevShape.Equals, "with --open=computer, the program it shows once open (radar)"),
        new("--into", DevShape.Next, "with --marks, the slot"),
        new("--pick", DevShape.Flag, "with --card, the card in hand"),
        new("--shots", DevShape.Next, "with --charger, the screen saved to that folder at a few moments, then quits"),
        new("--slots", DevShape.Equals, "the slots' size shown for a recording, keeping nothing (Slots.ScaleNamed)"),
        // Debris, wrecks and the ground.
        new("--arc-drill", DevShape.Flag, "with --asteroid, --planet or --ground, the drill in hand has the arc module fitted (0098)"),
        new("--axe", DevShape.Flag, "with --ground=hit-…, the axe instead of the drill (0094)"),
        new("--take-at", DevShape.Next, "with --ground at a spire, the second its nearest relic is struck from (0096)"),
        new("--ground-offset", DevShape.Next, "with --ground, the hero stood N columns right of its mode's spot"),
        new("--sprint", DevShape.Next, "with --ground=sprint --autopilot, cells run; 300 by default"),
        new("--sweep", DevShape.Equals, "the camera run along the row that deep under the spawn (CameraSweep)"),
        new("--creatures-drill", DevShape.Flag, "with --creatures, one loper under the still hero's drill"),
        new("--creatures-course", DevShape.Flag, "with --strip --creatures, a loper struck to run the strip's course"),
        new("--creatures-herd", DevShape.Flag, "with --creatures, five a side of the hero: a herd"),
        new("--creatures-kill", DevShape.Flag, "with --creatures, every creature near killed a while after, for recording deaths"),
        new("--creatures-ram", DevShape.Either, "with --strip --creatures=kind, it rams the hero: =hit, dodge or strike; hit by default (0103)"),
        new("--creatures-trace", DevShape.Either, "logs the creatures' strides a second, =frames every frame"),
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
