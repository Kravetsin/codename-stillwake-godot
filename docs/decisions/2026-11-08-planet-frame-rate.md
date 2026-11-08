# Planet frame rate

- Status: accepted
- Date: 2026-11-08

## Context

On the planet dev room (`--planet --creatures=capback --manual`) the user saw about 50 fps while digging down and 100+ while
not, both under what a mid-range PC should give. Measured on a desk with vsync off, in the Debug build the user runs, with
`--frames` and a new `--ablate` (each node's processing switched off in turn, the frame time it saves printed) and a scripted
`--dig` (the ground under the hero hewn at the drill's pace):

| Run   | Before                          | After                           |
| ----- | ------------------------------- | ------------------------------- |
| Idle  | 6.27 ms a frame (159 fps)       | 2.61 ms (383 fps)               |
| Dig   | 8.32 ms (120 fps), p99 16.2, max 34 | 3.97 ms (252 fps), p99 7.3, max 18 |

The sim tick was never the cause: 0.14 ms idle, 0.24 ms digging (a test with a Stopwatch).

## Decision

- **Idle: the water drawn skips dry rows by their bytes.** `WaterCells.Write` looked at every cell of the view and the 128 rows
  under it, about 51,000, every frame, to find none: 3.6 ms. A texel can hold water only with water within `Gap + 1` rows, so
  rows are counted wet by `ContainsAnyExcept` and those with none near are cleared. A property test draws alike with and without
  the skip.
- **Dig: each wall broken cost three things.**
  - *Occupancy upload, 2.1 ms → 0.06.* The lightmap sent the whole planet's occupancy texture per change. It is now a
    `Texture2DArray` of 256×256 tiles laid out as `StarTiles`'; a change sends only the tiles it touched.
  - *Starlight follow, 4.6 ms → about 1.3.* `StarField.Follow` redid a square as wide as daylight reaches down a shaft (53
    steps). Sideways and upward steps keep at most the largest share, which fades to faint in 17; only downward steps reach 53.
    The box redone is now grown 17 across and over, 53 under, and the box read from grows the other way. A property test digs
    under shafts and caps and compares with a whole redo, byte for byte.
  - *Felled growth, 3 to 8 ms → microseconds.* `CaveView` built a set of the planet's 57,000 plants per felling to find the
    one gone. `Felling.Take` halves the kept cells: plants only shrink and both lists ascend by cell, so a run whose ends stand
    with as many plants between them lost none.
- **Chips drawn as one list of triangles.** Played after the first fixes, digging still dipped, worse underground and worse
  at ×4: stone throws chips where soil throws dust, and each chip was a transform and a polygon of its own, its points
  copied to Godot and triangulated, every frame (a sample profile put a third of the frame there). `ChipShape` turns and
  sizes the corners itself and the frame's chips go as one triangle array. 45 s of `--dig`: ×1 4.03 → 3.48 ms (248 → 287
  fps), ×4 (`--speed=4`) 5.77 → 4.29 ms (173 → 233 fps), p99 9.1 → 7.2. Standing still deep down costs 3.1 ms: depth alone
  was not the cause. Chips now draw over the frame's other matter rather than among it in the order thrown.
- **A kind's first sight is ready before it.** The user's own run, logged (`--frames`, 85 s): 351 fps, p99 4.9 ms, the GC 510
  times at 0.6 ms a pause (2.7 at most), so not the freezes; a creature first seen cost 32 ms, as one did near a spire, its
  guards first seen. Each kind's pictures were read from disk and made on its first sight (14 pictures, 13 ms to read).
  `CreatureParts.Warm` reads and makes every kind's once a run, as a planet with creatures loads (160 ms there); the first
  loper costs 7 ms, the next ones 0.2 to 2. An individual's markings are still made at its first sight, 1.7 ms.
- **Hits on the ground throw no chips** (the user's ask): they fell to one floor under the hit and lay in a sheet over any
  step. Sparks and dust in the air stay.
- **The hologram keys stay still unless the pointer nears** (the user's idea) and draw again only when what they show changes;
  the energy tab draws its ring and count when they change, the bolt, whose beat shows the draw, every frame.
- **Budget tests** (`FrameBudgetTests`, run apart from the others) hold each fix to a fraction of a frame on a desk, times
  `STILLWAKE_BUDGET_SLACK` on CI.

## Deliberate omissions

- Rare frames of 8 to 10 ms remain while digging at ×4 (p99.9); a sample profile names no one cause for them. Not chased.
- The real drill's client path (mouse held) is not measured: driving input would move the user's cursor. `--dig` sends the
  same `HewCells` at the drill's pace with the pickaxe.
- The dev run uses the Debug build, about 2.4 times slower than Release for this code in tests. A release build plays faster;
  we do not optimise the Debug build.
- The remaining matter (dust motes, wisps) is still drawn a rectangle at a time: under a millisecond at ×4.
- No chunks are unloaded: depth and ground explored add about half a millisecond (2.6 ms standing on the surface, 3.1 deep
  down); Godot culls what is out of view and the sim's whole-world tick is 0.03 ms. The costs that grew with the world were
  whole-planet scans per change, fixed here; `BuildingView.PlatformsOf` still scans the planet per change while a platform
  card is in hand.
