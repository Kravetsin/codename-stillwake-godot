using System;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// The water eased between the sim's ticks: each cell's drawn amount, or a stream's width (the red of <see cref="WaterCells"/>'
/// texels), moves toward the sim's a part of the way each frame, by <see cref="Rate"/>, so a level rising, spreading or
/// draining at 20 steps a second glides at the frame's rate, and a stream let out each second tick reads steady. A cell that
/// was a stream stays one for <see cref="HoldS"/> after, so it never flips to lying water and back; one newly in view is drawn
/// as the sim has it; one the sim dried, or a stream gone, fades out before it goes.
/// </summary>
public sealed class WaterEase
{
    /// <summary>How fast the drawn amount closes on the sim's, a second: most of the way in a tick and a half.</summary>
    public const double Rate = 14;

    /// <summary>How long, in seconds, a cell stays drawn as a stream after the sim's last says it is one: a few ticks.</summary>
    public const double HoldS = 0.3;

    /// <summary>The least drawn amount, of 255, a cell the sim has dried keeps showing.</summary>
    private const float Fading = 6;

    private float[] _shown = [], _next = [], _held = [], _nextHeld = [];
    private int _left, _top, _wide, _tall;

    /// <summary>
    /// Eases <paramref name="texels"/>, as <see cref="WaterCells.Write"/> wrote them for the box at (<paramref name="left"/>,
    /// <paramref name="top"/>), <paramref name="elapsedS"/> after the last frame; returns whether any cell is drawn.
    /// </summary>
    public bool Step(byte[] texels, int left, int top, int wide, int tall, double elapsedS)
    {
        if (_next.Length != wide * tall) (_next, _nextHeld) = (new float[wide * tall], new float[wide * tall]);
        double dt = Math.Max(0, elapsedS);
        float k = (float)(1 - Math.Exp(-dt * Rate));
        bool any = false;
        for (int y = 0; y < tall; y++)
            for (int x = 0; x < wide; x++)
            {
                int at = y * wide + x, gx = left + x - _left, gy = top + y - _top;
                bool seen = gx >= 0 && gy >= 0 && gx < _wide && gy < _tall;
                float target = texels[at * 4 + 3] == 0 ? 0 : texels[at * 4];
                float held = texels[at * 4 + 1] != 0 ? (float)HoldS : seen ? Math.Max(0, _held[gy * _wide + gx] - (float)dt) : 0;
                float was = seen ? _shown[gy * _wide + gx] : target;
                float shown = was + (target - was) * k;
                if (Math.Abs(target - shown) < 1) shown = target;
                bool drawn = target > 0 || shown >= Fading;
                if (!drawn) shown = held = 0;
                (_next[at], _nextHeld[at]) = (shown, held);
                texels[at * 4] = (byte)Math.Round(shown);
                if (held > 0) texels[at * 4 + 1] = 255;
                texels[at * 4 + 3] = drawn ? (byte)255 : (byte)0;
                any |= drawn;
            }
        (_shown, _next) = (_next, _shown);
        (_held, _nextHeld) = (_nextHeld, _held);
        (_left, _top, _wide, _tall) = (left, top, wide, tall);
        return any;
    }
}
