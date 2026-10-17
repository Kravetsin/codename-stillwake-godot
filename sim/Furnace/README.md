# Furnace

Fire ([0023](../../docs/decisions/0023-a-furnace-burns-fuel.md)): a furnace or a burner generator burns
fuel and breathes its room's air. A furnace turns the salvage in its input into what it yields, the way
caught debris becomes materials; a burner generator makes power while it burns
([0045](../../docs/decisions/0045-a-burner-generator-shares-the-furnaces-fire.md)). Ported from
`packages/sim/src/modules/furnace`; its rules hold here unchanged, but a fire fed in time relights at
once.

- **Depends on:** `Grid` (`IGridQueries`: which machines are furnaces and generators, `MachinesOf`,
  and their cells), `Items` (`IItemsQueries`: their slots, `CanSmelt`; `IItemsOps`: `Smelt` and
  `Consume`), `Atmosphere` (`IAtmosphereQueries`: `AiredAt`; `IAtmosphereOps`: `Breathe`, the air their
  fire breathes), `Power` (`IPowerOps`: `Rest`, a cold generator makes nothing).
- **State** (`FurnaceState`, members internal): `Fires` — each furnace or generator burning, or furnace
  part-way through an item, `Fire(Machine, Fuel, Item, Progress)` by machine id in id order: the
  ticks of fire left and the item it is at work on (null for none, always for a generator), with its
  work in ticks of fire. Saved; nothing is derived. Format 8 renamed a fire's `furnace` to `machine`.
- **Files:** `Furnace.cs` — the module, its state and records.
- **Runs** last in `Registry.All`, as in TypeScript's `MODULES`.

## Rules

- A machine's slots live in `Items`: the furnace's input, its fuel, then its outputs; the generator's
  one fuel slot. Which slot burns is the machine's `FuelSlot` in `Content.Machines`. Both are opened
  with E as a crate is.
- Each tick, in the simulation phase, furnaces and generators together in machine id order:
  - **lit** only when it has none, with fuel in its fuel slot (an item with `BurnTicks`) and air in a
    cell of it; a furnace also only with salvage in its input (an item with `SmeltTicks`) and room for
    its yields. One fuel is used up (`IItemsOps.Consume`), worth its `BurnTicks`;
  - **burning**, it breathes `Content.Furnace.Breath` from its first aired cell every
    `Content.Furnace.BreathEvery` ticks of fuel (`IAtmosphereOps.Breathe`, as a hero does); given
    less, its fire goes out and the fuel left is lost. Otherwise it burns a tick of fuel, smelting or
    not; a furnace smelts a tick: at `SmeltTicks`, one of the input becomes its yields
    (`IItemsOps.Smelt`). Its last tick of fuel burnt, it is lit again on the same tick if it can be,
    so a fire fed in time never goes out between two fuels.
- Work on an item is dropped once the input holds another item or none, and begins again.
- **A generator rests while its fire is out:** in the construction phase, before power's derivation,
  each one not burning is told to rest (`IPowerOps.Rest`), one burning to work. `Power` counts no
  resting maker, so a generator makes its `Makes` from the tick after it is lit to the tick after it
  goes out, and one just built never makes a tick. It burns while it has fuel, whatever its network
  draws.

## API

- **Queries:** `At(id)` — furnace or generator `id` as its panel shows it, a `Smelting(Burning, Item,
  Progress, Work, Problem)`: whether it burns, and why it stands still: `furnace.full` (a furnace
  only), `furnace.noFuel` or `furnace.noAir`; for a furnace with salvage in its input, also what it
  smelts, how far, out of how much work. A furnace with nothing to smelt says only `Burning`
  (TypeScript's `{ burning }`). No query interface: no module reads it, only the client.
- No commands, so no fuzz generator: a hero works a fire by moving items in and out (`Items.Move`).

## Invariants

- Items change only by moves, `Consume` of one fuel as a fire is lit, and `Smelt`, which takes one
  item from the input and adds its yields to the outputs (`FurnaceTests`, beyond TypeScript's).

## Deliberately not handled

- A fuel slot takes anything, and an input anything: what does not burn or smelt only sits there.
- A furnace's fire with nothing left to smelt burns its fuel away, as in Minecraft; a generator's burns
  with nothing cabled or drawing (the crafting plan: no demand logic, the fuel taken out stops it).
- No queue, no recipes beyond `Yields`, no automation: what comes out stays until a hero takes it.
