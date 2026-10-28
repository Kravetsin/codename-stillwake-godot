# Furnace

## Files

| File | Purpose |
| --- | --- |
| `Furnace.cs` | Fire (ADR 0023): a furnace or a burner generator burns fuel and breathes its room's air; a furnace's fire heats it … |

Fire ([0023](../../docs/decisions/0023-a-furnace-burns-fuel.md)): a furnace or a burner generator burns
fuel and breathes its room's air. A furnace's fire heats it, so `Crafting` smelts the salvage in its input
into what it yields, the way caught debris becomes materials (a fed station,
[0057](../../docs/decisions/0057-stations-one-model-for-every-crafting-machine.md)); a burner generator
makes power while it burns ([0045](../../docs/decisions/0045-a-burner-generator-shares-the-furnaces-fire.md)).
Ported from `packages/sim/src/modules/furnace`; its rules hold here, but a fire fed in time relights at
once, and smelting has moved to `Crafting`.

- **Depends on:** `Grid` (`IGridQueries`: which machines have a fire, `MachinesWith(Fire)` (0113): with inputs it smelts, else it makes power;
  and their cells), `Items` (`IItemsQueries`: their slots; `IItemsOps`: `Consume`, a fuel lit),
  `Atmosphere` (`IAtmosphereQueries`: `AiredAt`; `IAtmosphereOps`: `Breathe`, the air their fire
  breathes), `Power` (`IPowerOps`: `Rest`, a cold generator makes nothing; `IPowerQueries`: `Needed`, whether its
  network wants a fire), `Crafting`
  (`ICraftingQueries`: `CanSmelt`, whether a furnace has what it can smelt, and `Progress` for its panel;
  `ICraftingOps`: `Heat`, a furnace's fire burns).
- **State** (`FurnaceState`, members internal): `Fires` — each furnace or generator burning,
  `Fire(Machine, Fuel)` by machine id in id order: the ticks of fire left, above 0. Saved; nothing is
  derived. Format 8 renamed a fire's `furnace` to `machine`; format 14 moved its `item` and `progress`
  to `Crafting`'s smelts and dropped the fires kept only for them.
- **Files:** `Furnace.cs` — the module, its state and records.
- **Runs** after `Debris` and before `Logistics` in `Registry.All`, last of the modules TypeScript had.

## Rules

- A machine's slots live in `Items`: the furnace's input, its fuel, then its outputs; the generator's
  one fuel slot. Which slot burns is the machine's `FuelSlot` in `Content.Machines`. Both are opened
  with E as a crate is.
- Each tick, in the simulation phase, furnaces and generators together in machine id order:
  - **lit** only when it has none, with fuel in its fuel slot (an item with `BurnTicks`) and air in a
    cell of it; a furnace also only with what it can smelt (`ICraftingQueries.CanSmelt`: salvage in its
    input, hot enough, room for its yields; never for what is too hot for it, which stays in its input).
    One fuel is used up (`IItemsOps.Consume`), worth its
    `BurnTicks`;
  - **burning**, it breathes `Content.Furnace.Breath` from its first aired cell every
    `Content.Furnace.BreathEvery` ticks of fuel (`IAtmosphereOps.Breathe`, as a hero does); given
    less, its fire goes out and the fuel left is lost. Otherwise it burns a tick of fuel, smelting or
    not. Its last tick of fuel burnt, it is lit again on the same tick if it can be, so a fire fed in
    time never goes out between two fuels.
- **A furnace's fire heats it:** in the construction phase, before crafting's simulation, each furnace
  whose fire burns is told to `Crafting` (`ICraftingOps.Heat`), which smelts in it that tick at its
  station's speed. The fire is read as the last tick left it, so a furnace starts smelting the tick after
  it lights, and a fire going out for want of air still smelts on its last tick.
- The numbers are content's (`Content.Items`): a plank burns 400 ticks; a scrap chunk smelts in 60
  into 4 scrap, a hull plate in 100 into 2 iron and 1 scrap, rock in 160 into 2 silicon, so a plank smelts about 6 chunks or 4
  plates ([0046](../../docs/decisions/0046-recipes-show-what-a-thing-is-made-of.md)).
- **A generator rests while its fire is out:** in the construction phase, before power's derivation,
  each one not burning is told to rest (`IPowerOps.Rest`), one burning to work. `Power` counts no
  resting maker, so a generator makes its `Makes` from the tick after it is lit to the tick after it
  goes out, and one just built never makes a tick. **It lights its next fuel only while its network wants a fire**
  (`IPowerQueries.Needed`: its draw and what its stores could take in, more than its panels make; [0101](../../docs/decisions/0101-a-network-stores-its-spare-power.md)):
  a fuel lit burns to its end, the next waits in its slot, and one on no network never lights. Other fires are not
  weighed, so two on a network burn together.

## API

- **Queries:** `At(id)` — furnace or generator `id` as its panel shows it, a `Smelting(Burning, Item,
  Progress, Work, Problem)`: whether it burns, and why it stands still: `crafting.tooCold` (a furnace
  only, what its input holds needs more heat: `ICraftingQueries.TooCold`), `crafting.tooFew` (a furnace only,
  fewer than one smelt takes: `TooFew`), `furnace.full` (a furnace
  only, nothing it can smelt fits), `furnace.noFuel`, `furnace.noAir` or `furnace.unneeded` (a generator only, cold with
  fuel its network does not want); for a furnace with salvage in
  its input, also what it smelts, how far, out of how much work, in thousandths of a tick (crafting's
  measure). A furnace with nothing to smelt says only `Burning` (TypeScript's `{ burning }`). No query
  interface: no module reads it, only the client.
- No commands, so no fuzz generator: a hero works a fire by moving items in and out (`Items.Move`).

## Invariants

- Items change only by moves, `Consume` of one fuel as a fire is lit, and `Crafting`'s smelt, which
  takes one item from the input and adds its yields to the outputs (`FurnaceTests`, beyond
  TypeScript's).

## Deliberately not handled

- A fuel slot takes anything, and an input anything: what does not burn or smelt only sits there.
- A furnace's fire with nothing left to smelt burns its fuel away, as in Minecraft; a generator's burns
  out to its end once lit, even if its network stops wanting it: a fire has no paused state (0101).
- No queue, no recipes beyond `Yields`, no automation: what comes out stays until a hero takes it.
- The tick a fire lights it does not smelt, and the tick it goes out for air it does: the fire is read
  once, as the tick begins, rather than crafting and the fire looping through each other within a tick.
