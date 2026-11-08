using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Shared;

/// <summary>
/// Out of every overlay, with no card in hand, the pointer on a machine tells its name, what the first of the features'
/// <see cref="Speaker"/>s says of it, and what it draws or makes (<see cref="MachineSays"/>), read from the sim once a tick: a
/// cistern's bottles, a tank's liquid, a vaporizer's air, a crusher's draw before it runs. Off them, only the tip it set is
/// taken away: another may be telling of what the pointer is on now.
/// </summary>
public sealed class MachineTips(Ui ui, params MachineTips.Speaker[] speakers)
{
    /// <summary>What a feature says of <paramref name="machine"/> in the tip's lines, read from <paramref name="m"/>; null for one it does not speak of.</summary>
    public delegate IReadOnlyList<TipLine>? Speaker(Registry m, Machine machine, MachineSpec spec);

    private (int Machine, int Tick) _told = (-1, -1);
    private IReadOnlyList<TipLine>? _said;

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
        var said = machine == null ? null
            : _told == (machine.Id, tick) ? _said
            : Say(m, machine, Machines.Get(machine.Kind));
        if (machine == null || said == null)
        {
            if (_said != null && ui.WorldTip.Get() is { } now && now.SequenceEqual(_said)) ui.WorldTip.Set(null);
            (_told, _said) = ((-1, -1), null);
            return;
        }
        (_told, _said) = ((machine.Id, tick), said);
        ui.WorldTip.SetLines(said);
    }

    private IReadOnlyList<TipLine>? Say(Registry m, Machine machine, MachineSpec spec) =>
        MachineSays.Of(machine.Kind, speakers.Select(say => say(m, machine, spec)).FirstOrDefault(lines => lines != null),
            MachineSays.Power(spec, m.Power.Draw(machine.Id)));
}
