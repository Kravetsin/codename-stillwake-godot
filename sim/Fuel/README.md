# Fuel

## Files

| File | Purpose |
| --- | --- |
| `Fuel.cs` | Fuel: every machine's biofuel store, its hopper, its line, `DrawFuel`, how long it lasts, the ops others fill and burn by. |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |

Biofuel as a liquid ([fuel-liquid](../../docs/decisions/2026-11-16-fuel-liquid.md)): a can is a litre, poured into the stores of the
machines that hold it, piped on from store to store, drawn out as cans. The numbers are `Content/Fuel.cs` and the rows' `FuelHolds`.

- **Depends on:** `Grid` (`MachinesWith`, `MachineById`), `Items` (`Crate`, `Consume`, `Give`, `FullFor`), `Pipes` (`NetworkOf`,
  `MachinesOn`), `Power` (`Rest`). Made before Crafting, Furnace, Landing and Building, which take `IFuelQueries`/`IFuelOps`; run
  and saved last.
- **State** (`FuelState`): `Stores` — each store holding any, in millilitres, by machine id, in id order (saved; Core's `Stored`,
  as Water's). Derived, never saved or hashed: `Outflows` and `Window`, what each store gave out, for `Lasts`.

## Rules

- **Stores.** A machine row with `FuelHolds` (litres) has `MachineTrait.HoldsFuel` and a store of that many litres: the fuel tank
  100, the crew shuttle 200, the drop pod, the biofuel generator, the engine and the fermenter 20. A machine taken apart, or no
  longer a store, loses its store in construction (Building refuses one holding over a litre).
- **Hoppers.** A store's `SlotTakes` slot takes cans only; each tick, after the lines, each store in id order absorbs one can when
  a litre fits. Drones keep a hopper stocked as they keep a tank's. The fermenter has none.
- **Drawing.** `DrawFuel(Machine, Count)` gives the sender `Count` cans, one at least, all or none. Refused, nothing changed:
  `fuel.noStore`, `fuel.low` (less than `Count` litres), `items.full`.
- **Lines** (tanks-in-a-line, `Stores.Pour`): each store in id order holding fuel gives up to `Fuel.Flow` (20 ml) a tick of its own
  to the stores whose fuel input stands on its fuel output's network, in id order, as each has room, never to itself; a later
  store pours on in the same tick. A store over its capacity (an old save) takes nothing till it drains below.
- **Filling and burning** are the others' rules: Crafting's fermenter makes a litre into its store (`Fill`), Furnace's biofuel
  generator pays a tick's share of a litre (`Take`), Landing burns a descent's 5 l and carries the rest (`Take`, `TakeAll`, `Fill`).
- **The engine** rests (`IPowerOps.Rest`) while its store is empty: a store that draws power and is no station.
- **How long it lasts:** `Take` and the lines count what each store gives out; every `Fuel.RateWindow` (100) ticks that becomes its
  rate. `Lasts(machine)` is its fuel over that rate, in ticks, rounded down; null while it gives none. Draws by hand do not count.

## API

- **Queries** (`IFuelQueries`): `FuelIn`, `RoomIn`, `DrawProblem`, `Lasts`.
- **Operations** (`IFuelOps`): `Fill` (what fit), `Take` (what it took), `TakeAll`.
- **Commands:** `DrawFuel(Machine, Count)`.
- **Fuzz:** draws one to three cans, mostly from a store the grid has, else from ids 0–5.

## Invariants

- A store holds between nothing and its capacity, but one loaded fuller from an old save; only stores holding any are listed.
- With no machine taken apart and no pod laid as its cost, the fuel in stores, hoppers, hands and flights changes only by what
  the fermenter makes and what fires and descents burn (`FuelTests`' property).

## Deliberately not handled

- No pumps or power for flow, no spills, leaks or fires. A part litre is never drawn as a can.
- `Lasts` reads nothing for a window after a load, and peers may differ until then: it is display only.
