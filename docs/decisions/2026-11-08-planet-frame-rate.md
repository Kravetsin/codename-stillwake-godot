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
- **Budget tests** (`FrameBudgetTests`, run apart from the others) hold each fix to a fraction of a frame on a desk, times
  `STILLWAKE_BUDGET_SLACK` on CI.

## Deliberate omissions

- About 16 frames of 5 to 8 ms per 28 s of digging remain in untimed node work; `--ablate` found no single node. Not chased.
- The real drill's client path (mouse held) is not measured: driving input would move the user's cursor. `--dig` sends the
  same `HewCells` at the drill's pace with the pickaxe.
- The dev run uses the Debug build, about 2.4 times slower than Release for this code in tests. A release build plays faster;
  we do not optimise the Debug build.
