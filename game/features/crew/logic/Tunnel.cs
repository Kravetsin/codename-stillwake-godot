using System;
using CrewSpec = Stillwake.Content.Crew;
using World = Stillwake.Sim.World;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What the last breath draws at one moment (2026-11-05-the-last-breath): how blurred the screen is, 0 to 1 of the most,
/// more toward its edges; how brown; the world's sway about the hero, in radians; its ghost's offset, in shares of the
/// screen's width; how far the picture swims, a share of the screen's width; and how far it has sunk into ink, 0 to 1.
/// </summary>
public readonly record struct TunnelLook(double Blur, double Dark, double Sway, double GhostX, double GhostY, double Swim, double Ink);

/// <summary>
/// The last breath on the screen (2026-11-05-the-last-breath): the world blurs, sways, swims and doubles and turns brown,
/// the edges first, with no line between, sinking into ink at the end and staying ink while passed out. <see cref="Read"/>
/// takes the sim's counter four times a second; <see cref="Advance"/> runs it on between reads, eases the jumps a hit makes
/// and lifts it as air comes back. Plain C#: <c>TunnelView</c> shows it.
/// </summary>
public sealed class Tunnel
{
    /// <summary>How long it takes to clear fully once air is back or the hero wakes.</summary>
    public const double LiftS = 0.7;
    /// <summary>How long it takes to close fully on a jump: a hit's cut, or a blow passing the hero out at once.</summary>
    public const double CatchS = 0.4;
    /// <summary>From here on the picture sinks into ink, all ink at the end.</summary>
    public const double Sinks = 0.85;

    /// <summary>The ghost's reach at its widest, a share of the screen's width, and its height to its width.</summary>
    private const double GhostReach = 14.0 / 480, GhostRise = 0.25;
    /// <summary>How far the picture swims at the end, a share of the screen's width.</summary>
    private const double SwimMost = 0.006;

    private double _target, _shown;
    private bool _gasping;

    /// <summary>How far it has gone, 0 clear to 1 ink.</summary>
    public double Shown => _shown;

    /// <summary>A read of this player's hero: the ticks left of its last breath and the ticks it is out, or none.</summary>
    public void Read(int gasp, int @out)
    {
        _gasping = gasp > 0 && @out == 0;
        _target = @out > 0 ? 1 : _gasping ? 1 - (double)gasp / CrewSpec.LastBreath : 0;
    }

    /// <summary>Runs <paramref name="seconds"/> on: the last breath on toward ink as the sim runs it, the shown closing on it.</summary>
    public double Advance(double seconds)
    {
        if (_gasping) _target = Math.Min(1, _target + seconds * World.TicksPerSecond / CrewSpec.LastBreath);
        _shown = _target > _shown ? Math.Min(_target, _shown + seconds / CatchS) : Math.Max(_target, _shown - seconds / LiftS);
        return _shown;
    }

    /// <summary>The look at <paramref name="p"/> gone, <paramref name="clockS"/> into the game.</summary>
    public static TunnelLook Of(double p, double clockS)
    {
        p = Math.Clamp(p, 0, 1);
        // The blur breathes a little, slower as it deepens.
        double blur = Smooth(0, 0.9, p) * (1 + 0.12 * Math.Sin(clockS * (2.4 - p)));
        double ghost = Math.Sin(clockS * 1.9) * GhostReach * Smooth(0.1, 0.8, p);
        return new TunnelLook(
            Math.Clamp(blur, 0, 1), 0.62 * Math.Pow(p, 1.2), Math.Sin(clockS * 0.8) * 0.028 * p,
            ghost, ghost * GhostRise, SwimMost * Smooth(0.2, 1, p), Smooth(Sinks, 1, p));
    }

    private static double Smooth(double a, double b, double x)
    {
        double f = Math.Clamp((x - a) / (b - a), 0, 1);
        return f * f * (3 - 2 * f);
    }
}
