using System;

namespace Stillwake.Game;

/// <summary>
/// How the salvage drone and its bay's fuel slot move (salvage-drones), the picks' numbers: the drone «Сетчатая рама»
/// (netter, <c>salvage_*.png</c>) and the bay «Хребет с кронштейнами» (wall, <c>droneBay_*.png</c>). A cosmetic of the
/// client, never read by the sim.
/// </summary>
public static class SalvageLook
{
    /// <summary>
    /// How a flying salvage drone follows the sim: no rotors, so it bobs a little only while the ship's air holds it up
    /// (its rig's spin stands for that air), and leans into its acceleration at most 0.06 radians; held as a drone is held
    /// over its cell, swinging what its net holds.
    /// </summary>
    public static readonly DroneLook.Motion Drift = new(Lift: 4, Clear: 2, BobAmp: 0.45, BobPeriod: 0.8, TiltMax: 0.06 * 180 / Math.PI, TiltCruise: 0, SpringHz: 3.2, Damping: 0.5, Swing: 20);

    /// <summary>How often a firing nozzle puffs, and how long a puff lives in the ship's air and in vacuum, seconds.</summary>
    public const double PuffEveryS = 0.085, LifeInS = 0.62, LifeOutS = 0.4;

    /// <summary>The least acceleration, pixels a second squared, that fires a group of nozzles.</summary>
    public const double Fire = 6;

    /// <summary>
    /// How far a puff blows out of its nozzle in air and in vacuum, pixels, by its nozzle's size; past the mouth, where it is
    /// born; how much of the drone's speed it keeps; how much smaller it is while the drone is low, and for the weaker puff
    /// that holds it up in air.
    /// </summary>
    public const double ReachIn = 4, ReachOut = 6.5, Mouth = 0.7, Carry = 0.6, LowShrink = 0.75, HoldShrink = 0.8;

    /// <summary>A puff's size, a share of its picture: at birth, and how much it grows over its life in air and in vacuum; how faint at birth.</summary>
    public const double PuffBorn = 0.32, GrowIn = 0.55, GrowOut = 0.42, PuffAlpha = 0.85;

    /// <summary>How long the net is half shut once it takes a load, seconds, before it closes round it.</summary>
    public const double GripMidS = 0.2;

    /// <summary>The fuel slot: feeding (valve open, vane turning, lamp blinking green) this long after a canister is spent, each step of its vane this long; a canister sliding into the breech this long.</summary>
    public const double FeedS = 1, FlowStepS = 0.3, CanisterInS = 0.4;

    /// <summary>A salvage drone's lamp low, red as its picture's, its light; the drone's own lamp lights are <see cref="DroneLook"/>'s.</summary>
    public const string Low = "ff5040";
}
