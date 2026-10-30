# Trade

## Files

| File | Purpose |
| --- | --- |
| `Trade.cs` | Selling goods for the ship's credits by capsule from trade stations. |

Selling goods for the ship's credits by capsule ([trade-capsule](../../docs/decisions/2026-10-30-trade-capsule.md)). The prices
and the flight are `Content/Market.cs`.

- **Depends on:** `Grid` (`IGridQueries.MachinesWith`, `MachineById`), `Items` (`IItemsQueries.Crate`, `IItemsOps.TakeFrom`),
  `Knowledge` (`IKnowledgeOps.Earn`).
- **State** (`TradeState`): `Flights`, the capsules away, what each carries and how long it has left. Saved, omitted when empty.

## Rules

- A machine with `MachineTrait.Sells` is a trade station. `SendCapsule` takes everything priced from its slots at once, however
  much, for what it is worth at that moment, and `Market.CapsuleFlight` ticks on, launch tick counted, earns that sum.
- One capsule a station. Slots fill as usual while it is away, for the next one. Two are open at first, the rest by upgrades
  (`Items.SlotOpen`).
- `MachineTrait.AutoSends`: the station launches by itself in the simulation phase whenever `SendCapsule` would be taken, so
  whatever drones brought goes on the tick after the last capsule came home.
- A price never falls, however much is sold.
- What has no price (gear, what is never carried) stays in its slot and counts for nothing, and drones never bring it.
- Its input slots remember what they held (`Items.Remembers`), a launch included, so drones keep a slot seeded by hand stocked.
  A hand taking it out forgets. A station holding and remembering nothing wants nothing.
- The drone switch stops drones, not capsules.

## API

- **Queries:** `Worth(machine)`, what it holds is worth; `FlightOf(machine)`, its capsule away; `SendProblem(machine)`.
- **Commands:** `SendCapsule(Machine)` (`trade.notStation`, `trade.capsuleAway`, `trade.nothingToSell`).

## Invariants

- Credits only ever grow here, by exactly what left a station's slots was worth, once its capsule is home.

## Deliberately not handled

- A capsule lost or intercepted, and a limit on what one carries: limiting the player is no fun.
- A station taken apart or broken while its capsule is away: the capsule still pays, there is nothing to refuse.
- Falling prices when a market is flooded: the economy is one world's, and limiting the player is no fun.
- Buying anything but blueprints. Coordinates, upgrades and cosmetics are the next users of credits.
- No machines stand on a planet, so a planet's goods are carried up to sell.
