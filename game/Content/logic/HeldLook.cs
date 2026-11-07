using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// How an item held in the front hand looks (0040), in pixels of its picture (<see cref="Picture"/>, +x
/// along the handle toward the head, +y down): the point the fist closes on (<see cref="Grip"/>), the
/// radians the handle leaves the fist at, turned back from the forearm (<see cref="Hold"/>), and the two
/// ends of its working edge (<see cref="EdgeFrom"/>, <see cref="EdgeTo"/>), which is what a swing of it
/// touches: no reach of its own, so a new grip, angle or picture moves the reach with it. A gun's edge is
/// its barrel, the rod seated in it, and the shot leaves <see cref="EdgeTo"/>, the muzzle, along it; its
/// <see cref="Loaded"/> picture, the same size up and left, shows the rod in the barrel while it is ready.
/// An <see cref="Aimed"/> item is pointed, not swung: the arm turns at the shoulder so its edge's line runs through
/// the pointer, the elbow bent by <see cref="Elbow"/> radians (2026-10-27-one-arm-holds). One that spins cycles its picture through <see cref="Spin"/>, each the same size, while it runs,
/// and stops on its picture; one with a single frame shows it steady while it runs, a light, not a turn; its <see cref="Lamp"/>, a pixel's middle, lights while it runs. On an arc bench (0099) its picture lies as held, the point
/// <see cref="Clamp"/> between the vise's jaws (<see cref="BenchLook.Clamp"/>): where a tool is clamped is its own, so any
/// tool sits on the bench with nothing drawn for it. A gun held to charge lays its <see cref="Charged"/> picture, the same size, over
/// it as it fills (2026-10-27-the-railgun), and the arc crackles over its own <see cref="Arcs"/> as a drill's coil does, more
/// as it fills. A builder works plans by a strip of three frames of its picture's size and grip: twisted by the wrist (<see cref="Twist"/>),
/// or, while its motor runs, turned by it (<see cref="Driven"/>). The cutter's beam hits what it touches, <see cref="Bite"/> pixels
/// either side of it (tool-edge); every other tool by its fan of rays from the shoulder (tool-reach).
/// </summary>
public sealed record HeldLook(
    string Picture, (double X, double Y) Grip, double Hold, (double X, double Y) EdgeFrom, (double X, double Y) EdgeTo, string? Loaded = null,
    bool Aimed = false, IReadOnlyList<string>? Spin = null, (double X, double Y)? Lamp = null, IReadOnlyList<ToolMount>? Mounts = null,
    double Elbow = 0, (double X, double Y)? Clamp = null, string? Charged = null,
    IReadOnlyList<(double X0, double Y0, double X1, double Y1)>? Arcs = null, string? Twist = null, string? Driven = null,
    double Bite = 0.75)
{
    /// <summary>
    /// The fire axe, <c>art/hero_axe.png</c>: gripped on the dark wrap a third up its haft; held square to
    /// the forearm, as a hammer is, the blade facing along it; the bright steel's outer line the edge.
    /// </summary>
    public static readonly HeldLook FireAxe = new("res://art/hero_axe.png", (7.5, 7), System.Math.PI / 2, (18, 13), (27, 13), Clamp: (10.5, 7));

    /// <summary>
    /// The spring harpoon, <c>art/hero_harpoon.png</c> (0044): gripped by its plank stock, held as a pistol
    /// is, the barrel along the forearm; the edge the rod seated in the tube, from its eye to the muzzle.
    /// </summary>
    public static readonly HeldLook Harpoon = new(
        "res://art/hero_harpoon.png", (4.5, 8), 0, (9, 3.5), (25, 3.5), Loaded: "res://art/hero_harpoon_loaded.png", Aimed: true, Clamp: (10.5, 4));

    /// <summary>
    /// The grappling gun, <c>art/hero_grapple.png</c> (grapple-climbs), the user's pick, «Кошка»: the harpoon's plank stock under a
    /// fatter spring tube with two hoops and a bell mouth, a gear pair and crank driving the spool; held as the harpoon is, the edge
    /// the grapnel's shank seated in the tube, to the bell. Loaded, the grapnel's folded crown shows out of the bell.
    /// </summary>
    public static readonly HeldLook Grapple = new(
        "res://art/hero_grapple.png", (4.5, 8), 0, (9, 3.5), (27.5, 3.5), Loaded: "res://art/hero_grapple_loaded.png", Aimed: true, Clamp: (10.5, 4));

    /// <summary>
    /// The nailer, <c>art/hero_nailer.png</c> (shots-are-the-sims), the user's pick, «Самоделка»: a pipe barrel bound with copper
    /// wire on a wooden grip, as the harpoon's, a brass spring and a tin-can magazine under it; held as a pistol is, one-handed,
    /// the barrel along the forearm; the edge the barrel, the nail leaving its muzzle. Its loaded picture shows the nail's point
    /// at the muzzle while it is ready.
    /// </summary>
    public static readonly HeldLook Nailer = new(
        "res://art/hero_nailer.png", (4.5, 7), 0, (7, 2.5), (25, 2.5), Loaded: "res://art/hero_nailer_loaded.png", Aimed: true, Clamp: (14, 2.5));

    /// <summary>
    /// The drill, <c>art/hero_drill.png</c> (0054): the tube hammer, a motor in a length of tube, held in one hand by the
    /// frame bent into a loop under its rear, the elbow a little bent, aimed at the pointer (2026-10-27-one-arm-holds); the edge its long bit, from the chuck to the point, which is what
    /// drills. Running, the bit turns through three frames and the lamp on its rear cap lights. Its one mount (0098), the
    /// coil wound round its stock: its turns from (4, 4), the box of the stock it is wound round. Clamped on the bench by the
    /// belly of its motor, the bit out past the jaws.
    /// </summary>
    public static readonly HeldLook Drill = new(
        "res://art/hero_drill.png", (3, 10.5), 0, (25, 5.5), (32, 5.5), Aimed: true,
        Spin: ["res://art/hero_drill_spin1.png", "res://art/hero_drill_spin2.png", "res://art/hero_drill_spin3.png"], Lamp: (1.5, 4.5),
        Mounts: [new ToolMount("coil", 4, 4, (3, 3.5, 11, 6.5))], Elbow: 0.4, Clamp: (17.5, 7));

    /// <summary>
    /// The screwdriver, <c>art/hero_screwdriver_held.png</c>, the first frame of the twist the hand works plans with
    /// (<c>hero_screwdriver.png</c>, drawn by the figure while it works): the fist round its ribbed handle, held square to
    /// the forearm as the axe is, the shaft's point the edge. No module fits it, so it never lies on a bench; its clamp the handle.
    /// </summary>
    public static readonly HeldLook Screwdriver = new("res://art/hero_screwdriver_held.png", (1.8, 1.2), System.Math.PI / 2, (7.8, 1.2), (9.6, 1.2), Clamp: (2.4, 1.2),
        Twist: "res://art/hero_screwdriver.png");

    /// <summary>
    /// The power driver, <c>art/hero_powerDriver_held.png</c> (power-driver), the user's pick, «Пистолет»: a short pistol-grip driver in
    /// worn green enamel, a battery slab under the grip with three charge lights, steady green; held as a pistol is, the barrel along the
    /// forearm, the hex bit the edge, to the screwdriver's fist. At work its motor turns the chuck and the bit, the lights bright
    /// (<c>hero_powerDriver.png</c>); flat, the wrist twists it as the screwdriver, the first light red (<c>hero_powerDriver_flat.png</c>).
    /// No module fits it; its clamp the barrel.
    /// </summary>
    public static readonly HeldLook PowerDriver = new("res://art/hero_powerDriver_held.png", (3.6, 4.56), 0, (8.16, 1.86), (9.6, 1.86), Clamp: (3.6, 1.8),
        Twist: "res://art/hero_powerDriver_flat.png", Driven: "res://art/hero_powerDriver.png");

    /// <summary>
    /// The railgun, <c>art/hero_railgun.png</c> (2026-10-27-the-railgun), the user's pick, «Конденсаторы»: a squat housing with a red
    /// band, three capacitor cans standing on it in copper bands, the arc shard at its rear, short heavy rails clamped twice; held as
    /// the drill is, in one hand by its pistol grip, the elbow a little bent; the edge the gap between
    /// the rails, the nail leaving their ends. Charged, the cans, the gap and the shard light (<c>hero_railgun_charged.png</c>), and
    /// the arc crackles along the whole barrel, a box each third of it.
    /// </summary>
    public static readonly HeldLook Railgun = new(
        "res://art/hero_railgun.png", (12, 10.5), 0, (17, 6.5), (38, 6.5), Aimed: true, Elbow: 0.4, Clamp: (21.5, 6.5),
        Charged: "res://art/hero_railgun_charged.png", Arcs: [(17, 4, 24, 8), (24, 4, 31, 8), (31, 4, 38, 8)]);

    /// <summary>
    /// The angle grinder, <c>art/hero_grinder.png</c> (salvage-tools), the user's pick, «Заводская»: a long motor tube in worn ochre
    /// enamel, a cast head, the wheel under a half guard; held as the drill is, by its pistol grip, aimed, the elbow a little bent; the
    /// edge the wheel's radius ahead, from its hub to its rim. Running, the disc turns through three frames and the lamp on its end cap
    /// lights. Its coil (0098) round the motor tube's vents, behind the switch. Clamped by the tube before its head.
    /// </summary>
    public static readonly HeldLook Grinder = new(
        "res://art/hero_grinder.png", (5.2, 9.8), 0, (22.6, 7.2), (27.2, 7.2), Aimed: true,
        Spin: ["res://art/hero_grinder_spin1.png", "res://art/hero_grinder_spin2.png", "res://art/hero_grinder_spin3.png"], Lamp: (1.5, 4.5),
        Mounts: [new ToolMount("coil", 2, 4, (2, 3.5, 9, 6.5))], Elbow: 0.4, Clamp: (14.5, 7));

    /// <summary>
    /// The plasma cutter, <c>art/hero_plasmaCutter.png</c> (salvage-tools), the user's pick, «Кольца»: a short dark body banded in brass,
    /// a copper nozzle, the jet's cage of three copper coils on a rail; held as the drill is, aimed, the elbow a little bent; the edge the
    /// jet's run through the coils, from the nozzle to the last. Running, the coils' inner faces, its lamp and charge lights are lit
    /// (<c>hero_plasmaCutter_on.png</c>). No module fits it; its clamp the rail where it leaves the body.
    /// </summary>
    public static readonly HeldLook PlasmaCutter = new(
        "res://art/hero_plasmaCutter.png", (4.6, 8), 0, (13.8, 4.4), (27.1, 4.4), Aimed: true,
        Spin: ["res://art/hero_plasmaCutter_on.png"], Elbow: 0.4, Clamp: (14, 8.3));

    /// <summary>
    /// The spear, <c>art/hero_spear.png</c> (salvage-tools), the user's pick, «Труба и лист»: a hull plate's leaf driven into a flattened
    /// tube, bound in copper, its grip in tape; held along the forearm, aimed at rest with the elbow bent (the jab's rest), thrust; the edge the leaf's point. Its coil (0098) on the sleeve
    /// riveted round the shaft. Clamped by the bare shaft before the sleeve.
    /// </summary>
    public static readonly HeldLook Spear = new(
        "res://art/hero_spear.png", (10.5, 5), 0, (42, 5), (47.8, 5), Aimed: true, Elbow: 1.2, Mounts: [new ToolMount("coil", 23, 4, (22, 3.5, 30, 6.5))], Clamp: (17.5, 5));

    /// <summary>
    /// The sledgehammer, <c>art/hero_sledgehammer.png</c> (salvage-tools), the user's pick, «Пакет плит»: five cut plates bolted into a
    /// block on a plank, a tube ferrule where they meet; gripped on the taped plank, held square to the forearm as the axe is; the edge
    /// the block's striking face along its bottom. Clamped by the plank.
    /// </summary>
    public static readonly HeldLook Sledgehammer = new(
        "res://art/hero_sledgehammer.png", (6.6, 8), System.Math.PI / 2, (21.4, 15), (31.4, 15), Clamp: (14.5, 8));

    /// <summary>
    /// The pickaxe, <c>art/hero_pickaxe.png</c> (the-pickaxe), the user's pick, «Шип из шпангоута»: a ship's rib in old primer, its lightening
    /// hole kept, torch-cut into a spike and welded to a pipe haft gripped in rubber; held square to the forearm as the axe is; the edge
    /// the spike's point. Clamped by the pipe, its spike hanging past the table.
    /// </summary>
    public static readonly HeldLook Pickaxe = new("res://art/hero_pickaxe.png", (5.5, 9), System.Math.PI / 2, (28.1, 17.3), (29.1, 17.3), Clamp: (14, 10));

    /// <summary>How long an item that spins takes to spin up to full speed and to wind down, in milliseconds: its picture and its motor's sound alike.</summary>
    public const double SpinUpMs = 120, SpinDownMs = 700;

    /// <summary>How many of its frames a spinning picture goes through a second at full speed, the picture itself among them.</summary>
    public const double SpinFps = 18;

    /// <summary>The clear pixels round a tool's icon, its held picture turned and fitted (2026-10-27-tool-icons), at any side.</summary>
    public const int IconMargin = 2;

    /// <summary>The colour of a running item's <see cref="Lamp"/>, "rrggbb": in the hand.</summary>
    public const string LampColour = "9dff8a";

    /// <summary>How <paramref name="item"/> looks in the hand, plain: its modules are laid over it (<see cref="HeldParts"/>, 0098); none for an item held as no picture.</summary>
    public static HeldLook? Of(string? item) =>
        item == Content.Weapons.Axe.Item ? FireAxe : item == Content.Harpoon.Item ? Harpoon : item == Content.Grapple.Item ? Grapple : item == Content.Weapons.Drill.Item ? Drill
        : item == Content.Weapons.Grinder.Item ? Grinder : item == Content.Weapons.PlasmaCutter.Item ? PlasmaCutter
        : item == Content.Weapons.Spear.Item ? Spear : item == Content.Weapons.Sledgehammer.Item ? Sledgehammer
        : item == Content.Weapons.Pickaxe.Item ? Pickaxe
        : item == Content.Shots.Nailer.Item ? Nailer : item == Content.Shots.Railgun.Item ? Railgun
        : item == Content.Screwdrivers.PowerDriver.Item ? PowerDriver : Content.Screwdrivers.Of(item) != null ? Screwdriver : null;

    /// <summary>Whether <paramref name="item"/> is run as the drill is, a module in it or not: a weapon drawing on the belt (the drill, grinder, cutter), aimed, hitting what its edge touches.</summary>
    public static bool IsDrill(string? item) => Content.Weapons.Of(item) is { } weapon && Content.Items.Get(weapon.Item).Draw != null;

    /// <summary>Whether <paramref name="item"/>, running, has its motor turn it at its work (<see cref="Driven"/>): the power driver.</summary>
    public static bool Drives(string? item) => Of(item)?.Driven != null;
}

/// <summary>
/// A gun's rod as it flies, its point along +x: its picture, where the line ties on (<see cref="Eye"/>) and its point
/// (<see cref="Tip"/>), in pixels of its picture.
/// </summary>
public sealed record RodLook(string Picture, (double X, double Y) Eye, (double X, double Y) Tip)
{
    /// <summary>The harpoon's rod, <c>art/hero_harpoon_rod.png</c> (0044).</summary>
    public static readonly RodLook Harpoon = new("res://art/hero_harpoon_rod.png", (3, 2.5), (23, 2.5));

    /// <summary>The grappling gun's grapnel, <c>art/hero_grapple_hook.png</c> (grapple-climbs): three prongs sprung open, swept back.</summary>
    public static readonly RodLook Grapple = new("res://art/hero_grapple_hook.png", (2.2, 5.5), (15.9, 5.5));

    /// <summary>The rod of the gun <paramref name="item"/>: the grappling gun's, else the harpoon's.</summary>
    public static RodLook Of(string? item) => item == Content.Grapple.Item ? Grapple : Harpoon;
}
