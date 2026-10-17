# Furnace

Smelting ([0023](../../docs/decisions/0023-a-furnace-burns-fuel.md)): a furnace burns fuel and breathes
its room's air, and turns the salvage in its input into what it yields. The way caught debris becomes
materials. Ported from `packages/sim/src/modules/furnace`; its rules hold here unchanged.

- **Depends on:** `Grid` (`IGridQueries`: which machines are furnaces, `MachinesOf`, and their cells), `Items`
  (`IItemsQueries`: their slots, `CanSmelt`; `IItemsOps`: `Smelt` and `Consume`), `Atmosphere`
  (`IAtmosphereQueries`: `AiredAt`; `IAtmosphereOps`: `Breathe`, the air their fire breathes).
- **State** (`FurnaceState`, members internal): `Fires` — each furnace burning or part-way through an
  item, `Fire(Furnace, Fuel, Item, Progress)` by machine id in id order: the ticks of fire left and
  the item it is at work on (null for none), with its work in ticks of fire. Saved; nothing is
  derived.
- **Files:** `Furnace.cs` — the module, its state and records.
- **Runs** last in `Registry.All`, as in TypeScript's `MODULES`.

## Rules

- A furnace's slots live in `Items` (`Machines` `furnace`): its input, its fuel
  (`Content.Furnace.FuelSlot`), then its outputs; it is opened with E as a crate is.
- Each tick, in the simulation phase, in machine id order:
  - **lit** only when it has none, with salvage in its input (an item with `SmeltTicks`) and room for
    its yields, fuel in its fuel slot (an item with `BurnTicks`), and air in a cell of it: one fuel
    is used up (`IItemsOps.Consume`), worth its `BurnTicks`;
  - **burning**, it breathes `Content.Furnace.Breath` from its first aired cell every
    `Content.Furnace.BreathEvery` ticks of fuel (`IAtmosphereOps.Breathe`, as a hero does); given
    less, its fire goes out and the fuel left is lost. Otherwise it burns a tick of fuel, smelting or
    not, and smelts a tick: at `SmeltTicks`, one of the input becomes its yields (`IItemsOps.Smelt`).
- Work on an item is dropped once the input holds another item or none, and begins again.

## API

- **Queries:** `At(id)` — furnace `id` as its panel shows it, a `Smelting(Burning, Item, Progress,
  Work, Problem)`: whether it burns; with salvage in its input, also what it smelts, how far, out of
  how much work, and why it stands still: `furnace.full`, `furnace.noFuel` or `furnace.noAir`. With
  none, only `Burning` (TypeScript's `{ burning }`). No query interface: no module reads it, only the
  client.
- No commands, so no fuzz generator: a hero works a furnace by moving items in and out (`Items.Move`).

## Invariants

- Items change only by moves, `Consume` of one fuel as a fire is lit, and `Smelt`, which takes one
  item from the input and adds its yields to the outputs (`FurnaceTests`, beyond TypeScript's).

## Deliberately not handled

- A fuel slot takes anything, and an input anything: what does not burn or smelt only sits there.
- A fire with nothing left to smelt burns its fuel away, as in Minecraft.
- No queue, no recipes beyond `Yields`, no automation: what comes out stays until a hero takes it.
