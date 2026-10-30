using Stillwake.Content;

namespace Stillwake.Sim.Hazards;

/// <summary>
/// A magnetic flare starts now, from the dev panel's button (2026-10-30-magnetic-flare): it strikes on the <c>Ticks</c>th tick, the one it
/// applies on counted first, so 1 strikes at once; its warning first when that is its place's <c>Warning</c>. In any world, armed or not.
/// </summary>
public sealed record FlareNow(int Ticks) : Command;

public sealed partial class HazardsModule
{
    private readonly List<int> _candidates = [], _ends = [];

    /// <summary>The flares of this place, or the starting space's where it has none: what a flare started by hand strikes with.</summary>
    public MagneticFlares.Flare FlareRules => Rules.Flares ?? MagneticFlares.StartingSpace;

    /// <summary>Whether a flare's warning runs: the aurora, the lamps flickering and the cables glowing till it strikes.</summary>
    public bool FlareComing => State.FlareIn > 0 && State.FlareIn <= FlareRules.Warning;

    /// <summary>Ticks of the warning left before the flare strikes, for the aurora's ramp; 0 while none is coming.</summary>
    public int FlareLeft => FlareComing ? State.FlareIn : 0;

    private string? FlareNowProblem(FlareNow c) =>
        c.Ticks < 1 ? "hazards.badTicks" : FlareComing ? "hazards.flareComing" : null;

    /// <summary>
    /// A tick of the flares: one drawn counts down whether or not they are armed, and strikes as it reaches 0; then, with none drawn,
    /// the next is drawn where the place has flares, the world is not calm and the oxygen generator is known. Not counted down the tick drawn.
    /// </summary>
    private void Flare()
    {
        var s = State;
        if (s.FlareIn > 0 && --s.FlareIn == 0) StrikeFlare(FlareRules);
        if (s.FlareIn != 0 || s.Calm || Rules.Flares is not { } flare || !_research.Knows(MagneticFlares.ArmedBy)) return;
        s.FlareIn = flare.EveryMin + (int)Random.Below((uint)(flare.EveryMax - flare.EveryMin + 1));
    }

    /// <summary>
    /// A flare strikes: of the <c>n</c> powered machines a cable ends on and not broken, in id order, it draws a percent between the place's
    /// least and most, then breaks <c>ceil(n × percent / 100)</c> of them, each drawn from those left and taken out. None, nothing drawn.
    /// </summary>
    private void StrikeFlare(MagneticFlares.Flare flare)
    {
        // The cables as they lie now, not the networks found last derivation: one cut or laid this tick counts.
        _ends.Clear();
        foreach (var c in _power.Cables) { _ends.Add(c.A); _ends.Add(c.B); }
        _ends.Sort();
        _candidates.Clear();
        foreach (var m in _grid.Machines)
            if (Machines.Get(m.Kind).Powered && !_power.IsBroken(m.Id) && _ends.BinarySearch(m.Cell) >= 0) _candidates.Add(m.Id);
        int n = _candidates.Count;
        if (n == 0) return;
        int percent = flare.PercentMin + (int)Random.Below((uint)(flare.PercentMax - flare.PercentMin + 1));
        // Rounded up: every flare breaks one machine at least.
        int k = (n * percent + 99) / 100;
        for (int i = 0; i < k; i++)
        {
            int at = (int)Random.Below((uint)_candidates.Count);
            _powerOps.Break(_candidates[at]);
            _candidates.RemoveAt(at);
        }
    }
}
