using System;
using CrewSpec = Stillwake.Content.Crew;
using World = Stillwake.Sim.World;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What the fogged visor draws at one moment (2026-11-05-the-last-breath): how far in from the corners the fog reaches, 0 to
/// about 0.6 of the way to the middle; how far it has evaporated, 0 to 1; and how much the suit gauge's last lit lamp dims.
/// </summary>
public readonly record struct FogLook(double Reach, double Go, double LastDim);

/// <summary>
/// A low suit without red (2026-11-05-the-last-breath): the visor fogs from its corners with each breath out and clears a
/// little with each breath in, the breath quicker and the fog deeper as the air runs down; at no air the gasps slow and
/// flatten into the last breath; out of danger the fog evaporates in patches. Read four times a second from the suit's
/// danger and share; <see cref="Advance"/> runs the breath between reads. Plain C#: <c>VisorFogView</c> draws it.
/// </summary>
public sealed class VisorFog
{
    /// <summary>How long the fog takes to come in once the suit runs low, and to evaporate once it is out of danger.</summary>
    public const double ComeS = 0.7, EvaporateS = 2.4;
    /// <summary>How long the fog takes to deepen or thin by the whole of its range as the share moves.</summary>
    private const double DeepenS = 1.0;
    /// <summary>A breath's length, in seconds: just below the danger's line, at no air, a gasp first and how much slower the last are.</summary>
    private const double Calm = 3.2, Quick = 0.95, GaspFirst = 1.3, GaspSlows = 1.8, Evaporating = 2.4;
    private static readonly double LastBreathS = (double)CrewSpec.LastBreath / World.TicksPerSecond;

    private bool _fogged, _dry, _out;
    private double _depthTarget, _depth, _on, _go = -1, _phase, _dryS;

    /// <summary>A read: the suit's danger and its share of a full suit.</summary>
    public void Read(SuitDanger danger, double share)
    {
        bool fogged = danger != SuitDanger.None;
        if (fogged) _depthTarget = danger == SuitDanger.Out ? 1 : Math.Clamp((Danger.Low - share) / Danger.Low, 0, 1);
        else if (_fogged && _on > 0) _go = Math.Max(_go, 0);
        (_fogged, _dry, _out) = (fogged, danger == SuitDanger.Low && share <= 0, danger == SuitDanger.Out);
    }

    /// <summary>Runs <paramref name="seconds"/> on and gives the look then.</summary>
    public FogLook Advance(double seconds)
    {
        // Low again while it evaporates: the same patches fog over again.
        if (_go >= 0 && _fogged && (_go -= seconds / ComeS) <= 0) _go = -1;
        else if (_go >= 0 && !_fogged && (_go += seconds / EvaporateS) >= 1) (_go, _on, _depth) = (-1, 0, 0);
        if (_go < 0 && _fogged)
        {
            _on = Math.Min(1, _on + seconds / ComeS);
            double step = seconds / DeepenS;
            _depth = _depth < _depthTarget ? Math.Min(_depthTarget, _depth + step) : Math.Max(_depthTarget, _depth - step);
        }
        _dryS = _dry ? _dryS + seconds : 0;
        double last = Math.Clamp(_dryS / LastBreathS, 0, 1);
        double period = _out ? Calm : _dry ? GaspFirst + GaspSlows * last : _go >= 0 ? Evaporating : Calm + (Quick - Calm) * _depth;
        _phase = (_phase + seconds / period) % 1;
        if (_on <= 0) return default;
        // 0 at the end of a breath in, 1 at the end of a breath out; flattening as the last breath runs, still while out.
        double amp = _out ? 0 : _dry ? 1 - Smooth(0.1, 0.5, last) : 1, breath = (0.5 - 0.5 * Math.Cos(_phase * Math.PI * 2)) * amp;
        double on = 1 - Math.Pow(1 - _on, 3), go = Math.Max(0, _go);
        double reach = (0.1 + 0.3 * _depth + breath * (0.05 + 0.14 * _depth)) * on;
        return new FogLook(reach, go, 0.7 * breath * on * (1 - go));
    }

    private static double Smooth(double a, double b, double x)
    {
        double f = Math.Clamp((x - a) / (b - a), 0, 1);
        return f * f * (3 - 2 * f);
    }
}
