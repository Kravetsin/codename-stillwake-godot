# Design: Catcher paths, run, fly, cut (and intercept in space)

Status: reviewed (design-reviewer), open questions below. Follows [catchers](catchers.md) and [tracker-catchers](../decisions/2026-11-09-tracker-catchers.md).

**Asked by the user** (2026-11-09): weighted A* for the path (a cell costs its time to pass); on a planet it runs, and flies
only where flying or cutting upward is the best way, free to choose any direction; no complex behaviour system; in space it
should intercept its hero, not tail it.

## Problem
A catcher flies straight at its hero and cuts whatever grown rock is in the way: it never runs or goes round a hill, and on a
surface it reads as a drone, not a hunter. In space, tailing alone, a jetpack outruns it.

## Player experience
1. A beep; ten seconds on a catcher lands at the view's edge and runs at the hero over the hills.
2. A ravine between them: it does not climb down and up, it lifts on its jetpack and flies across.
3. The hero drops down a shaft into a cave: it flies down the same shaft; if the cave's only way in is far round, it cuts
   down through the roof, heard digging closer.
4. In line within `Strike` it pounces, as today.

Goes wrong: the hero behind walls it built. The catcher runs to the outside cell nearest it, holds, and leaves when its time
runs out. A sprinting hero (26.4 cells/s) leaves it behind, paying in air.

## Out of scope (deliberate omissions)
- **Paths for walkers** (leap and fall edges): with the carapace pack, on this same search.
- **Leading the hero on a planet:** terrain bends the hero's way; a predicted cell often lies in rock.
- **Cracks in path costs:** a cracked cell is costed whole.
- **Moving obstacles** (other creatures, debris): no collisions (0006).
- **Space catchers:** designed here, built in slice 2 of catchers.md with the space meter and spawn.
- **A landing animation:** the pose switches in a frame.

## Scale
Up to four catchers, at most one path search a tick on a surface. Each plans every `Replan` ticks (10); after a cut-short
search toward a goal that has not moved, at twice the interval (a hero sealed in its base costs a full search each time).
No commands, no events.

## Approach
**Owner.** The search lives in Creatures; its users are creatures only: the catcher now, the embermoth and driftbell next (fly
only, `Wear` 0), the carapace pack after. It takes a body, its speeds, a start and a goal, and knows nothing of hunts.

**Why not a body as it is.** `Walker` falls under gravity, steers one way, simulates each leap tick by tick, and neither flies
nor cuts. `Flyer` flies and cuts but knows no ground. A catcher never falls uncontrolled: its jetpack holds it. So `Flyer` gets
one number, `Run`; ground changes only its speed and pose, no physics.

**The graph.** A node is the body's bottom-middle cell (3×5 body). An edge is judged by the cells the body newly covers at its
end (the leading column of 5 or row of 3): free for Run and Fly, all wearable for Cut. Costs in hundredths of a tick, rounded up:
- **Run:** left or right, up to `StepUp` (2) rows up or down, solid ground under the feet at both ends. `100000 / Run`.
- **Fly:** 8 directions through air; a diagonal only where both side cells are free, costing as a straight move (the body
  moves at full speed on its longer axis). `100000 / Speed`.
- **Cut:** a straight move into cells `Wears` allows: the move plus ceil(strength / `Wear`) × `CutEvery`.
- **Impassable:** a cell `Wears` refuses (built walls, machines, bedrock, lava, fog).

| | Speed | Ticks a cell | Cells a second |
| --- | --- | --- | --- |
| Run | 720 | 1.4 | 14.4 (the hero's walk) |
| Fly | 400 (as now) | 2.5 | 8 |
| Cut stone, `Wear` 120 | | about 6.5 | |

So it flies a gap only when walking round is over 1.8× longer, and cuts only when the detour costs over about 2.6 cells of
flight per rock cell.

**The search.** A*, heuristic `Run` cost × Chebyshev distance × 1.5 (a constant): fewer nodes in stone, a path at most 1.5×
the best. Bounded by a window round catcher and goal, `PathMargin` (24) cells wider each side (a 256×256 buffer, a constant);
it stops at `PathNodes` (3000) expanded. Cut short, it heads for the expanded node nearest the goal (least h, then least g, then least
cell): this is also how it holds outside built walls. The working memory is reused and never saved.

**Following.** `Hunt` steers at the saved path's next node, the move clamped to what is left of it, and takes the next node
within half a cell: at `Run` standing on ground on a level or step move, else at `Speed`. A rising edge moves up first, then
across, and a Run edge never cuts: `Flyer`'s X-then-Y step would bite the step it means to climb. Fly and Cut edges move and
cut as today. It replans when due, when the path runs out, or when the next node turned impassable
(built over). Warning, pounce, answer and leaving unchanged; leaving still goes straight up through all.

**Saved.** A creature's `Path`: the nodes left to its goal, empty by default; `Mem` `planAt`. No format bump: a catcher
with no path plans on its next tick.

**Space (slice 2).** The same planner; only the goal differs: the lead point, its quarry's cell plus (its cell now − its
cell 5 ticks ago) × distance / speed, capped by one constant; the rest written in slice 2, when it can be played. A clear line to the lead point: straight there each tick; else a path. A hero aboard is behind
built hull, so it holds at the ship's edge, as slice 2 asks. Slice 2 needs a space speed near the jetpack's top (500) and
bodies moving in parts of a cell over a cell a tick, as `Walker` does, or a fast body skips thin walls.

**Client.** A query `Afoot(c)`: on ground, not rising. `HunterView` draws the run pose at its horizontal speed, plume off;
else the air pose, as today.

## Does a running catcher reach its hero?
The hunt has 1800 ticks (90 s) after the warning. Running replaces flight only where cheaper, and the straight cut stays in
the graph, so a full search is never slower than today's straight flight; a cut-short one can be.

| Case | Today | With paths |
| --- | --- | --- |
| Surface, 130 columns, path about 1.3× | 325 ticks | about 240 (12 s) |
| Cave 60 rows below the spawn | | about 420 (21 s) |
| Sealed in stone, 130 columns out | | about 520 (26 s) |
| Worst plausible: 130 across, 200 down, all stone | | about 1320 (66 s) |

The sealed and worst rows are estimates (3000 nodes reach some 27 cells in stone); the sealed-in-stone scenario
measures them and decides. Cutting moves about 4 ticks a cell in practice: the body moves while the cut cools. `Wear` went to 120 with unified damage's
tenths (PR 513); at 12 the deep cases missed the 90 s.

## Determinism and co-op
No new command; hero cells come by `Enter`. Integer costs, rounded as stated. Fixed neighbour order; a node's parent changes
only on a strictly cheaper path; the heap orders by f, h, cell. At most one search a tick: due catchers in order of due tick,
then id; the rest wait. The search reads saved grid state (`Solid`, `Wears`) and Content only. Path and `planAt` saved and
hashed: a joiner follows the same path. Heroes stay client-authoritative (0002).

## Trade-offs accepted
- A cut-short search may head into a dead-end pocket; it replans within a second, and cutting rarely leaves it stuck.
- Ground dug from under a running catcher: it hovers in the air pose till it replans; the jetpack explains it.
- It aims at a hero's cell up to 10 ticks old (0.5 s); the pounce takes over within 12 cells.
- On a surface: a 30-cell run, a 40-cell flight, a run. The look wanted.

## Tests
- Scenarios: flat ground, it runs (`Afoot`), arrives and pounces; a short dip stepped through; a wide ravine flown; a hero up
  a shaft, flown up; a hill with a short way round, gone round; with a long way round, cut through; a cave under a thick roof,
  cut down; a built-wall ring, held at the nearest outside node, the wall never worn; a wall built across its path, replanned;
  two due on one tick, one search, in order; a run up a one-cell grown step leaves the step unworn; saved mid-path, loaded and played on alike; sealed in stone 130 columns out,
  caught in time.
- Invariants (FsCheck, random rock, air, built walls, bedrock): consecutive nodes an allowed edge; never a cell `Wears`
  refuses; at most `PathNodes` expanded; the same path from a saved and loaded world. Bites: a built wall costed passable once.
- Budget: four catchers on the reference planet, the hero sealed so each search spends all 3000 nodes, under 2 ms a tick
  (estimated 0.3–0.6 ms a search, unmeasured).

## Cost
Creatures (the search, `Hunt` following, `Flyer.Run`, `Path`); the crew client (`HunterView` pose). Content: `Run`, `Replan`,
`PathMargin`, `PathNodes`; slice 2: the lead cap, the space speed. Docs: an ADR, the Creatures README. About 600 lines with tests.

## Open
- **`Run` speed:** 720, the hero's walk (recommended); 600, a walking hero slowly outpaces it; 880, only a sprint does.
- **Space speed** (slice 2): 500, the jetpack's top (recommended): a hero fleeing straight holds its distance, and leading cuts
  off one that turns; or 400, only the hero's air ends it.
- **Space now or next:** next, with slice 2 (recommended): nothing in space fills the meter or spawns a catcher yet.
