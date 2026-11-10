using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using Pens = Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The trail a held blade leaves by its own motion, whatever moves it (blade-trail): its edge's two ends sampled every frame
/// where they are in the world, the stretch between two samples drawn by how fast the edge went, left where it was swept and
/// gone in <see cref="LifeMs"/>. It shows no reach, only where the blade has been; what it is drawn as is the drawer's.
/// </summary>
public sealed class BladeTrail
{
    /// <summary>How long a stretch of the trail is seen, in ms.</summary>
    public const double LifeMs = 170;

    /// <summary>The edge's speed, in pixels a second, under which it leaves nothing, and at which its trail is whole.</summary>
    public const double Slow = 60, Fast = 280;

    /// <summary>A step longer than this, in pixels, is a jump, not a sweep: the trail breaks.</summary>
    public const double Jump = 30;

    private readonly record struct Sample(double At, (double X, double Y) From, (double X, double Y) To, double Shown);

    private readonly List<Sample> _samples = [];
    private readonly List<(double X, double Y)> _one = [], _other = [];
    private double _clock;

    /// <summary>Whether anything is drawn now.</summary>
    public bool Live
    {
        get
        {
            for (int i = 1; i < _samples.Count; i++)
                if (_samples[i].Shown > 0) return true;
            return false;
        }
    }

    /// <summary><paramref name="elapsedMs"/> on, the edge from its one end to the other in the world now; none while no blade is held.</summary>
    public void Step(double elapsedMs, ((double X, double Y) From, (double X, double Y) To)? edge)
    {
        _clock += elapsedMs;
        int gone = 0;
        while (gone < _samples.Count && _clock - _samples[gone].At > LifeMs) gone++;
        if (gone > 0) _samples.RemoveRange(0, gone);
        if (edge is not { } e) return;
        double shown = 0;
        if (_samples.Count > 0 && elapsedMs > 0)
        {
            var last = _samples[^1];
            double step = Math.Max(Distance(last.From, e.From), Distance(last.To, e.To));
            // How much of a trail this stretch leaves, by how fast the edge went; a jump leaves none.
            shown = step > Jump ? 0 : Pens.Clamp((step * 1000 / elapsedMs - Slow) / (Fast - Slow));
        }
        _samples.Add(new(_clock, e.From, e.To, shown));
    }

    /// <summary>
    /// All of it as it is now, into <paramref name="ink"/> in <paramref name="tone"/>, fainter as it ages, its points of the
    /// world turned into the drawer's own by <paramref name="local"/>.
    /// </summary>
    public void Draw(Ink ink, Func<(double X, double Y), (double X, double Y)> local, Ink.Tone tone)
    {
        for (int i = 1; i < _samples.Count; i++)
        {
            var (was, now) = (_samples[i - 1], _samples[i]);
            if (now.Shown <= 0) continue;
            double left = 1 - (_clock - now.At) / LifeMs;
            _one.Clear();
            _other.Clear();
            _one.Add(local(was.From));
            _one.Add(local(now.From));
            _other.Add(local(was.To));
            _other.Add(local(now.To));
            ink.Band(_one, _other, tone.Faded((float)(now.Shown * left * left)));
        }
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
