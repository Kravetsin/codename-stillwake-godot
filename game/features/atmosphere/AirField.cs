using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>
/// The air as the overlay draws it, cell by cell: how much of each cell's air shows, eased toward what
/// the sim says so a cell comes and goes over <see cref="EaseMs"/> rather than at once, how red it is
/// for its room venting, and which way a venting room's air runs, toward its nearest opening. The first
/// survey shows the world as it is, eased from nothing only after. Purely drawn: the sim stays per cell,
/// on or off.
/// </summary>
public sealed class AirField(int width, int height)
{
    /// <summary>A cell closes the gap to what it should show at this pace: most of the way in about this long.</summary>
    public const double EaseMs = 220;

    public int Width { get; } = width;
    public int Height { get; } = height;

    private readonly float[] _air = new float[width * height], _red = new float[width * height];
    private readonly bool[] _aired = new bool[width * height], _venting = new bool[width * height], _inRoom = new bool[width * height];
    private readonly float[] _flow = new float[width * height];
    private bool _surveyed;

    /// <summary>Whether every cell shows what it should, so the texture need not be written again.</summary>
    public bool Settled { get; private set; }

    /// <summary>How much of <paramref name="cell"/>'s air shows, 0 to 1.</summary>
    public float Air(int cell) => _air[cell];

    /// <summary>Whether the sim says <paramref name="cell"/> holds air.</summary>
    public bool Aired(int cell) => cell >= 0 && cell < _aired.Length && _aired[cell];

    /// <summary>Whether <paramref name="cell"/> lies in a room, not in a wall or space.</summary>
    public bool InRoom(int cell) => cell >= 0 && cell < _inRoom.Length && _inRoom[cell];

    /// <summary>The heading, in radians screen-wise, a venting room's air runs at <paramref name="cell"/>; null outside one.</summary>
    public double? FlowAt(int cell) => InRoom(cell) && _venting[cell] ? _flow[cell] : null;

    /// <summary>
    /// What the sim says now: <paramref name="roomOf"/> each cell's room (negative for none),
    /// <paramref name="aired"/> the cells holding air, and each venting room's openings by room id.
    /// </summary>
    public void Survey(ReadOnlySpan<int> roomOf, Func<int, bool> aired, IReadOnlyDictionary<int, IReadOnlyList<int>> venting)
    {
        for (int cell = 0; cell < _air.Length; cell++)
        {
            int room = roomOf[cell];
            _inRoom[cell] = room >= 0;
            _aired[cell] = room >= 0 && aired(cell);
            _venting[cell] = room >= 0 && venting.TryGetValue(room, out var openings) && openings.Count > 0;
            if (_venting[cell]) _flow[cell] = (float)Toward(cell, venting[room]);
        }
        if (!_surveyed)
        {
            _surveyed = true;
            for (int cell = 0; cell < _air.Length; cell++)
            {
                _air[cell] = _aired[cell] ? 1 : 0;
                _red[cell] = _venting[cell] ? 1 : 0;
            }
        }
        Settled = false;
    }

    /// <summary>The heading from <paramref name="cell"/> to the nearest of <paramref name="openings"/>, the first on a tie.</summary>
    private double Toward(int cell, IReadOnlyList<int> openings)
    {
        int x = cell % Width, y = cell / Width, best = openings[0], bestD = int.MaxValue;
        foreach (int o in openings)
        {
            int dx = o % Width - x, dy = o / Width - y, d = dx * dx + dy * dy;
            if (d < bestD) (best, bestD) = (o, d);
        }
        return Math.Atan2(best / Width - y, best % Width - x);
    }

    /// <summary>Moves every cell on by <paramref name="elapsedMs"/> toward what it should show.</summary>
    public void Ease(double elapsedMs)
    {
        if (Settled) return;
        float k = (float)Math.Min(1, elapsedMs / EaseMs);
        bool settled = true;
        for (int cell = 0; cell < _air.Length; cell++)
        {
            settled &= Step(_air, cell, _aired[cell] ? 1 : 0, k);
            settled &= Step(_red, cell, _venting[cell] ? 1 : 0, k);
        }
        Settled = settled;
    }

    private static bool Step(float[] values, int cell, float target, float k)
    {
        float gap = target - values[cell];
        if (Math.Abs(gap) < 0.004f)
        {
            values[cell] = target;
            return true;
        }
        values[cell] += gap * k;
        return false;
    }

    /// <summary>
    /// Four bytes a cell for the overlay's shader: the air showing, the red of venting, the heading of the
    /// flow as a share of a turn, and whole where the cell is in a room.
    /// </summary>
    public void Write(byte[] texels)
    {
        for (int cell = 0; cell < _air.Length; cell++)
        {
            int k = cell * 4;
            texels[k] = (byte)Math.Round(_air[cell] * 255);
            texels[k + 1] = (byte)Math.Round(_red[cell] * 255);
            double turn = _flow[cell] / (2 * Math.PI);
            texels[k + 2] = (byte)Math.Round((turn - Math.Floor(turn)) * 255);
            texels[k + 3] = (byte)(_inRoom[cell] ? 255 : 0);
        }
    }
}
