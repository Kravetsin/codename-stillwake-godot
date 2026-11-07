using System;
using System.Collections.Generic;
using Stillwake.Game.Shared;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Features.Scanner;

/// <summary>A readout the scanner holds up (2026-11-07-the-scanner): over (<paramref name="X"/>, <paramref name="Y"/>), in cells, <paramref name="Left"/> of <paramref name="Max"/> hit points; read for thing <paramref name="Id"/>.</summary>
public readonly record struct Readout(int Id, double X, double Y, int Left, int Max);

/// <summary>
/// What the worn scanner reads (2026-11-07-the-scanner): the hit points left of the wreck under the pointer, held up at the
/// middle of its picture's top edge where it is drawn. Nothing while it is not worn, nor over a small piece or empty space.
/// </summary>
public static class Scan
{
    /// <summary>
    /// The readout over the wreck of <paramref name="pieces"/> under <paramref name="pointer"/> (in cells), each where it is drawn
    /// <paramref name="progress"/> ticks on (<see cref="PieceDrawn"/>), while the scanner is <paramref name="worn"/>.
    /// </summary>
    public static Readout? Of(IReadOnlyList<Piece> pieces, (double X, double Y) pointer, bool worn, double progress)
    {
        if (!worn || WreckAim.Under(pieces, pointer) is not { } wreck || Content.Wreckage.HpOf(wreck.Item) is not int hp) return null;
        var (x, y) = PieceDrawn.At(wreck, progress);
        int top = WreckAim.HalfOf(wreck.Item)!.Value.HalfH - WreckAim.Margin;
        return new Readout(wreck.Id, x / DebrisModule.Unit, (y - top) / DebrisModule.Unit, Math.Max(0, hp - wreck.Damage), hp);
    }
}
