using System.Collections.Generic;
using CrewSpec = Stillwake.Content.Crew;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Another hero breathing a canister, as this peer sees it in the sim's state (0047): nothing tells a client that a
/// friend sent <c>Refill</c>, but its suit jumps by more than a breath of room air and the pod's together can add, awake
/// before and after. A canister into a suit within that much of full shows nothing: accepted, it rises as a breath does.
/// Plain C#: read each frame with every other hero.
/// </summary>
public sealed class CanistersSeen
{
    /// <summary>The most a suit rises in a frame without a canister: one breath's room refill and the pod's.</summary>
    public const int MostByAir = CrewSpec.Refill + CrewSpec.PodRefill;

    private readonly Dictionary<int, (int Suit, int Out)> _last = [];
    private readonly HashSet<int> _seen = [];

    /// <summary>
    /// Whether hero <paramref name="id"/>, now with <paramref name="suit"/> units and <paramref name="out"/> ticks passed out,
    /// breathed a canister since it was last read. A hero read the first time breathed none.
    /// </summary>
    public bool Breathed(int id, int suit, int @out)
    {
        _seen.Add(id);
        bool had = _last.TryGetValue(id, out var last);
        _last[id] = (suit, @out);
        return had && last.Out == 0 && @out == 0 && suit - last.Suit > MostByAir;
    }

    /// <summary>Forgets every hero not read since the last call: one gone or left, read again later, starts afresh.</summary>
    public void Sweep()
    {
        foreach (int id in new List<int>(_last.Keys))
            if (!_seen.Contains(id)) _last.Remove(id);
        _seen.Clear();
    }
}
