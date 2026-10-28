# Crafting

## Files

| File | Purpose |
| --- | --- |
| `Crafting.cs` | The module, its commands, system and interfaces. |
| `Fuzz.cs` | The commands the desync suite draws. |
| `State.cs` | The saved slice: `Entry`, `Smelt`, `CraftingState`. |

Stations ([0057](../../docs/decisions/0057-stations-one-model-for-every-crafting-machine.md)): every crafting machine
on one model. A **queued** station (pod, Former, circuit assembler, arc bench) makes crafts from a queue shared by the
crew (0028); a **fed** one (furnace, smelter, grow capsule) works its input into yields by its process, "smelt" or
"grow" (0060). Not part of `Items`: it asks `Research`, which depends on items.

- **Depends on:** `Grid` (`Machines`, `MachineById`); `Items` (queries `Crate`, `CanSmelt`, `ShortOf`, `FullFor`,
  `OutputFull`, `InputsLack`, `Boost`; ops `Take`, `Give`, `Output`, `Smelt`, `TakeInputs`); `Research` (`Knows`);
  `Power` (`Satisfaction`; `Rest`); the world's `Rng`, for a crop's seeds. Runs after `Research`, before `Building`.
- **Depended on by:** `Building` (`Queue`: a station with crafts queued is not taken apart); `Furnace` (`CanSmelt`,
  `TooCold`, `WorkOf`, `Progress`; op `Heat`); `Logistics`' drones read `Queue` and `Progress` (0062).
- **State** (`CraftingState`, saved): `Queues`, each queued station ever crafted at, by machine id: `Machine`, `Next`
  (next entry id), `Progress` (the head's unit, thousandths of a tick) and `Entries`, `Entry(Id, Recipe, Count, By,
  FromInputs, Endless)` in queue order, `Count` the units left with the one at work; an emptied queue is kept so ids
  keep counting. `Smelts`: `Smelt(Machine, Item, Progress)` by machine id. Derived: `Heated`, the fed stations whose fire
  burns this tick, set by `Furnace` in construction.

## Rules

- **A station** is a machine in `Content.Stations`: `Speed` (thousandths), `QueueMost`, `Rows` (what a queued one
  makes), `Heat` (above 0 makes it fed), `Process`. `StationOf` alone reads these, so upgrades fold in there: fitted
  modules (`Boost`, [0086](../../docs/decisions/0086-modules-one-socket-for-machines-and-tools.md)) make speed
  `Speed × (1000 + their Speed) / 1000`, rounded down, and add their heat to a fed one's; a queued one's heat stays 0.
  Slots are the machine's (`MachineSpec.Slots`, `Inputs`), read by `Items`.
- **Energy** (0057): one that `Draws` works at its network's satisfaction; one with a `FuelSlot` at full while its fire
  burns, not at all while out; one with neither (the pod) at full. Work a tick is `Speed × share / 1000`, rounded down.
  Stations work in the simulation phase, after power's derivation, by machine id.
- **Idle** (nothing it can work, or no room) a station that draws is told to `Rest` from the next tick, and works at
  once when told to work (0057).

### Queued stations

- A craft is named by the item it makes; cost and `Ticks` per unit come from `Content.Crafts`; a unit takes
  `Ticks × 1000` of work; crafts layer parts, components, products (0046). Rosters and numbers are content's; the
  pod's list stops growing (0057); electronics are the assembler's, sky-shard crafts the arc bench's (0099).
- **Inputs** (0057): as many slots as the most distinct items any of its crafts needs (`ContentTests`). The pod has
  none and the arc bench's one is a vise (0099): a craft from inputs there is refused `crafting.noInputs`.
- **`Craft(Machine, Recipe, Count, FromInputs)`**: paid (the default) takes all `Count` units' cost from the sender at
  once (nothing in creative mode); fed from the inputs takes nothing. Either appends an entry by the sender with the
  queue's next id. Refused whole: `crafting.noStation`, `crafting.unknown` (no craft or not known), `crafting.notHere`
  (not in this station's rows, or a fed station), `crafting.full` (`QueueMost` entries, whoever queued them),
  `items.notEnough` (paid), `crafting.noInputs`.
- **`Cancel(Machine, Entry)`** by anyone gives the sender what was paid and not made, priced by content now: a paid
  entry's units left, the one at work included; a fed one's unit begun (progress above 0), else nothing; what the
  inputs hold stays. Refused `crafting.noStation`, `crafting.notHere`, `crafting.noEntry`, `items.full`. Cancelling
  the head, the next starts from nothing.
- **`SetEndless(Machine, Entry, On)`** sets (not toggles) an entry fed from the inputs to repeat: a unit made leaves its
  count. Refused `crafting.noStation`, `crafting.notHere`, `crafting.noEntry`, `crafting.paid`.
- Each tick the head adds its work, at most its craft's; at its work one unit goes to the outputs
  (`IItemsOps.Output`) and the next starts. No room: the unit waits at full progress. The queue runs whoever is near.
- **A unit fed from the inputs** takes its cost (`TakeInputs`, first slot first) on its first tick with work above
  0, so an unpowered station takes nothing; creative mode does not make it free. While inputs lack it the entry waits
  at 0 and the whole queue behind it, first in, first out (0057). `Lacks(machine)` says what it waits for.

### Fed stations

- The input slot is the queue. Each tick, work on another item than the input holds is dropped; then an item with
  `Yields` of the station's `Process`, `Heat` at most the station's, and yields that fit (`CanSmelt`) gains work; at
  its `SmeltTicks × 1000` one becomes its yields (`IItemsOps.Smelt`). Work stopped (fire out, outputs full, power cut)
  is kept while the item stays (0057).
- **Too cold / wrong process** (0057): such an item is taken in and sits unworked, the station resting, a furnace
  never lit for it; `TooCold` tells the too-hot case.
- **A crop** (0060) uses its seed or spore and gives its yields and none, one or two of itself by its `Returns` weights,
  drawn from the world `Rng` as it completes, stations in machine id order; an item without `Returns` draws nothing.
  It grows only with room for the yields and the most seeds it may give, so no draw overflows.
- **A furnace smelts while its fire burns at the tick's start** (0057): `Furnace` calls `Heat` in construction from
  the fire as the last tick left it, so it starts the tick after lighting and smelts on the tick it goes out.

## API

- **Queries** (`ICraftingQueries`): `StationOf`, `Queue` (live, read-only), `Progress`, `CanSmelt`, `TooCold`,
  `WorkOf` (null for nothing of its process). Module only: `Works` (a furnace reads idle between ticks), `Lacks`.
  Static: `Processes(station, item)`, of its process and no hotter, room aside.
- **Operations** (`ICraftingOps`): `Heat(machine)`.
- **Fuzz:** every command and refusal, at stations and non-stations, live and never-given ids.

## Invariants

- A queue holds at most `QueueMost` entries; the head's progress stays below its work but while its unit waits for
  room; ids within a queue only grow.
- Items are conserved: what a craft took comes back as units or a refund; a smelt turns one item into its yields, a
  crop a seed into its yields and 0–2 seeds (`CraftingTests`, `StationsTests`).
- Every item is reachable from the wreck (`ContentTests`).

## Deliberately not handled

- A count below one: the client sends one or five.
- A finished unit waits in the station, never dropped, so crafting needs no `crew`.
- A cancel refunds the canceller, not the payer: no record of who paid; `By` only marks whose order it is.
- In creative mode a cancel gives back what was never taken. A recipe queued twice is two entries.
- An empty queue left by a station taken apart stays: machine ids are never reused.
- A smelt's work past the item's on its last tick is dropped.
- Nothing stops a too-hot or wrong-process item entering a station: the player moves it (0057).
- A crop is not grown without power (0060).
- No skip rule: a head waiting for inputs, or endless, holds up the queue; the player cancels it (0057).
- What a cancelled fed entry left in the inputs stays.
