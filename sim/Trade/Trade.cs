using System.Text.Json.Serialization;
using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Knowledge;

namespace Stillwake.Sim.Trade;

/// <summary>
/// A capsule on its way from trade station <c>Machine</c>, <c>Left</c> ticks from coming back with <c>Credits</c> (2026-10-30-trade-capsule).
/// </summary>
public sealed record Flight(int Machine, int Left, int Credits);

/// <summary>Launches trade station <c>Machine</c>'s capsule with everything it holds that has a price.</summary>
public sealed record SendCapsule(int Machine) : Command;

/// <summary>The trade slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class TradeState
{
    /// <summary>The capsules away, oldest launch first.</summary>
    [JsonIgnore] internal List<Flight> Flights { get; set; } = [];
    /// <summary>Flights as saved: none written while none is away.</summary>
    [JsonInclude, JsonPropertyName("flights"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<Flight>? SavedFlights { get => Flights.Count > 0 ? Flights : null; set => Flights = value ?? []; }
}

/// <summary>
/// Selling goods for the ship's credits by capsule (2026-10-30-trade-capsule). One a surface, the credits the world's
/// <c>Knowledge</c>. A hero or a drone puts goods in; a hero sends them, or an auto trade station sends them itself.
/// </summary>
public sealed class TradeModule : Module<TradeState>
{
    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IKnowledgeOps _knowledge;

    public override string Id => "trade";

    public TradeModule(IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IKnowledgeOps knowledge)
    {
        _grid = grid;
        _items = items;
        _itemsOps = itemsOps;
        _knowledge = knowledge;
        Handles<SendCapsule>(c => SendProblem(c.Machine), c => Launch(c.Machine));
        System(Phase.Simulation, Fly);
    }

    protected override TradeState Init(Layout layout) => new();

    /// <summary>What trade station <paramref name="machine"/> holds is worth, in credits; what has no price counts for nothing.</summary>
    public int Worth(int machine)
    {
        int worth = 0;
        foreach (var held in _items.Crate(machine))
            if (held != null && Market.PriceOf(held.Item) is int price) worth += price * held.Count;
        return worth;
    }

    /// <summary>The capsule of trade station <paramref name="machine"/> while it is away; null while it is home.</summary>
    public Flight? FlightOf(int machine) => State.Flights.Find(f => f.Machine == machine);

    /// <summary>Why a capsule from <paramref name="machine"/> would be refused: what its button shows.</summary>
    public string? SendProblem(int machine)
    {
        if (_grid.MachineById(machine) is not { } m || !Machines.Get(m.Kind).Is(MachineTrait.Sells)) return "trade.notStation";
        if (FlightOf(machine) != null) return "trade.capsuleAway";
        return Worth(machine) > 0 ? null : "trade.nothingToSell";
    }

    /// <summary>Everything priced leaves its slots now, for what it is worth now; slots keep what they remember (<c>Items.Remembers</c>).</summary>
    private void Launch(int machine)
    {
        int credits = Worth(machine);
        var slots = _items.Crate(machine);
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] is { } held && Market.PriceOf(held.Item) != null) _itemsOps.TakeFrom(machine, i, held.Count);
        State.Flights.Add(new Flight(machine, Market.CapsuleFlight, credits));
    }

    /// <summary>Auto stations send what they hold, then every capsule flies a tick; one come back pays, its station gone or not.</summary>
    private void Fly()
    {
        foreach (var m in _grid.MachinesWith(MachineTrait.AutoSends))
            if (SendProblem(m.Id) == null) Launch(m.Id);
        for (int i = State.Flights.Count - 1; i >= 0; i--)
        {
            var flight = State.Flights[i];
            if (flight.Left > 1)
            {
                State.Flights[i] = flight with { Left = flight.Left - 1 };
                continue;
            }
            State.Flights.RemoveAt(i);
            _knowledge.Earn(flight.Credits);
        }
    }

    /// <summary>Sends from a trade station now and then, refused ones too.</summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(8) != 0) return null;
        var stations = _grid.MachinesWith(MachineTrait.Sells);
        int machine = stations.Count > 0 && rng.Below(4) != 0 ? stations[(int)rng.Below((uint)stations.Count)].Id : (int)rng.Below(64);
        return new SendCapsule(machine) { From = from };
    }
}
