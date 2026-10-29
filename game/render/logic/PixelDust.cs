using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// The pixels killed creatures burst into (2026-10-29-kill-feel), in world pixels and seconds: each flies under the surface's
/// gravity, bounces off what is solid, lands on it or on the pixels landed in its column, sliding a few down to a lower
/// neighbour, and fades; afloat it drifts as it was thrown, through everything. Drawn only, never part of the world: plain C#,
/// asking the ground through <see cref="IGround"/>.
/// </summary>
public sealed class PixelDust(int most)
{
    /// <summary>What the pixels meet: a world pixel solid, or out in space with nothing to fall to.</summary>
    public interface IGround
    {
        bool Solid(int x, int y);
        bool Afloat(int x, int y);
        /// <summary>What a fall gains a second, in pixels a second.</summary>
        float Gravity { get; }
    }

    /// <summary>One pixel: where it is and goes, its colour, how old and how long it lasts, landed or not.</summary>
    public struct Mote
    {
        public float X, Y, Vx, Vy, Age, Lasts;
        public (byte R, byte G, byte B) Colour;
        public bool Landed;

        /// <summary>How much of it is drawn, 1 till its last <see cref="KillLooks.Fades"/>, then down to none.</summary>
        public readonly float Alpha => Math.Clamp((Lasts - Age) / KillLooks.Fades, 0, 1);
    }

    private readonly List<Mote> _motes = [];
    // Each column's landed pixels: the highest row one lies in, and how many lie there.
    private readonly Dictionary<int, (int Top, int Count)> _heaps = [];

    public IReadOnlyList<Mote> Motes => _motes;

    /// <summary>Adds <paramref name="mote"/> unless <c>most</c> fly or lie already, or it starts inside something solid.</summary>
    public void Add(Mote mote, IGround ground)
    {
        if (_motes.Count >= most || ground.Solid(Floor(mote.X), Floor(mote.Y))) return;
        _motes.Add(mote);
    }

    /// <summary>Moves every pixel <paramref name="dt"/> seconds on; one past its time is gone, its column a pixel lower.</summary>
    public void Step(double dt, IGround ground)
    {
        float t = (float)dt;
        for (int i = _motes.Count - 1; i >= 0; i--)
        {
            var m = _motes[i];
            m.Age += t;
            if (m.Age >= m.Lasts)
            {
                if (m.Landed) Leave(Floor(m.X));
                _motes.RemoveAt(i);
                continue;
            }
            if (!m.Landed) Fly(ref m, t, ground);
            _motes[i] = m;
        }
    }

    private void Fly(ref Mote m, float t, IGround ground)
    {
        if (ground.Afloat(Floor(m.X), Floor(m.Y)))
        {
            (m.X, m.Y) = (m.X + m.Vx * t, m.Y + m.Vy * t);
            return;
        }
        m.Vy += ground.Gravity * t;
        // A pixel a step at most, so nothing passes through a wall or a floor however long the frame.
        float dx = m.Vx * t, dy = m.Vy * t;
        int steps = Math.Max(1, (int)MathF.Ceiling(MathF.Max(MathF.Abs(dx), MathF.Abs(dy))));
        for (int s = 0; s < steps && !m.Landed; s++)
        {
            float nx = m.X + dx / steps;
            if (Blocked(Floor(nx), Floor(m.Y), ground))
            {
                (m.Vx, dx) = (-m.Vx * KillLooks.WallKeeps, -dx * KillLooks.WallKeeps);
            }
            else m.X = nx;
            float ny = m.Y + dy / steps;
            int x = Floor(m.X);
            if (!Blocked(x, Floor(ny), ground)) m.Y = ny;
            else if (dy > 0) Land(ref m, x, Floor(m.Y), ground);
            else (m.Vy, dy) = (0, 0);
        }
    }

    /// <summary>Lands <paramref name="m"/> at row <paramref name="y"/> of column <paramref name="x"/>, slid first down to lower neighbours.</summary>
    private void Land(ref Mote m, int x, int y, IGround ground)
    {
        for (int s = 0; s < KillLooks.Slides; s++)
        {
            int side = Floor(m.X * 7.31f) % 2 == 0 ? 1 : -1;
            if (Surface(x + side, y, ground) is int a && a > y) (x, y) = (x + side, a);
            else if (Surface(x - side, y, ground) is int b && b > y) (x, y) = (x - side, b);
            else break;
        }
        (m.X, m.Y, m.Vx, m.Vy, m.Landed) = (x + 0.5f, y + 0.5f, 0, 0, true);
        var (top, count) = _heaps.GetValueOrDefault(x, (y, 0));
        _heaps[x] = (Math.Min(top, y), count + 1);
    }

    /// <summary>The row a pixel would rest in in column <paramref name="x"/> falling from row <paramref name="y"/>, a few rows down at most; null when blocked or deeper.</summary>
    private int? Surface(int x, int y, IGround ground)
    {
        if (Blocked(x, y, ground)) return null;
        for (int r = y; r < y + 3; r++)
            if (Blocked(x, r + 1, ground)) return r;
        return null;
    }

    private bool Blocked(int x, int y, IGround ground) =>
        ground.Solid(x, y) || _heaps.TryGetValue(x, out var heap) && y >= heap.Top;

    private void Leave(int x)
    {
        if (!_heaps.TryGetValue(x, out var heap)) return;
        if (heap.Count <= 1) _heaps.Remove(x);
        else _heaps[x] = (heap.Top + 1, heap.Count - 1);
    }

    private static int Floor(float v) => (int)MathF.Floor(v);

    /// <summary>Every pixel gone at once: another surface, or a load.</summary>
    public void Clear()
    {
        _motes.Clear();
        _heaps.Clear();
    }
}
