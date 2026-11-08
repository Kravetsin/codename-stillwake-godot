using System;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// The water eased between the sim's ticks: each cell's drawn amount, or a stream's width (the red of <see cref="WaterCells"/>'
/// texels), moves toward the sim's a part of the way each frame, by <see cref="Rate"/>, so a level rising, spreading or
/// draining at 20 steps a second glides at the frame's rate, and a stream's trail running down reads steady. One newly in
/// view is drawn as the sim has it; one the sim dried fades out before it goes, a stream as a stream.
/// </summary>
public sealed class WaterEase
{
    /// <summary>How fast the drawn amount closes on the sim's, a second: most of the way in a tick and a half.</summary>
    public const double Rate = 14;

    /// <summary>The least drawn amount, of 255, a cell the sim has dried keeps showing.</summary>
    private const float Fading = 6;

    private float[] _shown = [], _next = [];
    private bool[] _stream = [], _nextStream = [];
    private int _left, _top, _wide, _tall;

    /// <summary>
    /// Eases <paramref name="texels"/>, as <see cref="WaterCells.Write"/> wrote them for the box at (<paramref name="left"/>,
    /// <paramref name="top"/>), <paramref name="elapsedS"/> after the last frame; returns whether any cell is drawn.
    /// </summary>
    public bool Step(byte[] texels, int left, int top, int wide, int tall, double elapsedS)
    {
        if (_next.Length != wide * tall) (_next, _nextStream) = (new float[wide * tall], new bool[wide * tall]);
        float k = (float)(1 - Math.Exp(-Math.Max(0, elapsedS) * Rate));
        bool any = false;
        for (int y = 0; y < tall; y++)
            for (int x = 0; x < wide; x++)
            {
                int at = y * wide + x, gx = left + x - _left, gy = top + y - _top, was = gy * _wide + gx;
                bool seen = gx >= 0 && gy >= 0 && gx < _wide && gy < _tall;
                float target = texels[at * 4 + 3] == 0 ? 0 : texels[at * 4];
                float shown = seen ? _shown[was] + (target - _shown[was]) * k : target;
                if (Math.Abs(target - shown) < 1) shown = target;
                bool drawn = target > 0 || shown >= Fading;
                bool stream = drawn && (texels[at * 4 + 1] != 0 || target == 0 && seen && _stream[was]);
                (_next[at], _nextStream[at]) = (drawn ? shown : 0, stream);
                texels[at * 4] = (byte)Math.Round(drawn ? shown : 0);
                texels[at * 4 + 1] = stream ? (byte)255 : (byte)0;
                texels[at * 4 + 3] = drawn ? (byte)255 : (byte)0;
                any |= drawn;
            }
        (_shown, _next) = (_next, _shown);
        (_stream, _nextStream) = (_nextStream, _stream);
        (_left, _top, _wide, _tall) = (left, top, wide, tall);
        return any;
    }
}
