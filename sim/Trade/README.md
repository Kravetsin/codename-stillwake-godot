# Trade

## Files

| File | Purpose |
| --- | --- |
| `Trade.cs` | Selling goods for the ship's credits at trade stations. |

Selling goods for the ship's credits ([trade-and-tech](../../docs/decisions/2026-10-30-trade-and-tech.md)). The prices are
`Content/Market.cs`.

- **Depends on:** `Grid` (`IGridQueries.MachinesWith`), `Items` (`IItemsQueries.Crate`, `IItemsOps.TakeFrom`), `Knowledge`
  (`IKnowledgeOps.Earn`).
- **State** (`TradeState`): `Ticks`, ticks run on this surface, which the sales beat by. Saved.

## Rules

- A machine with `MachineTrait.Sells` is a trade station. Every `Market.SellEvery` ticks, in the simulation phase, after
  logistics, each one in id order sells one unit from its first slot holding what has a price, for that price.
- A price never falls, however much is sold.
- What has no price (gear, what is never carried) stays in its slot, unsold, and drones never bring it.
- Its input slots remember what they held (`Items.Remembers`), so drones keep a slot seeded by hand stocked. A hand taking
  it out forgets. A station holding and remembering nothing wants nothing.
- The drone switch stops drones, not selling.

## API

- **Queries:** `SellingFrom(machine)`, the slot it sells from, for its look.
- **Commands:** none, so no fuzz generator: a hero or a drone puts goods in (`Items.Move`).

## Invariants

- Credits only ever grow here, by the price of a unit taken out of a station.

## Deliberately not handled

- Falling prices when a market is flooded: the economy is one world's, and limiting the player is no fun.
- Buying anything but blueprints. Coordinates, upgrades and cosmetics are the next users of credits.
- No machines stand on a planet, so a planet's goods are carried up to sell.
