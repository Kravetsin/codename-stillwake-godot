# Flowing liquids: lakes in a planet's cells

- Status: accepted (stage 1, water); lava, water meeting lava, taking water and rain are later stages in
  [docs/plans/liquids.md](../plans/liquids.md)
- Date: 2026-11-08

## Context

A planet's caves were dry and nothing was let loose by digging. The user asked for flowing liquid: lakes in the caves to
find, swim in and dig open, water that pours out of the hole and pools below. Water came first; lava, its meeting with water,
taking water from a lake and rain follow. Playtests reshaped the flow and its drawing several times: the user's measure was
that it looks right, "as Terraria does it", over any physics.

## Decision

- **A module, `Liquids`**, after `Building` on every surface and on the ship, where its layer is never allocated. A cell holds
  a byte: 0 to `Full` (8) units in bits 0–3, the kind in 4–6. Eight levels settle; 240 crept for 30,000 ticks without
  resting (the plan's measure). Liquid enters only cells that do not block (`IGridQueries.Blocks`), dry or of its kind. A cell
  that comes to block loses its liquid the tick it is read. Liquid moved outside the zone is gone, drained by the fog.
- **The flow, as Terraria's.** Rows sweep from the bottom up, the way along each row turning with the saved `Clock`. Each
  row first falls, every cell of it, a cell a tick into the cell under it, even part-filled, so falling water is a whole
  column, never lumps with gaps between. Then what did not fall spreads half its lead to a side neighbour at least 2 lower.
  A lone unit runs off a ledge whole. A cell given liquid sideways moves it on only the next tick. `Fall` started at 4; the
  gaps it left in streams could not be drawn well, and 1 with the row's two passes gave whole columns.
- **Pressure and puddles**, after the sweep (the playtest of 2026-11-07). A body of full cells joined side to side moves up
  to `Press` (8) units a tick from its highest free surface to the lowest open cell beside it, so a U-tube rises and a pool
  settles within a unit. A run of resting cells short of full levels along its row. A run on the ground alone holding `Dry`
  (2) or less and unable to move dries up, as Terraria's thin water does, so no film is left on rock.
- **Awake chunks.** Squares of 16 cells sleep while none of their cells can move. Any write wakes the chunks of the cell
  and its eight neighbours: a lone unit runs off by the cell across its corner, so four left a chunk asleep that a peer
  which just loaded the world had awake (found in review, a desync). The set is derived. A world that skips sleeping chunks
  runs as one keeping every chunk awake, which a test holds tick by tick.
- **Saved as edits.** A grown surface's lakes grow again from its seed with its ground, so a save holds only the cells that
  differ, as the grid's edits do (0077). A drawn map's base is none. Format 56; an older save gets its lakes at its load.
- **Lakes grow** last in `Terrain.Generate` (sim/Core `Lakes.cs`). A lattice point every `LakeWave` cells in `LakeBands`
  may hold one, at the floor under it. Its level rises while the region stays in the zone, clear of ruins and earlier
  lakes, and under `LakeBig` (6,000) or `LakeMedium` (1,500) cells. A grown lake is at rest.
- **Swimming is the client's**, as hero movement is (0002). A hero swims once the cell of its middle holds `SwimDepth` (4)
  units, at `WaterSwim` along each axis, and sinks at `WaterSink`. Space or a shore lets it out with a leap. Feet
  `WadeDepth` (2) deep splash and nothing more. It floats in the pose of space.
- **Drawn as Terraria draws it** (`game/features/liquids`), over the heroes so a swimmer is seen through the wash. Each
  cell is filled from its bottom to its level, or whole where water is over it. A surface is blended toward its nearer
  neighbour's, so a pool reads flat. Falling water is streaked; where the sim lets it fall in strips a row or two apart, or
  in trickles a column apart, the gaps are drawn as falling water. Levels ease between ticks; a dried cell goes at once,
  for sinking out, cells drained together each left a line on their floor. Lit as the ground is.
- **Heard:** a splash going in and coming out, at most one a `SplashGapS`; a stroke while swimming and moving; one
  pouring loop at the stream nearest the view's middle. The water audition's picks, cut and credited in `CREDITS.md`.

## Consequences

- Digging near a lake can flood a cave: the stream, the pool below and a swim out of it are the planet's new event.
- A lake pouring out costs the first planet's tick about 1 ms on a desk, against 0.13 ms idle (`BudgetTests`).
- The flow's rules were chosen to look like Terraria's, not to conserve: units are lost to the fog and to drying.

## Deliberately not handled

- No liquid on the ship, no drowning (the suit is sealed), creatures and floor stacks move as through air.
- A shut hatch lets liquid through as a floor does.
- A full column whose head lies more than 128 rows under the view, or a sheet wider than 16 columns past it, is drawn
  lying till the view nears: whether it falls is known from its head.
- Nothing takes water from a lake, it does not evaporate, and no rain falls yet.
