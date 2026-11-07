# Design: Liquids — water and lava that flow in a planet's cells

Status: design accepted by the user (2026-11-07) for stage 1, water; the plan for it below. Lava and the rest follow as
their own stages.

**Decided with the user** (2026-11-07):
- Water first. Lakes lie in the middle bands, the garden and the amber deep: ice is above them, lava below. Big lakes of
  some 100×60 cells and medium ones.
- Heroes swim in water from the first PR, with the pose they float in space with (`Pose.Floating`).
- Water is heard from the first PR: a player who finds water in a cave jumps in. The sounds are picked by the user.
- Nothing takes water from a lake yet (no bottle, no pump): the link to the water chain comes later.
- Water and lava meeting (steam, lava cooling to stone) is later, but it will be done.
- The core band stays a solid wall while water is built.

## Problem
A planet's caves are dry and its lava is a wall: nothing is let loose by digging.

## Player experience
1. Deep in the caves, a hero finds a lake, still, its surface flat. It jumps in with a splash and swims: floating as in
   space, sinking slowly, Space up, out at the surface with a jump.
2. It drills into a cave wall and breaks into a lake's side: water pours out of the hole, rushing, falls down the shaft
   beside it and pools at the bottom; the lake's surface sinks.
3. A wall built across the stream dams it.
4. (Stage 2) Deep in the ember band a crust gives and glowing lava creeps out, slowly, burning the suit as the core does.

Goes wrong: a hero digs under a lake, the floor gives, water falls round it: it is swimming, and swims out.

## Out of scope (deliberate omissions)
- **The core band stays a solid wall** (0087): open cells along the bottom would join every cave and wake 4×4000 cells
  beside any deep dig.
- **Water and lava meeting** (later, promised): stage 1 has no liquid lava; stage 2 keeps them apart (a cell holds one
  kind, neither flows into the other) until the meeting is designed.
- **Taking water from a lake** (a bottle, a pump): later; the first planet allows no machines.
- **Rain, weather, evaporation:** the weather design's; it adds the op it calls (`Add(cell, kind, amount)`) and says how
  rain stays bounded, since nothing evaporates. Liquid in sky cells (surface ponds) comes with it.
- **Pressure:** water never rises in a U-tube; a body is levelled only by falling and side moves.
- **Liquid on the ship** (no source, vacuum).
- **Creatures, floor stacks, bodies in liquid:** they move as through air.
- **Drowning, any breath rule under water:** the suit is sealed.
- **Wading:** water under a hero's middle changes nothing; it swims once its middle is in water.

## Scale
- World: First is 4096×1536, 6.3M cells. A big lake is at most `Liquids.Big` (6,000) cells, a medium one `Medium`
  (1,500). How many per band: `Content`, tuned on a look.
- **Measured** (throwaway console run in Release: 4096×1536, awake 16×16 chunks, a lake drained through one dug cell into a
  600-row chasm and a 1200-wide cave below):

  | Full | 2,000-cell lake | 20,000-cell lake |
  | --- | --- | --- |
  | 8 | mean 0.06 ms, max 0.41; ≤ 96 chunks awake; settled after ~5,000 ticks | mean 0.13 ms, max 0.67; ≤ 128 awake |
  | 240 | mean 0.06 ms; never settled in 30,000 ticks | mean 0.26 ms, max 0.96; never settled |

  About 1 µs a tick per awake chunk; nothing awake, 0.01 ms; every unit conserved. Four heroes draining four big lakes
  stay far under 2 ms. 240 levels never settle (a ramp of 240 steps creeps for ever), so **`Full` is 8**.
- A settled planet has nothing awake; on the ship the layer is never allocated.
- Frequency: grid changes at digging pace (a drill hits 10 a second).

## Approach
A new module `Liquids` (nothing owns liquid in cells: Water owns cisterns, Atmosphere air). Depends on Grid's queries;
registered after Grid's dependants that build (Building), before Crew.

- **State:** per cell a kind (none, water; lava in stage 2) and an amount 0..`Full` (8: a pixel a unit in an 8 px cell).
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
- **Queries:** `AmountAt(cell)`, `KindAt(cell)`; `ChangedSince(version)` for the client to redraw only what moved; the
  layer read-only.
- **Grown lakes** (Terrain, last, so basins are final; carried in `Layout.Liquids`): pockets drawn on a lattice every
  `LakeWave` cells in the garden and amber bands, each by a chance big or medium; from a pocket's floor the level rises row
  by row while the open cells connected below it stay under its cap, inside the zone, clear of ruins and other lakes.
  Every cell full: settled by construction.
- **Client** (`game/features/liquids`): a shader quad over a texture of the cells in view, updated for changed cells; a
  run of top cells drawn at its mean level, so a settled lake reads flat (8 levels may leave a pixel between neighbours).
  The hero controller swims where its middle cell holds water. Sounds: a splash in and out, strokes, a stream pouring.
  **The look and the sounds are picked by the user** on Artifact pages before they are built.

## Determinism and co-op
- Inputs: the seed and grid writes (from commands). Integer units; a side move rounds down, the giver keeps the odd unit.
- Order fixed as above; the awake set is an optimisation over a whole sweep, so a loaded world and one kept running agree.
- Joining: the edits; the seed grows the rest again; `Rebuild()` wakes what can move.
- Client-authoritative: swimming is hero movement, read from each peer's own copy of the liquid.

## Trade-offs accepted
- A wall or a closed door built into a liquid cell destroys up to `Full` there: damming will be common, the loss a cell.
- A settled surface may keep 1 unit (a pixel) between neighbours; drawn flat by the client.
- A thin film of 1 unit at a pool's edge stays for ever (its neighbours differ by 1), saved as edits.
- Liquid passes machines (the drop pod) and is drawn through them: planets have almost no machines.
- A big body drained through a one-cell hole takes minutes (8 units a tick through it); a wider hole drains faster.

## Stages
1. **Water:** the module, grown lakes, the look, swimming, the sounds. One PR.
2. **Lava:** grown lava pockets in the ember band, lava moving every `Every` ticks, `Crew.Burns` reading liquid lava, the
   glow through `LightView`. One PR.
3. **Water meets lava;** taking water from lakes; rain with the weather. Each designed when it comes.

## Plan: stage 1, water

Branch `claude/liquids-c190ea`, one PR. Tasks in order; the look and sounds are picked while L-1 to L-3 are built.

### L-1 — The `Liquids` module
`sim/Liquids` (skill `new-module`): `LiquidsModule : Module<LiquidsState>`, `ILiquidsQueries`, Registry after Building and
before Crew, a save format bump with its empty step and fixture. `Content/Liquids.cs`: `Full` 8, `Fall` 4, `Chunk` 16.
State: `Kind` and `Amount` byte layers (null until liquid), saved as `Edits` against the grown layer (a drawn map saves
every wet cell). Derived: awake chunk flags, the grid version read, a change log for the client (as Grid's `ChangeLog`).
The flow system as Approach says; the grid-change step first in the same system. Legend: `w` a cell of water at `Full`
(`Layout.Liquids`). README with Rules, API, Invariants, Deliberately not handled; `docs/map.md` line; glossary "вода,
озеро".
Tests (`LiquidsTests`): every scenario below the plan; FsCheck: conservation on closed maps, settling within a bound and
staying still, no liquid in a blocking cell, amounts in 0..Full, every chunk with a movable cell awake, saved and loaded
mid-flow equals kept running. The generic suites cover it once registered; its `Fuzz` has no commands.

### L-2 — Grown lakes
`Terrain` fills `Layout.Liquids` last: lattice points every `LakeWave` in the garden and amber bands, each a lake by
`LakeChance`‰, big (`Big` 6000 cells) by `BigChance`‰ else medium (`Medium` 1500); the fill as Approach says, within
`PlanetType` numbers (none in `Ground`'s generic part unless First and Small differ). `Small` gets a lake or two so tests
and the fuzz meet water. Tests: grown lakes full, settled (a tick changes nothing), within caps and the zone, in the bands,
none on a ruin; growing is pure (same seed, same lakes); `Terrain.Generate` time on First before and after, stated in the
PR.

### L-3 — Water drawn
`game/features/liquids/LiquidsView.cs` in `SurfaceViews.cs` (after the ground, before heroes? by the picked look), a
shader over an R8 texture of the cells in view, redrawn for cells the module names changed; the surface run drawn at its
mean; the picked look exported through `tools/anime-art` if it is a picture. A dev room `--lake` (skill `new-demo`): a
grown planet's lake by the spawn, or a drawn map with a lake, a wall to dig and a chasm.

### L-4 — Swimming
`game/features/crew/logic/Hero.cs`: the controller's `Ship` gains the liquid lookup; a hero whose middle cell holds water
swims: keys at `Hero.WaterSwim`, sinking at `Hero.WaterSink` with none, Space up; at the surface (its middle cell dry
above the water) a jump leaves the water. `Pose`: floating (`Floating`) while in water. Unit tests in `game.tests`.
Numbers in `sim/Content/Hero.cs`.

### L-5 — Sounds
The picked sounds cut, levelled and credited (`game/audio/CREDITS.md`), cues in `gc/Sound.cs`: a splash when a hero's
middle enters water and when it leaves, strokes while swimming, a loop of pouring heard near awake falling water (the
module's awake chunks with a fall in them, nearest first, one loop).

### L-6 — Review and PR
`/review-implementation`; the user plays `--lake`; ADR `2026-11-07-liquids.md` (what stays true), this plan's stage 1
removed once merged.

### Scenarios (L-1)
A pool's wall dug: the water falls and pools, the level drops; water passes ladders, floors and open doors and stops at a
closed one; a wall built into water removes that cell's only; two basins joined end within 1 unit; a U-tube does not
rise; out of the zone gone; a fresh Small planet unchanged by a tick, nothing awake; the ship never allocates the layer;
the grid log overrun (null) wakes every wet chunk.
