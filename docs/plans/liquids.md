# Design: Liquids — water and lava that flow in a planet's cells

Status: design (design-author, cut by design-reviewer, cost measured), waiting for the user's go. Then a plan per stage.

## Problem
A planet's caves are dry and its lava is a wall: nothing is let loose by digging, and the water chain has only melted ice.

## Player experience
1. A hero drills into a cave wall and breaks into a lake's side: water pours out of the hole, falls down the shaft beside
   it and pools at the bottom; the lake's surface sinks.
2. A wall built across the stream dams it.
3. The hero points at the lake with the use key and fills a `water` bottle, carries it aboard and pours it into a cistern
   as today.
4. (Stage 2) Wading in, the hero walks slower and swims up with Space.
5. (Stage 3) Deep in the ember band a crust gives and glowing lava creeps out, slowly, burning the suit as the core does.

Goes wrong: a hero digs under a lake, the floor gives, water falls round it. Stage 1: it walks on through the water as
through air (nothing in the sim cares). Stage 3: digging up into a lava pocket, lava drops on the hero; the suit gauge falls
in some three seconds, it passes out and wakes at the pod, its body left by the lava (0061).

## Out of scope (deliberate omissions)
- **The core band stays a solid wall** (0087): open cells along the bottom would join every cave and wake 4×4000 cells
  beside any deep dig.
- **Water and lava never react:** a cell holds one kind; neither flows into a cell of the other; water rests on the core
  wall. No steam, no lava cooling to stone: nobody asked, and it needs a grid write from Liquids.
- **Rain, weather, evaporation:** the weather design's; it adds the op it calls (`Add(cell, kind, amount)`) and says how
  rain stays bounded, since nothing evaporates. Liquid in sky cells (surface ponds) comes with it.
- **Pumps and pipes on planets:** the first planet allows no machines (`PlanetTypes.First.Builds`).
- **Pressure:** water never rises in a U-tube; a body is levelled only by falling and side moves.
- **Liquid on the ship** (no source, vacuum); pouring a bottle back out into the world.
- **Creatures, floor stacks, bodies in liquid:** they move as through air.
- **A breath rule under water:** the suit is sealed; drowning is not asked for.
- **Sound** of flowing water and lava: a later pick.

## Scale
- World: First is 4096×1536, 6.3M cells. A grown lake is at most `Liquids.LakeMost` (2048) cells, a lava pocket
  `PocketMost` (512). Lakes per planet: the user's call (open question).
- **Measured** (throwaway console run in Release: 4096×1536, awake 16×16 chunks, a lake drained through one dug cell into a
  600-row chasm and a 1200-wide cave below):

  | Full | 2,000-cell lake | 20,000-cell lake |
  | --- | --- | --- |
  | 8 | mean 0.06 ms, max 0.41; ≤ 96 chunks awake; settled after ~5,000 ticks | mean 0.13 ms, max 0.67; ≤ 128 awake |
  | 240 | mean 0.06 ms; never settled in 30,000 ticks | mean 0.26 ms, max 0.96; never settled |

  About 1 µs a tick per awake chunk; nothing awake, 0.01 ms; every unit conserved. Four heroes draining four lakes stay
  far under 2 ms. 240 levels never settle (a ramp of 240 steps creeps for ever), so **`Full` is 8**.
- A settled planet has nothing awake; on the ship the layer is never allocated.
- Frequency: grid changes at digging pace (a drill hits 10 a second); a bottle a key press.

## Approach
A new module `Liquids` (nothing owns liquid in cells: Water owns cisterns, Atmosphere air). Depends on Grid's queries and
Items' ops; registered after Items, before Crew.

- **State:** per cell a kind (none, water; lava in stage 3) and an amount 0..`Full` (8: a pixel a unit in an 8 px cell).
  Byte layers, allocated when the first liquid appears. **Saved:** the cells that differ from the grown liquid, ascending
  (Grid's edits pattern, 0077). Everything else derived.
- **Flow** (simulation phase): rows bottom-up; in a row, cells left to right or right to left by the tick's parity. A cell
  falls into the open cell under it, through up to `Fall` empty ones; what cannot fall moves ⌊(a−b)/2⌋ to a side neighbour
  at least 2 lower (so it settles, never jitters: every fall lowers liquid, every side move lowers the row's Σa²); a cell
  given liquid sideways this tick passes none sideways. Liquid lies only where the grid does not `Block`, and only inside
  the zone: what leaves it is gone (the fog drains).
- **Awake chunks are derived, not saved.** The rule is the sweep of every cell; chunks of 16×16 only skip what cannot move.
  A move wakes the chunks round both cells, including chunks ahead in the same sweep, so skipping never changes the
  result. `Rebuild()` scans the layer once and wakes every chunk with a cell that can move (some 25 ms on a planet, at load).
- **Grid changes** (`ChangedSince`): a cell now blocking loses its liquid; each change wakes the chunks round it. When the
  log no longer reaches back (null), every chunk is scanned as in `Rebuild()`.
- **The surface drawn flat:** with 8 levels a settled pool may keep 1 unit between neighbours; the client draws a run of
  top cells at its mean level, so a lake reads flat.
- **Command** `Bottle(Cell)`: takes `Full` water from that cell, gives `Water.Item` (`GiveOrDrop`). Refused
  `liquids.noWater`, `liquids.tooShallow` (under `Full`). A lake is a source of bottles, as ice veins are.
- **Queries:** `KindAt`, `AmountAt`; the layers read-only for the client.
- **Grown lakes** (Terrain, after the ruins, so basins are final; carried in `Layout.Liquids`): pockets drawn on a lattice
  every `LakeWave` cells by a chance per band; from a pocket's floor the level rises row by row while the open cells
  connected below it stay under the cap, inside the zone, clear of ruins and other lakes. Every cell full: settled by
  construction.
- **Client** (`game/features/liquids`): a shader quad over a texture of the cells in view, updated for changed cells; the
  look is a new one, **picked by the user on an Artifact page** before it is built.

## Determinism and co-op
- Inputs: the seed, `Bottle`, grid writes (from commands). Integer units; a side move rounds down, the giver keeps the odd
  unit.
- Order fixed as above; the awake set is an optimisation over a whole sweep, so a loaded world and one kept running agree.
- Joining: the edits; the seed grows the rest again; `Rebuild()` wakes what can move.
- Client-authoritative: swimming (stage 2) is hero movement; a burn (stage 3) reads the reported cell, a second late as
  today.

## Trade-offs accepted
- A wall or a closed door built into a liquid cell destroys up to `Full` there: damming will be common, the loss a cell.
- A settled surface may keep 1 unit (a pixel) between neighbours; drawn flat by the client.
- A thin film of 1 unit at a pool's edge stays for ever (its neighbours differ by 1), saved as edits.
- Liquid passes machines (the drop pod) and is drawn through them: planets have almost no machines.
- A big body drained through a one-cell hole takes minutes (8 units a tick through it); a wider hole drains faster.

## Tests
- Scenarios: a pool's wall dug, the water falls and pools, the level drops; water passes ladders, floors and open doors,
  stops at a closed one; a wall built into water removes that cell's only; two basins joined end within 1 unit; a U-tube
  does not rise; out of the zone gone; water stops beside lava (stage 3); `Bottle` and its two refusals; a fresh Small
  planet unchanged by a tick; the ship never allocates the layer.
- Invariants (FsCheck): flow conserves the total (nothing leaving the zone); random closed maps settle within a bound,
  then stay still; every chunk with a cell that can move is awake; no liquid in a blocking cell, amounts within
  0..`Full`; grown lakes full, settled, within the cap and the zone; a world saved and loaded mid-flow equals one kept
  running (the generic suites too); Liquids' `Fuzz` draws `Bottle`.

## Stages
1. **Water.** The module, grown lakes, `Bottle`, the client's look (picked first) — one PR. Heroes walk through water.
2. **Swimming.** The hero controller (`game/features/crew` logic): slower in water, Space swims up. One PR.
3. **Lava.** Grown lava pockets in the ember band, lava moving every `Every` ticks, `Crew.Burns` reading liquid lava, the
   glow through `LightView`. One PR.

## Cost
- Modules: new `sim/Liquids`; changed `Terrain`/`Layout`, Registry (a format bump), then Crew (stage 3) and the hero
  controller (stage 2); new `game/features/liquids`.
- Content: `Content/Liquids.cs`: `Full`, `Fall`, `LakeMost`, `LakeWave`, the chance per band; later `Every`, `PocketMost`.
- Estimated diff, stage 1: some 600 lines of sim, 500 of tests, 300 of client.

## Open questions (for the user)
- How many lakes, and how big: `LakeMost` 2048 cells (a pocket some 60×35), in which bands?
- A bottle a full cell (a 2048-cell lake holds 2048 bottles; ice gives one each)?
- Swimming and lava as stages 2 and 3, or swimming in the first PR?
