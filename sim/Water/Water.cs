using System.Text.Json.Serialization;
using Stillwake.Content;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Power;

namespace Stillwake.Sim.Water;

/// <summary>Pours a bottle of water from the sender's inventory into cistern <c>Cistern</c>.</summary>
public sealed record PourWater(int Cistern) : Command;

/// <summary>The water a cistern holds, by its machine id.</summary>
internal sealed class Held
{
    public int Machine { get; set; }
    public int Amount { get; set; }
}

/// <summary>An electrolyser's work toward its next unit: a unit is <c>Air.LeakEvery × 1000</c>.</summary>
internal sealed class Work
{
    public int Machine { get; set; }
    public int Part { get; set; }
}

/// <summary>The water's slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class WaterState
{
    /// <summary>The water in each cistern holding any, by machine id, in id order.</summary>
    [JsonInclude] internal List<Held> Cisterns { get; set; } = [];
    /// <summary>Each electrolyser's work toward its next unit, by machine id, in id order.</summary>
    [JsonInclude] internal List<Work> Making { get; set; } = [];
}

/// <summary>What any module may read of the water.</summary>
public interface IWaterQueries
{
    /// <summary>The water in cistern <paramref name="machine"/>; none for one never filled, or no cistern.</summary>
    int WaterIn(int machine);
    /// <summary>Why peer <paramref name="peer"/>'s bottle would not go into machine <paramref name="machine"/>: what the client shows first.</summary>
    string? PourProblem(int machine, int peer);
    /// <summary>Why electrolyser <paramref name="machine"/> makes nothing, as an i18n key: no water behind it, its tanks full, or no power; null while it works.</summary>
    string? Stalled(int machine);
}

/// <summary>
/// Water (water): what cisterns hold, poured in by the bottle, and the electrolysers that split it into the liquid oxygen
/// of the tanks on their output's network, through Atmosphere. Run after Atmosphere: what it makes is breathed from next tick.
/// </summary>
public sealed partial class WaterModule : Module<WaterState>, IWaterQueries
{
    private static readonly IReadOnlyList<(string Item, int Count)> Bottle = [(Content.Water.Item, 1)];
    private const int Unit = Air.LeakEvery * 1000;

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IPipesQueries _pipes;
    private readonly IPowerQueries _power;
    private readonly IAtmosphereQueries _air;
    private readonly IAtmosphereOps _airOps;

    public override string Id => "water";

    public WaterModule(
        IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IPipesQueries pipes, IPowerQueries power,
        IAtmosphereQueries air, IAtmosphereOps airOps)
    {
        _grid = grid;
        _items = items;
        _pipes = pipes;
        _power = power;
        _air = air;
        _airOps = airOps;
        Handles<PourWater>(c => PourProblem(c.Cistern, c.From), c =>
        {
            itemsOps.Take(c.From, Bottle);
            var held = State.Cisterns.Find(h => h.Machine == c.Cistern);
            if (held != null) held.Amount += Content.Water.Bottle;
            else
            {
                State.Cisterns.Add(new Held { Machine = c.Cistern, Amount = Content.Water.Bottle });
                State.Cisterns.Sort((a, b) => a.Machine.CompareTo(b.Machine));
            }
        });
        // A cistern taken apart takes its water with it: a few cisterns at most, looked at every tick.
        System(Phase.Construction, () => State.Cisterns.RemoveAll(h => _grid.MachineById(h.Machine) == null));
        System(Phase.Simulation, Electrolyse);
    }

    protected override WaterState Init(Layout layout) => new();

    public int WaterIn(int machine) => State.Cisterns.Find(h => h.Machine == machine)?.Amount ?? 0;

    public string? PourProblem(int machine, int peer)
    {
        if (_grid.MachineById(machine) is not { } m || !Machines.Get(m.Kind).Is(MachineTrait.HoldsWater)) return "water.noCistern";
        if (WaterIn(machine) + Content.Water.Bottle > Content.Water.CisternCapacity) return "water.full";
        return _items.ShortOf(peer, Bottle);
    }

    public string? Stalled(int machine)
    {
        if (_grid.MachineById(machine) is not { } m || !Machines.Get(m.Kind).Is(MachineTrait.Electrolyses)) return null;
        var (cisterns, tanks) = Ends(m);
        if (WaterOf(cisterns) == 0) return "water.noWater";
        if (Room(tanks) == 0) return "water.tanksFull";
        return _power.Satisfaction(machine) == 0 ? "water.noPower" : null;
    }

    /// <summary>
    /// Each electrolyser, in id order, works toward its next unit by <c>Water.ElectrolyserRate</c> × its power's satisfaction,
    /// but only while the cisterns behind it hold water and the tanks before it have room; else its work stands. Each unit
    /// takes one of water from those cisterns in id order and puts one of liquid into those tanks in id order.
    /// </summary>
    private void Electrolyse()
    {
        var making = new List<Work>();
        var was = State.Making;
        int next = 0;
        foreach (var m in _grid.MachinesWith(MachineTrait.Electrolyses))
        {
            while (next < was.Count && was[next].Machine < m.Id) next++;
            int part = next < was.Count && was[next].Machine == m.Id ? was[next].Part : 0;
            var (cisterns, tanks) = Ends(m);
            int water = WaterOf(cisterns), room = Room(tanks);
            if (water > 0 && room > 0)
            {
                part += Content.Water.ElectrolyserRate * _power.Satisfaction(m.Id);
                // Rounded down: what water or room leaves unmade stays in the work.
                int made = Math.Min(part / Unit, Math.Min(water, room));
                part -= made * Unit;
                int left = made;
                foreach (int cistern in cisterns)
                {
                    if (left == 0) break;
                    var held = State.Cisterns.Find(h => h.Machine == cistern)!;
                    int taken = Math.Min(held.Amount, left);
                    held.Amount -= taken;
                    left -= taken;
                }
                left = made;
                foreach (int tank in tanks)
                    if (left > 0) left -= _airOps.Fill(tank, left);
            }
            making.Add(new Work { Machine = m.Id, Part = part });
        }
        State.Making = making;
        State.Cisterns.RemoveAll(h => h.Amount == 0);
    }

    /// <summary>
    /// The cisterns whose output stands on electrolyser <paramref name="m"/>'s input's network, and the tanks whose input stands
    /// on its output's, each in id order.
    /// </summary>
    private (List<int> Cisterns, List<int> Tanks) Ends(Machine m)
    {
        int? input = null, output = null;
        var cells = _grid.PortsOf(m);
        var ports = Machines.Get(m.Kind).Ports;
        for (int i = 0; i < cells.Count; i++)
            if (ports[i].Way == Way.In) input = _pipes.NetworkAt(cells[i]);
            else output = _pipes.NetworkAt(cells[i]);
        return (On(MachineTrait.HoldsWater, Way.Out, input), On(MachineTrait.Gas, Way.In, output));
    }

    /// <summary>The machines of <paramref name="trait"/> with a port of <paramref name="way"/> on <paramref name="network"/>, in id order.</summary>
    private List<int> On(MachineTrait trait, Way way, int? network)
    {
        var found = new List<int>();
        if (network == null) return found;
        foreach (var m in _grid.MachinesWith(trait))
        {
            var cells = _grid.PortsOf(m);
            var ports = Machines.Get(m.Kind).Ports;
            for (int i = 0; i < cells.Count; i++)
                if (ports[i].Way == way && _pipes.NetworkAt(cells[i]) == network)
                {
                    found.Add(m.Id);
                    break;
                }
        }
        return found;
    }

    private int WaterOf(List<int> cisterns)
    {
        int sum = 0;
        foreach (int c in cisterns) sum += WaterIn(c);
        return sum;
    }

    private int Room(List<int> tanks)
    {
        int sum = 0;
        foreach (int t in tanks) sum += Air.TankCapacity - _air.OxygenIn(t);
        return sum;
    }
}
