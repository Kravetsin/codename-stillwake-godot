# Design: Liquids — water and lava that flow in a planet's cells

Status: design accepted by the user (2026-11-07) for stage 1, water; its plan below, revised after `/review-plan`. Lava
and the rest follow as their own stages.

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
- **The core band stays a solid wall** ([0087](../decisions/0087-the-planets-core-is-lava.md)): open cells along the
  bottom would join every cave and wake 4×4000 cells beside any deep dig.
- **Water and lava meeting** (later, promised): stage 1 has no liquid lava; stage 2 keeps them apart (a cell holds one
  kind, neither flows into the other) until the meeting is designed.
- **Taking water from a lake** (a bottle, a pump): later; the first planet allows no machines.
- **Rain, weather, evaporation:** a later user of this module. Rain is pulled, not pushed: a storm is a field on `Sky.Storm`,
  and this module reads `ISkyQueries.Now` (Sky has no dependencies and registers early) and pours water into open-sky
  columns itself (the day-night design). How rain stays bounded, since nothing evaporates, is decided then. Liquid in sky
  cells (surface ponds) comes with it.
- **Pressure:** water never rises in a U-tube; a body is levelled only by falling and side moves (a body levelled as a
  whole needs bodies tracked as entities, and nothing in play asks for it).
- **Liquid on the ship** (no source, vacuum).
- **Creatures, floor stacks, bodies in liquid:** they move as through air.
- **Drowning, any breath rule under water:** the suit is sealed.
- **Wading:** water under a hero's middle changes nothing; it swims once its middle is in water.

## Scale
- World: First is 4096×1536, 6.3M cells. A big lake is at most `Big` (6,000) cells, a medium one `Medium` (1,500). How
  many per band: content, tuned on a look.
- **Measured** (throwaway console run in Release: 4096×1536, awake 16×16 chunks, a lake drained through one dug cell into a
  600-row chasm and a 1200-wide cave below):

  | Full | 2,000-cell lake | 20,000-cell lake |
  | --- | --- | --- |
  | 8 | mean 0.06 ms, max 0.41; ≤ 96 chunks awake; settled after ~5,000 ticks | mean 0.13 ms, max 0.67; ≤ 128 awake |
  | 240 | mean 0.06 ms; never settled in 30,000 ticks | mean 0.26 ms, max 0.96; never settled |

  About 1 µs a tick per awake chunk; nothing awake, 0.01 ms; every unit conserved. Four heroes draining four big lakes
  stay far under 2 ms. 240 levels never settle (a ramp of 240 steps creeps for ever), so **`Full` is 8**, not 240.
- A settled planet has nothing awake; on the ship the layer is never allocated.
- Frequency: grid changes at digging pace (a drill hits 10 a second). Draining: 8 units a tick through a one-cell hole;
  at 20 ticks a second a 6,000-cell lake in some 5 minutes; a wider hole drains faster.

## Terms
- **Open:** a cell whose structure does not `Block` (Grid's trait): empty, a ladder, a floor (it only `Supports`), an open
  door or hatch. A wall and a closed door block. Machines do not block liquid.
- **The zone:** the grid's build zone (`Layout.Zone`, [0048](../decisions/0048-a-big-world-a-build-zone-inside-it.md));
  on a planet the columns between the fog bands (fog-edges).
- **First, Small:** the first planet's type (`PlanetTypes.First`, 4096×1536) and its 128×96 copy for tests (`Small`).
- **Bands:** a planet's depth bands (`PlanetType.Bands`, 0080): 0 the cold (ice veins), 1 the garden, 2 the amber deep,
  3 the ember band over the core.
- **A lake:** water grown with the planet: every cell full, connected, resting on rock, settled.

## Approach
A new module `Liquids` (nothing owns liquid in cells: Water owns cisterns, Atmosphere air). Depends on Grid's queries;
registered after Building, before Crew.

- **State** (`LiquidsState`): `Clock`, the ticks it has run (saved: the sweep's direction comes from it, for modules have
  no clock of their own); `Kind` and `Amount`, a byte per cell (none, water; lava in stage 2; 0..`Full`, amount 0 always
  kind none), `[JsonIgnore]`, null until the surface has liquid. **Saved:** `Clock` and the edits: the cells that differ
  from the liquid the layout grew or was drawn with, ascending, as `cell, kind, amount`, worked out from the cells touched
  since (Grid's `Touch`/`Edits` pattern, `sim/Grid/Grown.cs`), never by scanning every cell: a save is hashed once a second.
- **Flow,** each tick in the simulation phase, `dir` = +1 (left to right) on an even `Clock`, −1 on an odd one:

  ```
  for y from the bottom row up:                      // whole rows of the world
    for x along the row in dir, skipping cells of sleeping chunks:
      a = Amount[c]; if a == 0: continue
      if open(below) and Amount[below] < Full:       // fall, into a part-filled cell too
        t = below
        repeat up to Fall-1 times: if Amount[t] == 0 and open(under t) and Amount[under t] < Full: t = under t
        f = min(a, Full - Amount[t]); move f from c to t; a -= f
      if a >= 2 and not given[c]:                    // what did not fall spreads; given: got liquid sideways this tick
        for n in (c + dir, c - dir):                 // the sweep's way first
          if open(n) and Amount[n] <= a - 2:
            f = (a - Amount[n]) / 2                  // rounded down: the giver keeps the odd unit
            move f from c to n; a -= f; given[n] = true
  ```
  A move into a cell outside the zone destroys what moved (the fog drains). `given` is cleared before every sweep. It
  settles: a fall lowers liquid; a side move from a to b ≤ a−2 lowers a²+b², so the row's Σa² falls; both are bounded.
- **Awake chunks** (16×16, `[JsonIgnore]`): the result is that of sweeping every cell; a chunk is skipped only when no
  cell in it can move. *Can move:* a > 0 and (an open cell below holding under `Full`, or a ≥ 2 and an open side
  neighbour holding ≤ a−2). Any change to a cell's amount or blocking wakes the chunks of the cell and of its four
  neighbours at once, so a chunk ahead in the sweep (later in the row, or a row above) is swept this tick. After the
  sweep, a chunk in which nothing moved and nothing woke it this tick sleeps if no cell in it can move. `Rebuild()` wakes
  every chunk with a cell that can move (a scan of the layer, some 25 ms on a planet, at load only).
- **Grid changes,** first in the same system: `ChangedSince` the version last read (derived: read every tick, so none is
  pending at a save). A cell now blocking loses its liquid (an edit); each changed cell wakes as above. Doors open and
  close through Grid's writes, so they are in the log. When the log no longer reaches back (null), every cell is checked
  as `Rebuild()` does.
- **`Rebuild()`** after create and load: liquid in a blocking cell is removed (an edit), then the wake scan. A running
  world never holds liquid in a blocking cell, so for its saves this is a no-op; it matters for a save from before liquids
  (lakes grow into ground already dug and built: walls empty them, open tunnels let them flow) and for a retune of the
  lake numbers (unedited lakes move): accepted, the world flows to rest.
- **Queries** (`ILiquidsQueries`): `AmountAt(cell)`, `KindAt(cell)`; `Layer` — a byte a cell, amount | kind << 4,
  read-only, empty on a surface without liquid, for the client to copy what is in view; `FallNear(cell, reach)` — the
  nearest cell (squared distance, the lowest cell on a tie) liquid fell into last tick within `reach`, or null, from a
  derived list of the tick's falls.
- **Grown lakes:** L-2. **Client:** L-3 to L-5. **The look and the sounds are picked by the user** on Artifact pages
  before they are built.

## Determinism and co-op
- Inputs: the seed and grid writes (from commands). Integer units; rounding stated in the flow.
- Order: whole rows bottom-up, the direction from the saved `Clock`; skipping sleeping chunks never changes the result,
  so a loaded world and one kept running agree; the generic suites' save/load/play-on test proves it.
- Joining: the save's edits and `Clock`; the seed grows the rest again; `Rebuild()` wakes what can move.
- Client-authoritative: swimming is hero movement, read from each peer's own copy of the liquid.
- Grown lakes: a pure function of the type and the seed, hashed by new salts of their own (from 200, clear of the veins'
  and pockets', which end below 100 + `Terrain.MostVeins`); existing salts never renumbered.

## Trade-offs accepted
- A wall or a closed door built into a liquid cell destroys up to `Full` there: damming will be common, the loss a cell.
- A settled surface may keep 1 unit (a pixel) between neighbours; drawn flat by the client.
- A thin film of 1 unit at a pool's edge stays for ever (its neighbours differ by 1), saved as edits: a drained lake adds
  some thousands of edits to a save, at most its cells.
- Liquid passes machines (the drop pod) and is drawn through them; nothing under water is harmed.
- A hero in water to the ceiling swims freely with no surface to leave by: the suit is sealed, no drowning.
- Water does nothing to a suit: no cold, no burn, no cost; `Crew` is untouched in stage 1.
- Old planet saves grow lakes on load into ground already dug (`Rebuild()` above).

## Stages
1. **Water:** the module, grown lakes, the look, swimming, the sounds. One PR.
2. **Lava:** grown lava pockets in the ember band, lava moving every `Every` ticks, `Crew.Burns` reading liquid lava, the
   glow through `LightView`. One PR.
3. **Water meets lava;** taking water from lakes; rain with the weather. Each designed when it comes.

## Plan: stage 1, water

Branch `claude/liquids-c190ea`, one PR. Tasks in order; the look and sounds are picked while L-1 and L-2 are built.

### L-1 — The `Liquids` module
`sim/Liquids` (skill `new-module`): `LiquidsModule : Module<LiquidsState>`, `ILiquidsQueries`, Registry after Building,
before Crew, on the ship and every surface; `World.SaveFormat` bumped with an empty `Migrations` step and its fixture (a
save without the slice loads with the layout's liquid). `Content/Liquids.cs`: `Full` 8, `Fall` 4, `Chunk` 16. `Layout`
gains `Liquids` (a byte a cell as the `Layer` query, null for none, carried through `Doubled()` and `InWorld`); the
legend's `w` is a cell of water at `Full` over open ground (drawn maps until L-2 lands). The flow, wake and grid steps as
Approach says. README (Rules, API, Invariants, Deliberately not handled); `docs/map.md`; glossary "вода, озеро → water,
lake".
Tests (`LiquidsTests`): the scenarios at the end; FsCheck on random closed drawn maps: the total conserved, settled within
a bound and then still, never liquid in a blocking cell, amounts in 0..`Full`, every chunk with a cell that can move
awake; saved and loaded mid-flow equals kept running. The generic suites cover it once registered (its `Fuzz` has no
commands): Small's lake (L-2) lies where the fuzz's digs reach it, or a drawn map with water joins the suites' maps.

### L-2 — Grown lakes
`Terrain` fills `Layout.Liquids` last, after the ruins (sim/Core `Lakes.cs`). Numbers on `PlanetType` (First's and Small's
own): `LakeWave`, `LakeBands` [1, 2], `LakeChance`‰, `BigChance`‰, `LakeBig` 6,000 and `LakeMedium` 1,500 cells.
- Lattice points every `LakeWave` cells across and down, each moved within its square by the seed, visited row by row
  from the top, left to right; a point counts if its row is in band 1 or 2, it is open, and the seed's draw passes
  `LakeChance`.
- Its floor: straight down from the point through open cells to the first open cell over a blocking one. Its cap: `Big`
  by `BigChance`, else `Medium`.
- The fill: level L starts at the floor's row; region(L) = the open cells connected (four ways) to the floor through open
  cells in rows ≥ L. L rises a row at a time while region(L−1) holds at most the cap, every cell inside the zone, under
  the surface line (no sky cell), clear of the ruins' boxes and of earlier lakes; the lake is the last region that passed
  (none if even region(floor row) fails). Every cell full: a full cell's open neighbours below and beside are in the
  region and full, so it is settled by construction.

Tests: on First and Small and 50 seeds of Small: lakes full, settled (a tick moves nothing, nothing awake), within caps,
in the zone and bands 1–2, none on a ruin; growing is pure (the same seed, the same lakes); `Terrain.Generate` on First
before and after, stated in the PR.

### L-3 — Water drawn
`game/features/liquids/LiquidsView.cs`, a line in `SurfaceViews.cs` after the heroes (water over a swimming hero), unless
the picked look says otherwise: each frame the cells in view (and a cell round them) copied from `Layer` into an R8 image
and uploaded (a 1920×1080 view at zoom 1 is some 33 kB), drawn by a shader as the picked look; a run of wet top cells
(water with an open, dry cell above) drawn at the mean amount of its run, so a settled lake reads flat. Nothing is kept
between frames, so joining, loading and moving the camera need nothing more. A drawn surface kind `testLake`
(`Surfaces`, the planet's rules, like `TestStrip`): a lake on a ledge, a wall to dig into it, a shaft and a pool below; the
dev room `--lake` lands a hero beside it (skill `new-demo`).

### L-4 — Swimming
`game/features/crew/logic/Hero.cs`: the step's surface lookup (`Ship`, the record the controller reads the grid through)
gains the liquid's amount per cell. *In water:* the cell of the hero's middle holds at least `Hero.SwimDepth` (4, half a
cell) — worked out every step from the liquid, never stored, so a load, a join or water rising round a standing hero all
just swim. In water the hero moves as afloat (`Swim`), the keys at `Hero.WaterSwim` (thousandths of a cell a step, as
`Hero.Swim`), sinking at `Hero.WaterSink` with no key, Space or W up; Space with the cell over its middle holding under
`SwimDepth` and open is a jump, out of the water. A ladder in water is swum past, not climbed. `Pose` uses `Floating`
while in water. Unit tests in `game.tests`: into water from a jump, sinking, swimming up, the jump out, water rising
round a standing hero, water to the ceiling. Numbers in `sim/Content/Hero.cs`.

### L-5 — Sounds
The picked sounds cut, levelled and credited (`game/audio/CREDITS.md`); cues in `game/Content/logic/Sound.cs`: a splash when a
hero goes in water or out (from the step's in-water flag, at most one per `SplashGap` 0.4 s, so bobbing at the surface
does not chatter), a stroke every `StrokeEvery` while swimming and moving, one pouring loop at `FallNear(hero, 24)`, its
volume by distance.

### L-6 — Review and PR
`/review-implementation`; the user plays `--lake` and a grown planet; ADR `2026-11-07-liquids.md` holds what stays true
of stage 1 and the stages to come; this plan's stage 1 removed once merged.

### Scenarios (L-1)
A pool's wall dug: the water falls and pools, the level drops; water passes ladders, floors and open doors and stops at
a closed one; a door opened mid-flow lets it on; a wall built into water removes that cell's only; two basins joined end
within 1 unit; a U-tube does not rise; a fall of `Fall` cells a tick through an empty shaft; out of the zone gone; the
direction alternates with `Clock` and `Clock` survives a save; a fresh Small planet unchanged by a tick, nothing awake;
the ship never allocates the layer; the grid log overrun (null) with a wall built among the writes empties that cell and
wakes the rest; a save loaded mid-flow (beside the FsCheck one); a drawn map whose water lies in a wall loads with that
cell dry.
