using System;
using Stillwake.Content;

namespace Stillwake.Game.Features.Pipes;

/// <summary>A port as the piping shows it (2026-10-29-typed-ports): a socket in its fluid's colour, an arrow out of the machine or into it.</summary>
public static class Sockets
{
    /// <summary>The colour of a fluid's sockets, as 0xRRGGBB: air the canister's blue, water the cistern's teal, liquid oxygen pale blue.</summary>
    public static uint ColourOf(string fluid) => fluid switch
    {
        Fluids.Air => 0x7fc4f0,
        Fluids.Water => 0x2fa3b8,
        Fluids.LiquidOxygen => 0xa8d8ff,
        _ => throw new ArgumentException($"no socket colour for fluid {fluid}"),
    };

    /// <summary>
    /// Which way a port's arrow points, a unit step on one axis: away from the middle of its machine's footprint (<paramref name="width"/>
    /// by <paramref name="height"/> cells) for one that gives, toward it for one that takes. A port at the middle points up or down.
    /// </summary>
    public static (int Dx, int Dy) ArrowOf(Port port, int width, int height)
    {
        // Twice the offsets, so a footprint's middle is whole.
        int dx = 2 * port.X + 1 - width, dy = 2 * port.Y + 1 - height;
        var (x, y) = Math.Abs(dx) >= Math.Abs(dy) && dx != 0 ? (Math.Sign(dx), 0) : (0, dy >= 0 ? 1 : -1);
        return port.Way == Way.Out ? (x, y) : (-x, -y);
    }
}
