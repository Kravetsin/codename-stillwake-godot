using System.Text.Json.Serialization;
using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Knowledge;

namespace Stillwake.Sim.Trade;

/// <summary>The trade slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class TradeState
{
    /// <summary>Ticks run on this surface: a module has no clock, and the sales beat by it.</summary>
    [JsonInclude] internal int Ticks { get; set; }
}

/// <summary>
/// Selling goods for the ship's credits at trade stations (2026-10-30-trade-and-tech). One a surface, the credits the world's
/// <c>Knowledge</c>. No commands: a hero or a drone puts goods in, and they sell by themselves.
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
        System(Phase.Simulation, Sell);
    }

    protected override TradeState Init(Layout layout) => new();

    /// <summary>The slot trade station <paramref name="machine"/> sells from: its first holding what has a price; null for none.</summary>
    public int? SellingFrom(int machine)
    {
        var slots = _items.Crate(machine);
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] is { } held && Market.PriceOf(held.Item) != null) return i;
        return null;
    }

    /// <summary>On the beat, every trade station in id order sells one unit from <see cref="SellingFrom"/>.</summary>
    private void Sell()
    {
        if (++State.Ticks % Market.SellEvery != 0) return;
        foreach (var m in _grid.MachinesWith(MachineTrait.Sells))
            if (SellingFrom(m.Id) is int slot && _itemsOps.TakeFrom(m.Id, slot, 1) is { } sold)
                _knowledge.Earn(Market.PriceOf(sold.Item)!.Value);
    }
}
