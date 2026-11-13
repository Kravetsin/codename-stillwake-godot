using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// Leaves in the air and on the ground (plant-stirs): each a small picture falling at its look's pace, swinging side to side
/// (stalling at the ends of a swing, falling fastest through its middle), turning and flipping over as it goes, carried by
/// the wind; landed on the ground under it, it lies a while and fades. Shaken off by a hit, a body going through a bush, a
/// falling tree or a gust; drawn by <c>LeafLayer</c>. At most <see cref="Most"/>: past it the oldest go. Plain C#.
/// </summary>
public sealed class Leaves
{
    /// <summary>The most leaves at once.</summary>
    public const int Most = 1500;

    /// <summary>
    /// Seconds a leaf lies on the ground, then fades over; seconds in the air before it is let go; how fast it takes up its
    /// falling pace (a share of the difference a second); seconds its swing takes to grow.
    /// </summary>
    public const float Lie = 3, Fade = 1.2f, Life = 14, Settle = 3, Swaying = 0.7f;

    /// <summary>
    /// How a look's leaves fly: falling at <see cref="Fall"/> px/s, swinging <see cref="Sway"/> px/s side to side
    /// <see cref="SwayHz"/> times a second, tilted <see cref="Tilt"/> radians with the swing, flipping over
    /// <see cref="FlipHz"/> times a second and turning up to <see cref="Spin"/> radians a second (both faster in a wind),
    /// taking <see cref="Catch"/> of the wind's speed. Each leaf's own a little more or less.
    /// </summary>
    public sealed record Flight(float Fall, float Sway, float SwayHz, float FlipHz, float Spin, float Tilt, float Catch);

    /// <summary>One leaf.</summary>
    public sealed class Leaf
    {
        public float X, Y, Vx, Vy;
        /// <summary>How fast it gives up its speed across for the air's, a share a second.</summary>
        public float Drag;
        /// <summary>Its turn, radians, and the turn it swings about; how fast that turns.</summary>
        public float Angle, Turn, Spin;
        /// <summary>Its tumble: its width is the cosine of this, flipping it over; how fast it tumbles.</summary>
        public float Flip, FlipSpeed;
        /// <summary>Its swing's phase, rate and reach; its falling pace; its look's tilt and catch of the wind.</summary>
        public float Swing, SwingSpeed, SwingReach, Fall, Tilt, Catch;
        /// <summary>Which picture it is drawn with; whether it is drawn in front of the heroes.</summary>
        public int Picture;
        public bool Front;
        /// <summary>How light its paint is, about 1: no two alike.</summary>
        public float Shade;
        /// <summary>Seconds in the air; seconds lying, or -1 in the air.</summary>
        public float Age, Lying = -1;
        /// <summary>The ground under it, looked up when it enters a new column.</summary>
        public float Floor;
        public int Column = int.MinValue;

        /// <summary>How much of it shows: whole till it has lain <see cref="Lie"/>, then fading.</summary>
        public float Alpha => Lying < Lie ? 1 : Math.Max(0, 1 - (Lying - Lie) / Fade);

        /// <summary>
        /// Its width now, a share of its picture's, below zero showing its back: its tumble's cosine, never quite edge-on; lying,
        /// seen from low, a little over half.
        /// </summary>
        public float Width
        {
            get
            {
                float c = MathF.Cos(Flip), side = c < 0 ? -1 : 1;
                return Lying >= 0 ? 0.6f * side : MathF.Max(0.07f, MathF.Abs(c)) * side;
            }
        }
    }

    private readonly List<Leaf> _leaves = [];
    private readonly Func<double> _random;
    private readonly Func<float, float, float> _floor;
    private readonly IReadOnlyList<Flight> _flights;

    /// <summary>
    /// Leaves drawing their looks from <paramref name="random"/> (0 to 1), landing on <paramref name="floor"/>(x, y): the top of
    /// the ground under that point, in pixels, or past the world's bottom where there is none; a leaf of picture n flying as
    /// <paramref name="flights"/>[n].
    /// </summary>
    public Leaves(Func<double> random, Func<float, float, float> floor, IReadOnlyList<Flight> flights)
    {
        _random = random;
        _floor = floor;
        _flights = flights;
    }

    /// <summary>Every leaf, in the air and lying.</summary>
    public IReadOnlyList<Leaf> All => _leaves;

    /// <summary>
    /// A leaf of picture <paramref name="picture"/> at (<paramref name="x"/>, <paramref name="y"/>) going
    /// (<paramref name="vx"/>, <paramref name="vy"/>) pixels a second, giving up its speed across <paramref name="drag"/> a second.
    /// </summary>
    public Leaf Add(float x, float y, float vx, float vy, int picture, float drag = 2.4f)
    {
        if (_leaves.Count >= Most) _leaves.RemoveAt(0);
        var f = _flights[picture];
        const float tau = 2 * MathF.PI;
        var leaf = new Leaf
        {
            X = x,
            Y = y,
            Vx = vx,
            Vy = vy,
            Drag = drag,
            Picture = picture,
            Turn = Between(0, tau),
            Spin = Between(-1, 1) * f.Spin,
            Flip = Between(0, tau),
            FlipSpeed = f.FlipHz * tau * Between(0.7f, 1.3f) * (_random() < 0.5 ? -1 : 1),
            Swing = Between(0, tau),
            SwingSpeed = f.SwayHz * tau * Between(0.8f, 1.2f),
            SwingReach = f.Sway * Between(0.7f, 1.3f),
            Fall = f.Fall * Between(0.85f, 1.15f),
            Tilt = f.Tilt,
            Catch = f.Catch,
            Front = _random() < 0.4,
            Shade = Between(0.85f, 1.1f),
        };
        leaf.Angle = leaf.Turn;
        _leaves.Add(leaf);
        return leaf;
    }

    /// <summary>Moves every leaf on by <paramref name="seconds"/>, the wind blowing <paramref name="wind"/>(x) pixels a second across.</summary>
    public void Advance(double seconds, Func<float, float> wind)
    {
        float dt = (float)Math.Min(seconds, 0.1);
        int kept = 0;
        for (int i = 0; i < _leaves.Count; i++)
        {
            var l = _leaves[i];
            if (Step(l, dt, wind)) _leaves[kept++] = l;
        }
        _leaves.RemoveRange(kept, _leaves.Count - kept);
    }

    /// <summary>One leaf on by <paramref name="dt"/>; false once it is gone.</summary>
    private bool Step(Leaf l, float dt, Func<float, float> wind)
    {
        if (l.Lying >= 0)
        {
            l.Lying += dt;
            return l.Lying < Lie + Fade;
        }
        l.Age += dt;
        if (l.Age > Life) return false;
        float blow = wind(l.X), windy = 1 + MathF.Abs(blow) / 70;
        l.Vy += (l.Fall - l.Vy) * Settle * dt;
        l.Vx += (l.Catch * blow - l.Vx) * l.Drag * dt;
        l.Swing += l.SwingSpeed * dt;
        l.Flip += l.FlipSpeed * windy * dt;
        l.Turn += l.Spin * windy * dt;
        // A pendulum: fastest down and across through the middle of a swing, stalling at its ends; grown as it finds its fall.
        float grow = MathF.Min(1, l.Age / Swaying), swing = MathF.Cos(l.Swing);
        l.X += (l.Vx + l.SwingReach * grow * swing) * dt;
        l.Y += l.Vy * (l.Vy > 0 ? 0.5f + 0.5f * MathF.Abs(swing) : 1) * dt;
        l.Angle = l.Turn + l.Tilt * grow * swing;
        int column = (int)MathF.Floor(l.X / 8);
        if (column != l.Column)
        {
            l.Column = column;
            l.Floor = _floor(l.X, l.Y);
        }
        if (l.Y < l.Floor) return true;
        // Landed: lying on the ground, nearly flat.
        l.Y = l.Floor;
        l.Vx = l.Vy = 0;
        l.Lying = 0;
        l.Angle = MathF.Round(l.Angle / MathF.PI) * MathF.PI + Between(-0.2f, 0.2f);
        return true;
    }

    private float Between(float from, float to) => from + (to - from) * (float)_random();
}
