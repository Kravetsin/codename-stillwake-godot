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

    // The ship's structure and machines (grid), a socket on each powered machine.
    public const int Walls = 0, Edges = 1, Fixtures = 2, Doors = 3, Machines = 4, Sockets = 5;

    /// <summary>Grommets, pegs and plugs on the wall's face, then the bolts over machines short of power (power).</summary>
    public const int Fittings = 6, Bolts = 7;

    /// <summary>Stacks on the floor, under the heroes walking over them (items).</summary>
    public const int FloorItems = 8;

    /// <summary>Fire in a furnace's mouth, under a hero standing before it (furnace).</summary>
    public const int Fire = 9;

    // The crew: what E would work outlined, every tether, others' heroes, then this machine's.
    public const int Aim = 10, Tethers = 11, OtherHeroes = 12, Hero = 13;

    /// <summary>A lit lamp's glow, over what it lights (light).</summary>
    public const int Glows = 14;

    /// <summary>A venting room's pulse, then the tanks' gauges (atmosphere).</summary>
    public const int Pulse = 15, Gauges = 16;

    // The plan (building): blueprints, their marks and bars, the ghost of a drag, the hands at work.
    public const int Plans = 17, PlanMarks = 18, PlanGhost = 19, PlanHands = 20;

    /// <summary>Dust, sparks, embers and air over everything in the world, plans included (effects).</summary>
    public const int Effects = 21;

    // An overlay over the whole world (air, pipes, power), what it marks, and the cable dragged in it.
    public const int Overlay = 22, OverlayMarks = 23, OverlayHands = 24;
}
