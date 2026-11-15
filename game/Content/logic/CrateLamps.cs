using System;
using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// The contraband drift's crates' amber lamps (docs/decisions/2026-11-15-stretches-of-space.md, the pick's four crates), by the look a crate wears: where each
/// lamp sits from the picture's middle and how big it is, in the world's pixels, and when it is lit, as the pick blinked it.
/// </summary>
public static class CrateLamps
{
    /// <summary>A lamp at (<paramref name="X"/>, <paramref name="Y"/>) from the picture's middle, its lens <paramref name="R"/> across half.</summary>
    public readonly record struct Lamp(float X, float Y, float R);

    /// <summary>
    /// Each look's lamps: the case's one between its locks; the drum's three round its rim; the double bottom's in the black box behind
    /// its broken plank; the bundle's beacon on its strap. Each picture drawn in its own box and centred in the wreck's 24 px square.
    /// </summary>
    public static readonly IReadOnlyList<IReadOnlyList<Lamp>> Of =
    [
        [new(0, -2.9f, 1.05f)],
        [new(-4, -4.2f, 0.68f), new(0, -4.2f, 0.68f), new(4, -4.2f, 0.68f)],
        [new(0.4f, 0.4f, 0.95f)],
        [new(0.1f, 1, 0.9f)],
    ];

    /// <summary>
    /// Whether lamp <paramref name="lamp"/> of look <paramref name="look"/> is lit <paramref name="seconds"/> into its blinking: the case
    /// once in 1.6 s, lit 0.28 s; the drum's three running round, one a beat, a beat dark; the double bottom's double flash every 1.5 s;
    /// the bundle's short quick strobe.
    /// </summary>
    public static bool Lit(int look, int lamp, double seconds) => look switch
    {
        0 => seconds % 1.6 < 0.28,
        1 => (int)Math.Floor(seconds * 3.2 % 4) == lamp,
        2 => seconds % 1.5 is < 0.11 or (> 0.24 and < 0.35),
        _ => seconds % 0.5 < 0.06,
    };
}
