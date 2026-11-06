using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// What this hero just gained, as a column of chips over its head (2026-11-06-head-tally): one per item, its count adding up
/// while the item keeps coming, held a while after the last and faded. The column hangs on an anchor that follows the head
/// through a critically damped spring, so it trails a walking hero and never shakes with it. The anchor in cells, rows in rows.
/// </summary>
public sealed class HeadTally
{
    /// <summary>How long a chip stays whole after its last gain, then how long it fades, in ms; a gain within both adds to it.</summary>
    public const double HoldMs = 1600, FadeMs = 500;
    /// <summary>How long an add's bump and a new chip's fade-in last, in ms.</summary>
    public const double BumpMs = 220, InMs = 140;
    /// <summary>
    /// The anchor's springs, per second, critically damped so it reaches a still head without overshoot: stiff across, to keep
    /// within about 1.5 cells of a walk; soft up and down, where the head bobs with each stride.
    /// </summary>
    public const double OmegaX = 20, OmegaY = 9;
    /// <summary>How fast a chip slides to its row, per second.</summary>
    public const double Slide = 14;
    /// <summary>The key of a body's chip: all it gave at once (2026-10-29-body-bag).</summary>
    public const string Body = "items.fromBody";

    /// <summary>One item's chip: what it names and how many, when it opened and last grew, and its height over the anchor.</summary>
    public sealed class Chip(string key, int count, double born)
    {
        public string Key { get; } = key;
        public int Count { get; internal set; } = count;
        public double BornMs { get; } = born;
        public double LastMs { get; internal set; } = born;
        /// <summary>How many rows above the anchor it sits, sliding to its row: 0 the newest.</summary>
        public double Rise { get; internal set; } = double.NaN;
    }

    private readonly List<Chip> _chips = [];
    private double _now, _vx, _vy;
    private bool _anchored;

    /// <summary>The chips, oldest first: the newest is lowest, at the anchor.</summary>
    public IReadOnlyList<Chip> Chips => _chips;

    /// <summary>Where the column stands: the bottom chip's middle, in cells.</summary>
    public (double X, double Y) Anchor { get; private set; }

    /// <summary>Adds <paramref name="count"/> of <paramref name="key"/> gained now, to its chip while it shows, else to a new one.</summary>
    public void Add(string key, int count)
    {
        var chip = _chips.Find(c => c.Key == key);
        if (chip != null)
        {
            chip.Count += count;
            chip.LastMs = _now;
            _chips.Remove(chip);
            _chips.Add(chip);
            return;
        }
        _chips.Add(new Chip(key, count, _now));
    }

    /// <summary>Moves <paramref name="elapsedMs"/> on: chips age and go; the anchor follows the head at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public void Update(double elapsedMs, double x, double y)
    {
        _now += elapsedMs;
        _chips.RemoveAll(c => _now - c.LastMs >= HoldMs + FadeMs);
        if (_chips.Count == 0 || !_anchored)
        {
            (Anchor, _vx, _vy, _anchored) = ((x, y), 0, 0, _chips.Count > 0);
        }
        else
        {
            // Small steps, so a long frame cannot throw the spring.
            for (double left = elapsedMs / 1000; left > 0; left -= 1.0 / 120)
            {
                double dt = Math.Min(left, 1.0 / 120);
                var (ax, ay) = Anchor;
                _vx += (OmegaX * OmegaX * (x - ax) - 2 * OmegaX * _vx) * dt;
                _vy += (OmegaY * OmegaY * (y - ay) - 2 * OmegaY * _vy) * dt;
                Anchor = (ax + _vx * dt, ay + _vy * dt);
            }
        }
        double k = 1 - Math.Exp(-Slide * elapsedMs / 1000);
        for (int i = 0; i < _chips.Count; i++)
        {
            var chip = _chips[i];
            double row = _chips.Count - 1 - i;
            chip.Rise = double.IsNaN(chip.Rise) ? row : chip.Rise + (row - chip.Rise) * k;
        }
    }

    /// <summary>How solid <paramref name="chip"/> is now, 0 to 1: faded in, held, faded out.</summary>
    public double Alpha(Chip chip)
    {
        double fin = Math.Clamp((_now - chip.BornMs) / InMs, 0, 1), age = _now - chip.LastMs;
        return fin * (age < HoldMs ? 1 : Math.Clamp(1 - (age - HoldMs) / FadeMs, 0, 1));
    }

    /// <summary>How much <paramref name="chip"/> swells from its last add, 1 at once to 0, eased out.</summary>
    public double Bump(Chip chip)
    {
        double b = Math.Max(0, 1 - (_now - chip.LastMs) / BumpMs);
        return b * b;
    }
}
