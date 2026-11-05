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

    /// <summary>A trade station's capsule going off behind the hull (research, 2026-10-30-trade-capsule).</summary>
    public const int Capsules = -15;

    // What runs behind the ship's structure: back walls, pipes and their valves, cables (grid's and power's behind).
    public const int BackWalls = -11, Pipes = -6, Valves = -5, Cables = -4;

    /// <summary>The ground's soft shadow on its back walls (0088), multiplying them, under all that is drawn over them.</summary>
    public const int BackShade = -10;

    /// <summary>The air in the rooms and the lifepod's reach, over the back walls, behind all that stands in them (atmosphere).</summary>
    public const int Air = -9;

    /// <summary>A planned back wall and its outline, just over the air, under all that is built in front of it (building).</summary>
    public const int PlanBacks = -8;

    // The ship's structure and machines (grid), a socket on each powered machine.
    /// <summary>Dust along the floor (<c>Luminous.Low</c>, 0077): over the back walls, behind the walls, which hide it.</summary>
    public const int LowDust = -3;

    public const int Walls = 0, Edges = 1, Fixtures = 2, Doors = 3, Machines = 4, Sockets = 5;

    /// <summary>A planet's lava (grid): glowing over its own tiles and their edges, after the caves' tint, which leaves it its colour.</summary>
    public const int Lava = Fixtures;

    /// <summary>What grows on a planet (flora, 0079, 0080): over the ground and its moss, behind the heroes, its glows and glints too.</summary>
    public const int Flora = Fixtures;

    /// <summary>A screen or a lamp on a machine's face beside its socket: a station's (research), a tank's sight glass (atmosphere).</summary>
    public const int Screens = Sockets;

    /// <summary>
    /// A debris net's pieces over its booms (the machine's picture), its plates on the hull's face with them, then the net in front of
    /// them (debris).
    /// </summary>
    public const int NetHeld = Sockets, NetFront = Fittings;

    /// <summary>Grommets, pegs and plugs on the wall's face, then the bolts over machines short of power (power).</summary>
    public const int Fittings = 6, Bolts = 7;

    /// <summary>What a hero left where it died, a limp figure under its stacks (crew, 0061).</summary>
    public const int Bodies = 8;

    /// <summary>Stacks on the floor, under the heroes walking over them (items).</summary>
    public const int FloorItems = 9;

    /// <summary>Creatures (creatures, 0102): with the stacks, under the heroes and what they hold.</summary>
    public const int Creatures = FloorItems;

    /// <summary>Fire in a furnace's mouth, under a hero standing before it (furnace).</summary>
    public const int Fire = 10;

    /// <summary>
    /// Heat shimmer (0075), with the fire: one copy of the screen, then every shimmer, then the flames. After the
    /// world, before the aim's outline, the plans and every mark, which it must never copy.
    /// </summary>
    public const int Shimmer = Fire;

    // The plan (building): blueprints and their marks and bars, over the world and under the heroes, who stand before them (driver-order).
    public const int Plans = 11, PlanMarks = 12;

    // The crew: what E would work outlined, every tether, others' heroes, then this machine's.
    public const int Aim = 13, Tethers = 14, OtherHeroes = 15, Hero = 16;

    /// <summary>Drones flying with what they carry, over the heroes they pass (logistics, 0062).</summary>
    public const int Drones = 17;

    /// <summary>
    /// The ground a hero digs at (0077): its cracks and the aim's outline round it, over every hero and the tool in its hands,
    /// so a drill held close never hides what it bites.
    /// </summary>
    public const int Dug = 17;

    /// <summary>
    /// The tufts and chips of a planet's living edge laid before the heroes' feet (0082), thin enough that boots stand on the ground,
    /// not in it; with the cracks, which lie in the cell below.
    /// </summary>
    public const int GroundFront = 17;

    /// <summary>A lit lamp's glow, over what it lights (light).</summary>
    public const int Glows = 18;

    /// <summary>A venting room's pulse (atmosphere).</summary>
    public const int Pulse = 19;

    // The plan (building): the ghost of a drag and the hands at work, over the heroes.
    public const int PlanGhost = 21, PlanHands = 22;

    /// <summary>The power driver's square, over the pale ghosts of the plans it works, else lost under them (driver-square).</summary>
    public const int SquareAim = PlanMarks;

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

    /// <summary>A planet's fog at its sides (fog-edges), over the world, the heroes and the sky, under all that is read (<c>FogView</c>).</summary>
    public const int Fog = 1;

    /// <summary>The frost past the field, over the world and under all that is read (<c>Features.Crew.Frost</c>, 0050).</summary>
    public const int Frost = 2;

    /// <summary>A magnetic flare's white flash over the world, with the frost, under all that is read (<c>Features.Hazards.AuroraView</c>).</summary>
    public const int FlareFlash = Frost;

    /// <summary>A low suit's fogged visor (<c>Features.Crew.VisorFogView</c>), over the world and the frost it blurs, under all that is read (2026-11-05-the-last-breath).</summary>
    public const int VisorFog = 3;

    /// <summary>The top of the screen: the suit's gauge, the hotbar, the inventory and crafts, pick-ups over a head.</summary>
    public const int Hud = 4;

    /// <summary>The last breath's tunnel (<c>Features.Crew.TunnelView</c>), over the HUD it blurs, under the bars (2026-11-05-the-last-breath).</summary>
    public const int LastBreath = 5;

    /// <summary>The bottom of the screen: the build menu, the prompt for E, the overlays' bar.</summary>
    public const int Bars = 10;

    /// <summary>The Handbook's window (0066), over the HUD and the bars, under the dev panel and the menu.</summary>
    public const int Handbook = 11;

    /// <summary>The tablet's top edge and its keys (<c>Shared.TabletEdge</c>), over every tablet, whose top they ride on.</summary>
    public const int TabletEdge = 12;

    /// <summary>The dev panel, then the game menu over it all.</summary>
    public const int Dev = 13, Menu = 20;

    /// <summary>A drop pod's flight whiting out the screen (<c>Features.Landing.FlightOverlay</c>): over the HUD and the dev panel, under the menu.</summary>
    public const int Flight = 14;

    /// <summary>The title's scene, under its words and over all else, so nothing of the game behind shows (2026-10-25-title-scene).</summary>
    public const int TitleScene = 19;

    /// <summary>
    /// Words by the pointer over every panel: an item's name, a card's tooltip, a cost, an overlay's
    /// words, a refusal, the stack carried; then a scripted run's drawn pointer.
    /// </summary>
    public const int Tips = 30, Pointer = 31;
}
