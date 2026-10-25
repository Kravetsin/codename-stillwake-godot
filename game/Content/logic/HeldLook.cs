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
/// An <see cref="Aimed"/> item is pointed, not swung: the straight arm turns so its edge's line runs through
/// the pointer. One that spins cycles its picture through <see cref="Spin"/>, each the same size, while it runs,
/// and stops on its picture; its <see cref="Lamp"/>, a pixel's middle, lights while it runs. One held in
/// <see cref="Both"/> hands is held as a rifle is (<c>TwoHand</c>, 0078): <see cref="Grip"/> in the near hand, its
/// foregrip in the far one, <see cref="Hold"/> unused. On an arc bench (0099) its picture lies as held, the point
/// <see cref="Clamp"/> between the vise's jaws (<see cref="BenchLook.Clamp"/>): where a tool is clamped is its own, so any
/// tool sits on the bench with nothing drawn for it.
/// </summary>
public sealed record HeldLook(
    string Picture, (double X, double Y) Grip, double Hold, (double X, double Y) EdgeFrom, (double X, double Y) EdgeTo, string? Loaded = null,
    bool Aimed = false, IReadOnlyList<string>? Spin = null, (double X, double Y)? Lamp = null, IReadOnlyList<ToolMount>? Mounts = null,
    TwoHands? Both = null, (double X, double Y)? Clamp = null)
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
    /// The drill, <c>art/hero_drill.png</c> (0054): the tube hammer, a motor in a length of tube, held as a rifle is
    /// (0078), the near hand on the frame bent into a loop under its rear, the far one on the stub of a foregrip under
    /// the front of the motor, aimed at the pointer; the edge its long bit, from the chuck to the point, which is what
    /// drills. Running, the bit turns through three frames and the lamp on its rear cap lights. Its one mount (0098), the
    /// coil wound round its stock: its turns from (4, 4), the box of the stock it is wound round. Clamped on the bench by the
    /// belly of its motor, the bit out past the jaws.
    /// </summary>
    public static readonly HeldLook Drill = new(
        "res://art/hero_drill.png", (3, 10.5), 0, (25, 5.5), (32, 5.5), Aimed: true,
        Spin: ["res://art/hero_drill_spin1.png", "res://art/hero_drill_spin2.png", "res://art/hero_drill_spin3.png"], Lamp: (1.5, 4.5),
        Mounts: [new ToolMount("coil", 4, 4, (3, 3.5, 11, 6.5))], Both: new TwoHands((12, 10.5), (7.5, 10.5)), Clamp: (17.5, 7));

    /// <summary>
    /// The screwdriver, <c>art/hero_screwdriver_held.png</c>, the first frame of the twist the hand works plans with
    /// (<c>hero_screwdriver.png</c>, drawn by the figure while it works): the fist round its ribbed handle, held square to
    /// the forearm as the axe is, the shaft's point the edge. No module fits it, so it never lies on a bench; its clamp the handle.
    /// </summary>
    public static readonly HeldLook Screwdriver = new("res://art/hero_screwdriver_held.png", (3, 2), System.Math.PI / 2, (13, 2), (16, 2), Clamp: (4, 2));

    /// <summary>How long an item that spins takes to spin up to full speed and to wind down, in milliseconds: its picture and its motor's sound alike.</summary>
    public const double SpinUpMs = 120, SpinDownMs = 700;

    /// <summary>How many of its frames a spinning picture goes through a second at full speed, the picture itself among them.</summary>
    public const double SpinFps = 18;

    /// <summary>The colour of a running item's <see cref="Lamp"/>, "rrggbb": in the hand and on its card.</summary>
    public const string LampColour = "9dff8a";

    /// <summary>How <paramref name="item"/> looks in the hand, plain: its modules are laid over it (<see cref="HeldParts"/>, 0098); none for an item held as no picture.</summary>
    public static HeldLook? Of(string? item) =>
        item == Content.Weapons.Axe.Item ? FireAxe : item == Content.Harpoon.Item ? Harpoon : item == Content.Weapons.Drill.Item ? Drill
        : Content.Screwdrivers.Of(item) != null ? Screwdriver : null;

    /// <summary>Whether <paramref name="item"/> is the drill, a module in it or not: aimed, run on the belt, drilling what its bit touches.</summary>
    public static bool IsDrill(string? item) => item == Content.Weapons.Drill.Item;
}

/// <summary>
/// How an item held in both hands sits in them (0078), in pixels of its picture: where the far hand closes
/// (<see cref="Foregrip"/>), the near one closing on its <see cref="HeldLook.Grip"/>; and the pixel that rests on the
/// body's hold point and turns there as it aims (<see cref="Mount"/>), between the two hands.
/// </summary>
public sealed record TwoHands((double X, double Y) Foregrip, (double X, double Y) Mount);

/// <summary>
/// The harpoon's rod as it flies, <c>art/hero_harpoon_rod.png</c>, its point along +x: where the line ties
/// on (<see cref="Eye"/>) and its point (<see cref="Tip"/>), in pixels of its picture.
/// </summary>
public static class RodLook
{
    public const string Picture = "res://art/hero_harpoon_rod.png";
    public static readonly (double X, double Y) Eye = (3, 2.5), Tip = (23, 2.5);
}
