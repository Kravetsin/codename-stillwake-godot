using System;
using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The crew's keys on the tablet's edge (2026-11-03-the-regen-suit): the suit's mode, V's, lit while it works and dim with no
/// body that has one; and a canister breathed, R's, dim while none can be. A press asks as the key does (<see cref="Ui"/>),
/// and the hero sends it.
/// </summary>
public static class SuitKeys
{
    /// <summary>The suit's body in outline, its shoulders, sleeves and chest: any body with a mode.</summary>
    private static readonly Rect2[] Body =
    [
        new(4, 0, 2, 1), new(8, 0, 2, 1), new(2, 1, 4, 1), new(8, 1, 4, 1), new(1, 2, 2, 1), new(11, 2, 2, 1), new(0, 3, 2, 3),
        new(12, 3, 2, 3), new(3, 4, 2, 8), new(9, 4, 2, 8), new(3, 12, 8, 2),
    ];

    /// <summary>A canister standing: its valve, neck and band.</summary>
    private static readonly Rect2[] Bottle =
    [
        new(5, 0, 4, 1), new(6, 1, 2, 1), new(5, 2, 4, 1), new(4, 3, 6, 1), new(3, 4, 2, 2), new(9, 4, 2, 2), new(3, 6, 8, 1),
        new(3, 7, 2, 5), new(9, 7, 2, 5), new(3, 12, 8, 1), new(4, 13, 6, 1),
    ];

    /// <param name="on">Whether this player's suit's mode works now (<c>SuitOn</c>).</param>
    /// <param name="why">Why it cannot be switched on: the sim's refusal; null while it can, or while it is on.</param>
    public static EdgeKey Mode(Ui ui, Func<bool> on, Func<string?> why) => new(Body, ui.SwitchSuit, on, () => on() ? null : why());

    /// <param name="why">Why no canister can be breathed now: the sim's refusal (<c>RefillProblem</c>); null while one can.</param>
    public static EdgeKey Canister(Ui ui, Func<string?> why) => new(Bottle, ui.Breathe, () => false, why);
}
