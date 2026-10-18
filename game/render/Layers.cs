namespace Stillwake.Game.Render;

/// <summary>
/// Where each thing in the world is drawn, back to front, as the TypeScript game stacked its feature
/// layers (features/index.ts's <c>FEATURES</c>, then the overlays): every view sets its nodes'
/// <c>ZIndex</c> from here, its parents left at 0, so no order hangs on which node was added first.
/// The stars lie under all of it, on a canvas layer of their own (<see cref="Stars"/>).
/// </summary>
public static class Layers
{
    /// <summary>Debris drifting past behind the hull (debris).</summary>
    public const int Debris = -20;

    // What runs behind the ship's structure: back walls, pipes and their valves, cables (grid's and power's behind).
    public const int BackWalls = -10, Pipes = -6, Valves = -5, Cables = -4;

    /// <summary>The air in the rooms and the lifepod's reach, over the back walls, behind all that stands in them (atmosphere).</summary>
    public const int Air = -9;

    // The ship's structure and machines (grid), a socket on each powered machine.
    public const int Walls = 0, Edges = 1, Fixtures = 2, Doors = 3, Machines = 4, Sockets = 5;

    /// <summary>An analyzer's screen, on its face beside its socket (research).</summary>
    public const int Screens = Sockets;

    /// <summary>Grommets, pegs and plugs on the wall's face, then the bolts over machines short of power (power).</summary>
    public const int Fittings = 6, Bolts = 7;

    /// <summary>What a hero left where it died, a limp figure under its stacks (crew, 0061).</summary>
    public const int Bodies = 8;

    /// <summary>Stacks on the floor, under the heroes walking over them (items).</summary>
    public const int FloorItems = 9;

    /// <summary>Fire in a furnace's mouth, under a hero standing before it (furnace).</summary>
    public const int Fire = 10;

    // The crew: what E would work outlined, every tether, others' heroes, then this machine's.
    public const int Aim = 11, Tethers = 12, OtherHeroes = 13, Hero = 14;

    /// <summary>Drones flying with what they carry, over the heroes they pass (logistics, 0062).</summary>
    public const int Drones = 15;

    /// <summary>A lit lamp's glow, over what it lights (light).</summary>
    public const int Glows = 16;

    /// <summary>A venting room's pulse, then the tanks' gauges (atmosphere).</summary>
    public const int Pulse = 17, Gauges = 18;

    // The plan (building): blueprints, their marks and bars, the ghost of a drag, the hands at work.
    public const int Plans = 19, PlanMarks = 20, PlanGhost = 21, PlanHands = 22;

    /// <summary>Dust, sparks, embers and air over everything in the world, plans included (effects).</summary>
    public const int Effects = 23;

    // An overlay over the whole world (air, pipes, power), what it marks, and the cable dragged in it.
    public const int Overlay = 24, OverlayMarks = 25, OverlayHands = 26;
}

/// <summary>
/// The screen's canvas layers, back to front: every <c>CanvasLayer</c> takes its <c>Layer</c> from here,
/// so no panel added later draws over a tip. Words by the pointer have a layer of their own, over all
/// of it; within a layer, nodes draw in tree order.
/// </summary>
public static class ScreenLayers
{
    /// <summary>The stars, under the world (<see cref="Render.Stars"/>).</summary>
    public const int Stars = -1;

    /// <summary>The frost past the field, over the world and under all that is read (<c>Features.Crew.Frost</c>, 0050).</summary>
    public const int Frost = 1;

    /// <summary>The top of the screen: the suit's gauge, the hotbar, the inventory and crafts, pick-ups over a head.</summary>
    public const int Hud = 2;

    /// <summary>The bottom of the screen: the build menu, the prompt for E, the overlays' bar.</summary>
    public const int Bars = 10;

    /// <summary>The dev panel, then the game menu over it all.</summary>
    public const int Dev = 12, Menu = 20;

    /// <summary>
    /// Words by the pointer over every panel: an item's name, a card's tooltip, a cost, an overlay's
    /// words, a refusal, the stack carried; then a scripted run's drawn pointer.
    /// </summary>
    public const int Tips = 30, Pointer = 31;
}
