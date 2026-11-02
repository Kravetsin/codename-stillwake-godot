using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Water;

/// <summary>
/// Out of every overlay, with no card in hand, the pointer on a cistern tells its bottles, on an electrolyser what it does or
/// why it stands (<see cref="WaterShows.Says"/>), read from the sim as the world runs. Off them, only the tip it set is taken
/// away: another may be telling of what the pointer is on now.
/// </summary>
public sealed class WaterTip(Ui ui)
{
    private (int Machine, int Tick) _told = (-1, -1);
    private IReadOnlyList<string>? _said;

    public void Frame(Registry m, int tick, (double X, double Y)? pointer)
    {
        if (ui.OverlayOn != null)
        {
            _told = (-1, -1);
            return;
        }
        int width = m.Grid.Width, height = m.Grid.Height;
        var machine = ui.Hand.Get() != Holding.Card && pointer is { } p && p.X >= 0 && p.Y >= 0 && p.X < width && p.Y < height
            ? m.Grid.MachineAt((int)p.Y * width + (int)p.X)
            : null;
        var spec = machine == null ? null : Stillwake.Content.Machines.Get(machine.Kind);
        if (machine == null || spec == null || !(spec.Is(Stillwake.Content.MachineTrait.HoldsWater) || spec.Is(Stillwake.Content.MachineTrait.Electrolyses)))
        {
            if (_told.Machine < 0) return;
            if (_said != null && ui.WorldTip.Get() is { } now && now.SequenceEqual(_said)) ui.WorldTip.SetLines(null);
            (_told, _said) = ((-1, -1), null);
            return;
        }
        if (_told != (machine.Id, tick)) _said = WaterShows.Says(spec, m.Water.WaterIn(machine.Id), m.Water.Stalled(machine.Id));
        _told = (machine.Id, tick);
        ui.WorldTip.SetLines(_said);
    }
}
