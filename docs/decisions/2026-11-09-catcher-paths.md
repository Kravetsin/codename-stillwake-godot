# Catcher paths

- Status: accepted; built for the planet. Supersedes in part [tracker-catchers](2026-11-09-tracker-catchers.md) (its warning
  hover, its straight flight, its cut of grown rock only, shelter holding it)
- Date: 2026-11-09

## Context

A catcher flew straight at its hero and cut grown rock bare-handed: it never ran or went round a hill, read as a drone, and
stopped at any wall a player built. The user asked for a weighted A* (a cell costs its time to pass), running on the ground
at the hero's walk, flying only where flying or drilling upward is the best way, any direction, no behaviour system; breaking
the player's blocks by the one damage rule ([unified-damage](2026-11-09-unified-damage.md)), going round a new wall only if
cheaper; a tool in hand when it cuts; and, lagging for long, a leap that lands a few cells ahead of the hero. Space intercepts
its hero rather than tailing it, designed now and built with slice 2.

## Decision

- **The search** (`Creatures/Paths.cs`), for creatures only: the catcher now, flyers and walkers next. A node is the body's
  bottom-middle cell. Edges, judged by the cells the body newly covers: **Run** (a column, up to `StepUp` rows up or down,
  ground under the feet at both ends, `100000 / Run` hundredths of a tick), **Fly** (eight ways through air, a diagonal only
  with both sides and its corner free, `100000 / Speed`), **Cut** (straight into cells that all break: the flight plus the
  dearest cell's strikes × `CutEvery` × 100). Heuristic: the run's cost × Chebyshev × 3/2, rounded down. A window
  `Paths.Margin` (24) round start and goal, clamped to the map and a 256-cell square round the start; at most `Paths.Nodes`
  (3000) places weighed; cut short, it heads for the place weighed nearest (least estimate, cost, cell). Each cell is asked
  of the world once a search; its memory is reused, apart by a stamp, never saved.
- **One search a tick:** before creatures run, the catcher due longest (then the least id) searches; the rest wait. A path
  is due every `Paths.Replan` (10) ticks, twice that after a search fell short of an unmoved goal, and at once when it runs
  out or its next step is no longer passable as planned.
- **Following:** a creature's saved `Path` holds each node with its kind. Running, it rises before moving across up a step and
  moves across before dropping down one, at `Run` (1320, the hero's sprint: a playtest found the walk's 720 a stroll) a tick
  through the corners of its run steps, a step up or down free as a hero's, so it never halts at one; a tick's run may pass
  several nodes. Otherwise at `Speed` (400). A Cut step sets it cutting. Rising, its body moves up first, else across first. A
  search never sends it back to the middle of its cell when the way on is clear.
- **No warning:** it hunts from the tick it comes (a playtest found it hanging still for its 10 s); the beep, the reticle
  and its spawn out of view, some 5 s off at a sprint, warn the hero.
- **Breaking by the rule:** `Building.Wear` strikes as a burst does: ground at its strength, giving nothing (a hunter is no
  mine); a built wall or piece at the built share, dropping its cost for anyone (`Spoils.Drop`). `Strikes(cell, damage)`
  says how many strikes break a cell, null where none does (bedrock, lava, fog, the lifepod). Catcher `Wear` 120, every
  `CutEvery` 4: stone in one strike, a scrap wall in five, an iron one in twelve. `IGridQueries.GrownWall` is gone.
- **The leap:** chasing, if its distance to its quarry has not dropped below its closest for `LeapAfter` (60) ticks, it aims
  at a lead point: the quarry's drift over `Sample` (10) ticks times the flight's ticks at the closing speed, plus
  `LeapAhead` (3) cells its way, capped at `LeadMost` (64), raised out of ground by up to `StepUp` and dropped to the ground
  within `Margin`. It flies at `Leap` (2400, 48 cells/s) up to an apex over the midpoint, then down to land; a blocked arc
  means no leap. The target is saved, so a joiner flies the same arc.
- **Drawn:** `Afoot` (on ground, not rising) shows the hero's walk pose with the jetpack's plume off, else the air pose;
  `Cutting` (struck within `CutEvery`) puts the hero's drill in its hand, spinning toward the cell it bit.
- **Space (slice 2):** the same planner and lead point flown at a space speed (500 proposed, the jetpack's top), straight
  where the line is clear; through a hull once the damage rule reaches aboard.

## Consequences

- Shelter no longer holds a catcher on a planet, except bedrock and the lifepod: a fight or a sprint is the answer.
- No format bump: `Path` is written only when set, the hunt's new `Mem` keys default to none.
- Not handled: paths for walkers (leaps and falls), cracks in a path's cost (a cracked cell is costed whole), moving
  obstacles, a landing animation, a catcher's own cutter (it holds the hero's drill). A cut-short search may enter a pocket
  and replan; ground dug from under a runner leaves it hovering till it replans; it aims at a cell up to half a second old.
- Four catchers searching through rock on the first planet cost about 0.6 ms a tick (Release, a desk;
  `BudgetTests.KeepsAPlanetsTickWithFourCatchersSearchingThroughRockUnderBudget`).
