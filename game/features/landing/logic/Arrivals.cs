using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// Which drop pods have just come down on the surface this player sees, and how long ago (the descent's slice 2): a pod that is
/// seated now and was not on this surface last frame, or this player's own pod the frame its flight ended. Off the ship it is held
/// back and drawn coming down; aboard, docked back from a planet, it is only heard (<see cref="DockedSince"/>).
/// The sim has the pod standing at once; the client holds it back from the grid for <see cref="FlightTimeline.Release"/> seconds
/// and draws it coming down. Time here is frame time: the planet's opening freezes the ticks the touchdown would play in.
/// </summary>
public sealed class Arrivals
{
    /// <summary>The longest frame counted: the freeze and the planet's first drawing would otherwise eat the touchdown.</summary>
    public const double MostDelta = 0.05;

    private readonly Dictionary<int, double> _since = [], _docked = [];
    private HashSet<int> _last = [];
    private int? _surface;

    /// <summary>Changes whenever the pods held change: what a view drawing the grid redraws on.</summary>
    public int Version { get; private set; }

    /// <summary>Whether pod <paramref name="pod"/> is still coming down: kept out of the grid's drawing.</summary>
    public bool Holds(int pod) => _since.ContainsKey(pod);

    /// <summary>Seconds since pod <paramref name="pod"/> was seen arriving; null for one standing.</summary>
    public double? SecondsSince(int pod) => _since.TryGetValue(pod, out double s) ? s : null;

    /// <summary>
    /// Seconds since pod <paramref name="pod"/> docked aboard, back from a planet: not drawn moving, only heard, for
    /// <see cref="FlightTimeline.DockS"/>; null for none.
    /// </summary>
    public double? DockedSince(int pod) => _docked.TryGetValue(pod, out double s) ? s : null;

    /// <summary>
    /// A frame <paramref name="delta"/> seconds on, on <paramref name="surface"/> (<paramref name="aboard"/> the ship), its pods and
    /// whether each is seated. <paramref name="ownLanded"/> is the pod this player sits in the frame its own flight ended;
    /// <paramref name="newWorld"/> a load or a resync: nothing arrives in a world just made.
    /// </summary>
    public void Frame(int surface, bool aboard, IReadOnlyList<(int Pod, bool Seated)> pods, int? ownLanded, bool newWorld, double delta)
    {
        int before = _since.Count;
        bool switched = surface != _surface;
        _surface = surface;
        var here = pods.ToDictionary(p => p.Pod, p => p.Seated);
        if (newWorld)
        {
            _since.Clear();
            _docked.Clear();
        }
        else
        {
            delta = Math.Min(delta, MostDelta);
            // One gone, or left by its rider, or long enough down, is let go.
            foreach (int pod in _since.Keys.ToList())
            {
                double s = _since[pod] + delta;
                if (switched || !here.GetValueOrDefault(pod) || s >= FlightTimeline.Release) _since.Remove(pod);
                else _since[pod] = s;
            }
            // Docked aboard: heard for a moment, never held back, as nothing is drawn moving there.
            foreach (int pod in _docked.Keys.ToList())
            {
                double s = _docked[pod] + delta;
                if ((switched && pod != ownLanded) || !here.ContainsKey(pod) || s >= FlightTimeline.DockS) _docked.Remove(pod);
                else _docked[pod] = s;
            }
            foreach (var (pod, seated) in pods)
            {
                bool came = seated && (switched ? pod == ownLanded : !_last.Contains(pod) || pod == ownLanded);
                if (!came) continue;
                if (!aboard && !_since.ContainsKey(pod)) _since[pod] = 0;
                else if (aboard && !_docked.ContainsKey(pod)) _docked[pod] = 0;
            }
        }
        _last = [.. here.Keys];
        if (_since.Count != before || newWorld) Version++;
    }
}
