# Plan: Liquid oxygen

## Why

A powered generator makes air from nothing, so oxygen is never something the player has to find, and the wreck's one
panel holds its room for ever. After this plan, air comes only from canisters, salvaged or frozen from ice at the
Former:
- canisters are poured into a tank as liquid oxygen;
- a liquid pipe carries it to the vaporizer (today's generator);
- the vaporizer turns it into gas on power;
- a gas pipe carries the gas to the vents.

The furnace keeps burning only in room air and breathing it (0023). The lifepod keeps refilling suits in reach and
giving the room nothing (0028).

**Builds on typed ports** ([ADR](../decisions/2026-10-29-typed-ports.md), in `main`): ports with a way and a fluid,
`pipes.mixedFluid`, and the rule that a machine is placed only where its ports fit. L-1 and L-2 are each a branch from
`main` and a PR, L-1 first. Between them `main` stays playable: L-1's wreck starts with its tank filled, and L-2 empties
it and adds the crate.

**Decided with the user** (2026-10-29):
- Two pipe fluids, not liquid and gas kept only inside machines.
- L-1 and L-2 are separate PRs.
- The wreck's tank stands in the torn room B, right of the analyzer (since then the tech station, in the same cells).
- The two starting canisters lie in a crate.
- The water plan's electrolyser gives liquid oxygen into tanks.
- The kind id stays `generator`. Only its player-facing name becomes "Oxygen vaporizer".

**Decided with the user** (2026-10-30):
- The crate stands in room A's top-left corner, on the generator.
- After L-2 comes water, in one PR, in a small `Water` module (the design: `docs/plans/water.md` on `feat/water`). Its
  electrolyser puts liquid oxygen into the tanks on its output's network through one Atmosphere operation; the tank
  gains `(3, 2) In liquidOxygen` in that PR, so that a pipe from the electrolyser's output to a tank joins an output to
  an input. L-1 keeps every store in Atmosphere.

The cistern, which the 2026-10-29 list moved into the water plan, was built after all (typed ports' second task) and is
in `main`, holding nothing. "The typed-ports ADR" below is `docs/decisions/2026-10-29-typed-ports.md`.

**Words.**
- *Liquid* is fluid `liquidOxygen`: what a tank holds and a liquid pipe carries.
- *Gas* is fluid `air`: what a vaporizer's store holds, a gas pipe carries and a vent fills a room with.
- Both are counted in the same units. A canister is `Air.Canister` (9000), and one unit of liquid becomes one unit of
  gas.
- `MachineTrait.Gas` is the existing trait of a store a canister fills: the tank. It names the trait, not the fluid; a
  vaporizer's gas store has no such trait.
- A *network* is what carries now, split by closed valves (0109). Every rule here is per network. A *line* means only a
  drawn run of `-` in a map's `Under`. A vent has one port, but a valve on it puts it on no network; "its networks" is
  the one or none `NetworksOf` gives.
- A *vaporizer feeds* a network when its output port is a node of it.

## Tasks

### L-1 — Tanks hold liquid, the generator vaporizes it

**Rules, as the player meets them:**
- **The tank.** Its port carries liquid. A pipe from a tank to a vent is refused `pipes.mixedFluid`. Canisters are
  poured in as today (`LoadCanister`, same refusals).
- **The vaporizer has two ports:**
  - a liquid input, on the left of its bottom row: `(0, 2)`, the generator's port today;
  - a gas output, on the right of its top row: `(3, 0)`.
- **A vaporizer at full power makes 1 unit of gas a tick** (`Air.GeneratorRate` per `Air.LeakEvery` ticks, 12000 per
  12000). At its own turn, after lower ids have drawn, it may make a unit only if:
  - its store has room (`Air.GeneratorStore`, 160);
  - a tank on its input's network holds liquid.

  If it may, `Making` grows by the rate × its power satisfaction (thousandths). A whole unit is `Air.LeakEvery` × 1000 of
  `Making`, the measure the generator's progress uses today. For each whole unit finished it takes 1
  liquid from those tanks, in tank id order, and adds 1 gas to its store. Units made are `min(finished, store room,
  liquid left)`, and what is not made stays in `Making`. If it may not, `Making` does not grow: nothing is made from
  nothing, and nothing is lost.
- **Draws** — breaths, leaks, fills, a venting room's drain — are paid only from the stores of the vaporizers feeding
  the vent's networks, in vaporizer id order. Tanks are never drawn from directly.
- **A vent is fed** (`Fed`, which orders the room's air, and the vent's blow and hum) when a vaporizer feeds one of its
  networks. A vaporizer's input on the vent's network does not count.
- **A vent counts as supplied** when one of its networks holds gas in a vaporizer's store, or a vaporizer feeding it is
  powered with liquid behind it. The second case keeps a room from flickering to vacuum while the store refills. Both
  are read after `Produce` on the tick they are asked, never kept in the survey, so a tank `Produce` just emptied stops
  counting on that tick on every peer. The vent's blow and hum (`AirView`, today `Oxygen > 0 || Made > 0`) follow this
  rule too.
- **A room in vacuum shows why**, from its own vents, in this order:
  - `noVent`;
  - `notPiped` (no vaporizer feeds any of its vents);
  - `dry` (no liquid behind any of those vaporizers, powered or not);
  - `unpowered`.
- **The air overlay's `RoomAir`:**
  - `Oxygen` is the liquid in the tanks on the input networks of the vaporizers feeding the room, each tank counted
    once. The gas stores are not shown.
  - `Made` is what those vaporizers make a period at their power, as today.
  - `Lasts`: liquid leaves at the smaller of the room's draw and what its vaporizers make, so `Lasts` is `Oxygen` over
    that smaller rate a tick, rounded down, computed in 64 bits and clamped to `int.MaxValue`. The room's draw is its
    leak and breathing, or, while it vents, `Air.VentDrain` × its vents, as today. Today's rule that a generator makes
    `Lasts` null goes, so the player sees the tank running dry.
  - With no vaporizer feeding the room, `Oxygen` and `Lasts` are null.
- **Throughput.** One vaporizer makes at most 1 unit a tick, and filling takes up to `Air.CellsPerFill` (4). So a big
  room fills at a quarter of today's speed from one vaporizer: the store starts empty and every unit goes out as it is
  made, so room A's 128 cells take about 128 ticks. A breach drains the store at `Air.VentDrain` (8) a tick, but liquid
  leaves only at the vaporizer's 1 a tick, so a canister outlasts a breach for about 9000 ticks (7½ minutes) instead of
  about 1125 (1 minute) today; `Lasts` shows that. More vaporizers make it faster.

**What does not change:**
- How air fills, leaks, vents and is breathed.
- The furnace and the burner generator.
- `LoadCanister`'s refusals.
- The vent.
- Power numbers: the generator still draws 4.

- **Content:**
  - `Fluids.LiquidOxygen = "liquidOxygen"`, in `All`.
  - The tank: `(0, 2) Out liquidOxygen`. It loses `AirSource` and keeps `Gas`.
  - The generator: `[(0, 2) In liquidOxygen, (3, 0) Out air]`.
  - In `Air.cs`, the summary and the `GeneratorRate`, `GeneratorStore` and `VentDrain` comments say liquid in, gas
    out, and the new breach time.
  - A content test: `GeneratorRate ≤ LeakEvery`, so a vaporizer never finishes two units in a tick at full power.
  - `TestbedTests` exempts the drawn tank–generator span (`("tank", "generator")`). Drop that exemption, and redraw the
    testbed's tank, generator and vent lines so no line mixes fluids.
- **Wreck (interim, so `main` keeps air until L-2):**
  - The tank `TT` at drawing columns 43–44, rows 21–22, in room B, right of the tech station (`AA`, columns 41–42).
  - One `o` under its top row (column 43, row 21): one canister's worth (`Air.Canister`, 9000), as `StartingTanks` reads
    it today: one canister per tank any `o` touches, however many.
  - `Under`: the liquid line at row 22, columns 43–46; the gas line at (47, 21), (48, 21), (48, 22). Doubled, they reach
    the ports and touch in no cell:
    - tank (86, 44);
    - vaporizer input (92, 44) and output (95, 42);
    - vent (96, 44).

    The gas line's cells inside the vaporizer, (94, 42), (94, 43) and (95, 43), are a stub, harmless: a drawn pipe
    meets only even cells, and the output port is on an odd one.
  - The tank comes right after the tech station in reading order, so the generator, vent and anything after move up one. Grep `WreckTests`,
    `RoomsTests`, the net tests and the `Atmosphere/Fuzz.cs` comment ("ids 0–5, the wreck's generator") for
    hard-coded ids.
- **Atmosphere:**
  - Air sources are found with `MachinesWith(AirSource)`, which is now exactly the vaporizers, and `MakesAir` goes.
    Tanks are found with `MachinesWith(Gas)`. `IsTank` stays.
  - The survey (`GatherAll`) keeps, by network, the vaporizers feeding it in id order (what vents draw from). It keeps,
    by vaporizer, the tanks on its input port's networks in id order. A vaporizer's input and output are told apart by
    the order of its spec's `Ports` (`PortsOf` returns cells in that order) and their `Way`.
  - `Fed(vent)` follows the rule above.
  - The air tick runs `Gather`, then `Produce` (the vaporizers, in id order, as above), then sets `_readFor = null` so
    `Held` is read again, then the draws. Today `Produce` runs before `Gather`.
  - `RoomsAir` follows the reasons and the overlay fields above.
  - `State.Tanks` still holds both the tanks' liquid and the stores' gas, told apart by `Gas`.
- **Save:** `World.SaveFormat` 38 → 39 (or the next free number when this lands), with an empty `Migrations` step,
  because the tank's oxygen changes meaning while the bytes stay.
  - An old tank's oxygen loads as liquid, and an old generator's store and `Making` as gas and its progress.
  - Old ships lose their supply:
    - a tank piped straight to a vent stops airing it (`notPiped`);
    - a generator's old span now joins its liquid input to the vent and airs nothing.

    The rooms bleed what they hold, the lifepod keeps heroes alive, and the player re-pipes.
  - An old wreck has no tank at all: its room loses its only supply until a tank is built and a canister found.
    Accepted: no build has been released (`Migrations.cs`), so only developers' saves meet it.
  - `MigrationsTests` gets `Fixtures/format-39.json`, written with `STILLWAKE_WRITE_FIXTURE=1`.
- **Client:**
  - `PipesOverlay.Feeds` lights and moves a network when:
    - a tank on it holds liquid (a liquid network);
    - a vaporizer feeding it is powered or holds gas (a gas network). A vaporizer's flow starts at its output port
      only, not at every cell `PortsOf` gives.

    Otherwise the network is faded dry. Liquid sockets and lines are pale blue (`#a8d8ff`), in `Sockets.ColourOf`
    beside air's and water's.
  - `Streams.pouring` and `Explain` (`Generated`, `Of`) read the new `RoomAir`. While a vaporizer feeds a room, they say
    whether it keeps up, and for how long the liquid lasts (`Oxygen` over leak and breathing).
  - The generator's name and handbook line in the building feature's `En` become "Oxygen vaporizer" and say it turns a
    tank's liquid into air.
  - The `atmosphere.dry` text says the tank behind the vaporizer is empty.
  - Art: the generator's own, with its sockets as the piping draws them (typed-ports ADR).
- **Docs:**
  - An ADR, `2026-10-29-liquid-oxygen`, with the rules above and why: the user asked for two fluids so the chain reads
    in the pipes, and the id is kept to spare a migration of every slice naming it.
  - The Atmosphere README: Rules (generators, draws, fed, supplied, reasons, throughput), API (`Fed`, `RoomAir`),
    Invariants.
  - Glossary lines: испаритель / vaporizer → `generator`; жидкий кислород / liquid oxygen → fluid `liquidOxygen`.
  - The map line for Air.
  - `concept.md`: the wreck's "weak oxygen generator" (line 30), the oxygen paragraph ("Oxygen works like a power grid")
    and the generator as the first thing that needs power.
  - The typed-ports ADR: notes that the liquid-oxygen ADR supersedes its "the tank and the generator give air", "a tank
    and generators on one manifold still work" and "a drawn tank–generator span included".
- **Done when** scenario tests cover:
  - An empty tank with a powered vaporizer: vacuum, `dry`, nothing changes over 12000 ticks.
  - After one canister: one unit a tick at full power, and the store gains exactly what the tank loses.
  - Half power, half the rate. Unpowered with liquid: `unpowered`.
  - A full store takes no liquid, and `Making` stands.
  - Tank → vent is refused `pipes.mixedFluid`.
  - A drawn tank–vent span shows `notPiped`, and its vent is not `Fed`.
  - A closed valve on the liquid network gives `dry`.
  - Two tanks and two vaporizers draw in id order. A tank behind two vaporizers counts once in `Oxygen`.
  - A tank taken apart mid-work: its liquid is lost, and the vaporizer stalls with `Making` kept.
  - A room keeps its air across a store refill (supplied, no flicker).
  - A saved world of format 38 upgraded through the new step: `format-38.json` (the old wreck) loads with the
    generator's room `notPiped` and its store and `Making` kept; a format-38 save with a tank piped to a vent, written in
    the test before the bump, loads with that room `notPiped`.

  An FsCheck invariant: liquid + gas + aired cells never grow, except by `LoadCanister`, with tanks built and taken
  apart among the drawn steps. It is shown to bite once, by a test-only break that makes a unit of gas without taking
  liquid.

  Fixed tests, each named in the PR. The rule: grep every place that supplies a vent from a tank or a generator, or
  treats the tank as an air source, and turn it into the chain; one shared test helper builds a tank → vaporizer →
  vent chain with its power. Known today:
  - `AirTests`, `AtmosphereTests`, `PowerTests` that pipe a tank or a generator straight to a vent, and those that
    assert `Fed` (`AirTests` around line 438, `MachineTraitsTests` around line 99).
  - `MachineTraitsTests` (the tank loses `AirSource`: lines 17, 42–49, 66, 71).
  - `TypedPortsTests`: its ships put a tank and a generator, two air outputs, on one line, now `pipes.mixedFluid`.
  - `FurnaceTests`, `BurnerGeneratorTests`, `CrewTests`, `HazardsTests`, `CisternTests` (its air room).
  - `net/Tests/DesyncTests` (around 300–360: a tank piped to a vent, the tank asserted drawn).
  - `BudgetTests`: `Reference`'s tile (`#TT.V.-#`) and the thousand-furnaces tile (around line 331, `#.TT.V.#`).
    `Reference`'s six inner columns are redrawn to hold the tank `TT`, the vaporizer `GG` and the vent `V`, keeping the
    `o` under the tank, with a power source the tile allows, so the generic `ModulesTests` and the 2 ms budget run the
    whole chain; the new baseline is measured and named in the PR.
  - Dev worlds: `game/dev/DevWorlds.cs`' perf tile, the `Autopilot` map, `HillsDemo` (writes tank oxygen into a save).
  - `WreckTests`: the machine list and ids, and room A aired within 300 ticks from the start, as today.

  Frames: piping mode with a blue liquid network from a tank to a vaporizer and a gas network on to a vent; the
  overlay's `dry` on an empty tank; the wreck with its two lines.

### L-2 — The wreck starts with an empty tank and a crate of two canisters

**Rules, as the player meets them:**
- The tank in room B starts empty.
- A crate stands in room A's top-left corner, on the generator: drawing columns 46–47, row 20.
- The crate holds 2 canisters, once per world. No hero, joiner or respawn gets more.
- So the first act is: take the canisters from the crate, open the door to B and pour them into the tank. Room A has
  no air yet, so the open door costs nothing.
- Later pours vent room A while the door stands open. That costs 128 units (one per cell of room A) to fill it again,
  against 9000 a canister.

**What does not change:**
- Where the hero wakes.
- Room B's tech station, which takes no power.
- The airlock.
- The panel: it makes 5, the vaporizer draws 4 and the lamp 1.

- **Content** (`Wreck.cs`):
  - Rows: `KK` at columns 46–47, row 20, on the generator's top. It comes before the lifepod in reading order, so the
    lifepod and every machine after it move up one; grep the same places as L-1 for hard-coded ids. Room A's id (its
    smallest cell, (46, 19)) does not move.
  - `WreckTests` builds a workbench on those cells (around line 194, `At(46, 20)`): it moves to free floor.
  - Under: the `o` goes.
  - The summary line names the tank in B and the crate.
  - `Wreck.Stacks`: `[("canister", 2)]`, the crate's starting stacks.
- **Core / Registry:** `Wreck.Stacks` feeds `Layout.Stocked`, an init property like `Grown`, set on `WreckMap` beside
  `DebrisAtStart`. `Doubled` and `InWorld` carry it with `this with`. No other map sets it, so scenario worlds start
  with empty crates.
- **Items:** `Init(layout)` writes `Stocked` into the layout's first crate in reading order (the first `crate` in
  `layout.Machines()`), as `StartingTanks` finds tanks. L-2 changes no saved shape: no format bump.
  - The crate gets as many slots as its spec, `Stocked` from slot 0 on and the rest empty.
  - More stacks than slots, or `Stocked` with no crate on the map, throws `FormatException`, as `StartingTanks` does.
  - It runs once per world, never on load.
- **Docs:**
  - `concept.md`'s Start paragraph (lines 29–31): the tank in the torn room, the vaporizer, and the crate of two
    canisters.
  - Its own ADR, `2026-…-wreck-crate`: the wreck starts with a crate, superseding that part of 0023. The ADR index is
    regenerated.
  - Glossary: the wreck's crate.
- **Done when** `WreckTests` cover:
  - the machine list (solarPanel, lamp, crate, lifepod, techStation, tank, generator, vent);
  - the tank empty, the crate's 2 canisters, power 5 of 5;
  - room A in vacuum and `dry` at the start;
  - after two `LoadCanister`s, room A aired within 300 ticks;
  - a saved and loaded wreck with its crate emptied stays empty;
  - the two-heroes and four-heroes air tests, after the canisters are poured.

  Desync coverage: with the wreck's tank empty and its canisters in the crate, the generic `ModulesTests` and the net
  `DesyncTests` (its `joinAt: 300` joiner too) on `Registry.Wreck` no longer run a vaporizer. Each gains a run on a
  layout whose tank starts filled (`BudgetTests.Reference`, its `o` kept), so liquid draws, partial `Making` and store
  draws stay under the two-worlds, save-load and joiner checks.

  `RoomsTests`' wreck expectations follow. Frames: the wreck at the start (faded liquid network, crate on the generator),
  and after the pour (blue network, room aired), with `tools/snap.ps1`.

## Not in this plan

- Water: the bottle, melting ice, pouring into the cistern and the electrolyser, which puts liquid oxygen into the tanks
  on its output's network, and the tank's In port that pipe ends at. All of it is the water design's
  (`docs/plans/water.md`), after L-2.
- Retuning canister drops, the ice canister (3 ice at the Former), `Air.Canister` or `Air.TankCapacity`. Air now rides
  on them. Retune after a playtest.

## Deliberately not handled

- **Boil-off, temperature, pressure:** liquid keeps for ever in a tank.
- **Liquid oxygen as an item:** a canister is the only way it moves by hand.
- **Switching a vaporizer off:** it runs while powered and fed, as the generator does.
- **Old saves' tank–vent spans** are not re-piped by a migration: they show `notPiped` and the player re-pipes.
- **A vaporizer's partial unit** resumes where it stood after a block, rather than a whole one waiting ready.
- **One vaporizer fills a big room at a quarter of today's speed:** build more.
- **`Lasts` ignores breathing and other rooms on the networks,** as today.
- **No warning before a tank runs dry on a slow leak:** `Lasts` and `dry` show in the air overlay; the `losing` pulse
  already marks breaches. A warning waits for a playtest that asks for it.
- **Pouring into the wreck's tank later vents room A** while the door stands open. The air lost is small next to a
  canister, and the player can come round through space instead.
