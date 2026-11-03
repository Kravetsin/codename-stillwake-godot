using System;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The crew's key on the tablet's edge (2026-11-03-the-regen-suit): the suit's mode, V's, a vest with a lamp on its chest, lit while it
/// works and dim with no body that has one. A press asks as V does (<see cref="Ui"/>), and the hero sends it. A canister is breathed by R
/// alone, with no key on the edge (2026-11-03-edge-lenses).
/// </summary>
public static class SuitKeys
{
    /// <param name="on">Whether this player's suit's mode works now (<c>SuitOn</c>).</param>
    /// <param name="why">Why it cannot be switched on: the sim's refusal; null while it can, or while it is on.</param>
    public static EdgeKey Mode(Ui ui, Func<bool> on, Func<string?> why) => new("suit", ui.SwitchSuit, on, () => on() ? null : why());
}
