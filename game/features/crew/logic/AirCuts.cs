using Pens = Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The air a running tool works (tool-reach), in the arms' frame, the shoulder at the origin, in pixels: a drill's or grinder's
/// zone, eased in as it starts and out after it stops, what the grinder's haze is drawn over (blade-trail). A swung blade's air is
/// its trail (<see cref="BladeTrail"/>), not this.
/// </summary>
public sealed class AirCuts
{
    /// <summary>
    /// A running tool's zone this frame: a <paramref name="Disc"/>'s forward half round <paramref name="At"/>, <paramref name="Size"/>
    /// its radius, or a cone from <paramref name="At"/> along <paramref name="Toward"/> (a unit), <paramref name="Size"/> long.
    /// </summary>
    public readonly record struct Zone(bool Disc, (double X, double Y) At, (double X, double Y) Toward, double Size);

    private double _ranFor, _off;
    private Zone? _zone;

    /// <summary>How much a running tool's zone shows: eased in over <see cref="WorkLooks.ZoneIn"/> ms, out over <see cref="WorkLooks.ZoneOut"/> after it stops.</summary>
    private double On => _zone == null ? 0
        : _off > 0 ? 1 - Pens.Clamp(_off / WorkLooks.ZoneOut) : Pens.Clamp(_ranFor / WorkLooks.ZoneIn);

    /// <summary>The running tool's zone as it shows now and how much, none while none runs.</summary>
    public (Zone Zone, double On)? Working => _zone is { } z && On > 0 ? (z, On) : null;

    /// <summary><paramref name="elapsedMs"/> on, a running tool's <paramref name="zone"/>, none while none runs.</summary>
    public void Step(double elapsedMs, Zone? zone)
    {
        if (zone is { } z)
        {
            _ranFor = _off > 0 ? 0 : _ranFor + elapsedMs;
            (_zone, _off) = (z, 0);
        }
        else if (_zone != null)
        {
            _off += elapsedMs;
            _ranFor += elapsedMs;
            if (_off >= WorkLooks.ZoneOut) (_zone, _ranFor, _off) = (null, 0, 0);
        }
    }
}
