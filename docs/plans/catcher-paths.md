# Design: Catcher paths, run, fly, break (and intercept)

Status: reviewed (design-reviewer, review-plan), revised with the user's answers. Follows [catchers](catchers.md),
[tracker-catchers](../decisions/2026-11-09-tracker-catchers.md) and [unified-damage](../decisions/2026-11-09-unified-damage.md).

**Asked by the user** (2026-11-09):
- Weighted A* for the path (a cell costs its time to pass); on a planet it runs, and flies only where flying or cutting
  upward is the best way, free to choose any direction; no complex behaviour system.
- It runs as fast as the hero walks. Lagging behind for long, it lifts off and lands a few cells ahead of the hero.
- It breaks the player's blocks too, by the one damage rule (another change takes that rule aboard the ship, so a catcher
  will reach a hero there through the hull); a replan goes round a new wall only if that is cheaper than breaking it.
- It cuts with a tool in hand, not by thought.
- In space it intercepts its hero rather than tailing it.

## Problem
A catcher flies straight at its hero and cuts whatever grown rock is in the way, bare-handed: it never runs or goes round a
hill, reads as a drone, stops at any wall a player built, and in space would trail a jetpack that outruns it.

## Player experience
1. A beep; ten seconds after it a catcher lands at the view's edge and runs at the hero over the hills.
2. A ravine between them: it does not climb down and up, it lifts on its jetpack and flies across.
3. The hero drops down a shaft into a cave: it flies down the same shaft; if the cave's only way in is far round, it takes
   out a drill and cuts down through the roof, heard digging closer.
4. The hero runs and keeps its lead: some seconds on, the catcher boosts up and lands across the hero's way, a few cells
   ahead.
5. The hero shuts itself in a scrap hut: the catcher drills through the cheapest wall, a second a cell; an iron bunker holds
   it some seconds; an open door it simply runs through. The walls it breaks drop their materials.
6. In line within `Strike` it pounces, as today.

Goes wrong: a hero inside bedrock, lava or the lifepod's shelter is out of reach; the hero's catcher holds at the nearest cell and
leaves when its time runs out. A hero sprinting the whole way (26.4 cells/s) outruns it between leaps, paying in air.

## Out of scope (deliberate omissions)
- **Paths for walkers** (leap and fall edges): with the carapace pack, on this same search.
- **Cracks in path costs:** a cracked cell is costed whole.
- **Moving obstacles** (other creatures, debris): no collisions (0006).
- **Space catchers and the ship's hull:** designed here, built in slice 2 of catchers.md with the space meter and spawn, and
  once the damage rule reaches aboard.
- **A new tool's look:** it holds the hero's drill (`hero_drill`, its spin frames); a catcher's own cutter would be a pick.
- **A landing animation:** the pose switches in a frame.

## Scale
Up to four catchers, at most one path search a tick on a surface. Each plans every `Replan` ticks (10); after a cut-short
search toward a goal that has not moved, at twice the interval. No commands, no events.

## Approach
**Owner.** The search lives in Creatures; its users are creatures only: the catcher now, the embermoth and driftbell next (fly
only, no cutting), the carapace pack after. It takes a body, its speeds, a start and a goal, and knows nothing of hunts.

**Why not a body as it is.** `Walker` falls under gravity, steers one way, simulates each leap tick by tick, and neither flies
nor cuts. `Flyer` flies and cuts but knows no ground. A catcher never falls uncontrolled: its jetpack holds it. So `Flyer`
gets `Run` (and `Leap`, below); ground changes only its speed and pose, no physics. It steps in parts of a cell, as `Walker`
does, so the leap's speed over a cell a tick never skips a thin wall (space needs the same).

**Breaking, by the one rule.** Its cut becomes a hit through Building's damage rule (unified-damage), not a rule of its own:
natural ground at its strength, a built wall, piece or shut door at its strength × 1000 / `BuiltTakes` (about 27×), rounded
up, as a burst pays. `Wears` is renamed `Breakable`, "a hit could break it": all but bedrock, lava, the fog and the lifepod, which stop a
burst too; where the rule later reaches (the ship), the catcher follows with no change here. Natural ground it breaks gives
nothing (a hunter is no mine); a built thing drops its cost at its cell for anyone (`Spoils.Drop`), so a player loses the
work, not the materials. Machines are not solid to bodies (a hero walks past them), so it never needs to break one. It strikes as no peer: what drops
is anyone's. A sealed hut it breaks vents like any breach; the beep and the drilling heard warned the hero.

**The graph.** A node is the body's bottom-middle cell (3×5 body). An edge is judged by the cells the body newly covers at its
end (the leading column of 5 or row of 3): free for Run and Fly, all breakable for Cut. Costs in hundredths of a tick,
rounded up:
- **Run:** left or right, up to `StepUp` (2) rows up or down, solid ground under the feet at both ends. `100000 / Run`.
- **Fly:** 8 directions through air; a diagonal only where both side cells are free, costing as a straight move. `100000 / Speed`.
- **Cut:** a straight move into breakable cells: the move plus, for the dearest cell newly covered, ceil(its cost / `Wear`) ×
  `CutEvery` (the cut strikes all of them together).
- **Impassable:** bedrock, lava, the fog, the lifepod.

| Units a tick (50 = 1 cell/s) | Ticks a cell | |
| --- | --- | --- |
| Run (720, the hero's walk) | 1.4 | 14.4 cells/s |
| Fly (400, as now) | 2.5 | 8 cells/s |
| Cut stone (120), `Wear` 120 | about 6.5 | one strike |
| Cut a scrap wall (20 → 541) | about 22 | five strikes, a second |
| Cut an iron wall (50 → 1352) | about 50 | twelve strikes, 2.5 s |

So it flies a gap only when walking round is over 1.8× longer, cuts rock when going round costs more than about 2.6 cells of
flight per cell cut, and goes round a scrap wall for up to some 9 cells of flight, an iron one for some 20: an open door near is always taken.

**The search.** A*, heuristic `Run` cost × Chebyshev distance × 3 / 2, rounded down: fewer nodes in stone, a path at most
1.5× the best. Bounded by a window round catcher and goal, `PathMargin` (24) cells wider each side, clamped to the map and to
a 256×256 buffer centred on the catcher (a goal outside it is aimed at through the buffer's nearest node); it stops at
`PathNodes` (3000) expanded. Cut short, it heads for the expanded node nearest the goal (least h, then least g,
then least cell). The working memory is reused and never saved, reset by a generation stamp, so a used buffer and a fresh one give the same
path (tested).

**Following.** `Hunt` steers at the saved path's next node, the move clamped to what is left of it, and takes the next node
within half a cell: at `Run` standing on ground on a level or step move, else at `Speed`. A rising edge moves up first, then
across, and a Run edge never cuts: `Flyer`'s X-then-Y step would bite the step it means to climb. It replans when due, when
the path runs out, or when the next step is no longer passable as the kind it was planned as (a Run or Fly step built over),
each node saved with its kind: the new path breaks the new wall or goes round it, by cost. A cost that fell (a door opened)
waits for the next due replan. Waiting for the search, it keeps its old path, or holds still with none. Warning, pounce and
answer are unchanged; leaving still goes straight up through all.

**The leap.** Chasing, if its Chebyshev distance to the quarry in cells has not dropped below its closest for `LeapAfter`
ticks (60, 3 s), it aims at a lead point: the flight's ticks are the present distance × 1000 / `Leap`, rounded down; the lead is
(the quarry's cell now − its cell when last sampled) × those ticks / `Sample`, rounded toward zero and capped at `LeadMost`
(12 cells) each way (a respawn's jump is capped too); the point is dropped to the first ground below. The sample is refreshed
every `Sample` ticks (10), so it is 0–9 ticks older than that. If there is no ground below, or the body (3×5) stepped along
the straight line meets solid, no leap: it keeps its path. Else it flies there at `Leap` (1400, 28 cells/s, just over a
sprint), its path set aside, and lands; the clock starts again. Mid-leap its quarry within `Strike` in line still draws the
pounce, and the hunt's end still ends it. The lead point is the shared part: space's
intercept is the same point, flown at the space speed.

**Saved.** A creature's `Path`: the nodes left to its goal with each step's kind, absent by default (`JsonIgnore` when null, so
other creatures' saves stay as they were); in `Mem` `planAt`, the leap's clock, its closest distance, the quarry's sampled
cell, and the leap's landing cell (none: not leaping). No format bump: a catcher with no path plans on its next tick.

**Space (slice 2).** The same planner and lead point; a clear line to the lead point is flown straight each tick, else a
path. Through a hull it breaks its way at the built share once the rule reaches aboard. Slice 2 sets a space speed, 500
proposed (the jetpack's top).

**Client.** Queries `Afoot(c)` (on ground, not rising) and `Cutting(c)` (struck in the last `CutEvery` ticks). `HunterView`
draws the run pose at its horizontal speed, plume off, else the air pose; cutting, it holds the hero's drill spinning toward
the cut, its gun away, sparks and the broken cell's sound as today.

## Does a running catcher reach its hero?
The hunt has 1800 ticks (90 s) after the warning. Running replaces flight only where cheaper and the straight cut stays in
the graph, so a full search is within 1.5× of the best way, at worst near today's straight flight; a cut-short one can be
slower. Rows count running at 1.4 ticks a cell and cutting stone at 4 (the body moves while the cut cools; the table's 6.5 is
the planner's cost, move plus cool-down, which over-states it).

| Case | Today | With paths |
| --- | --- | --- |
| Surface, 130 columns, path about 1.3× | 325 ticks | about 240 (12 s) |
| Cave 60 rows below the spawn | | about 420 (21 s) |
| Sealed in stone, 130 columns out, 30 cells of stone | | about 450 (23 s) |
| A scrap hut on the surface | never | about 300 (15 s) |
| Worst plausible: 130 across, 200 down, all stone | | about 1300 (65 s) |

The sealed and worst rows are estimates (3000 nodes reach some 27 cells in stone); the sealed-in-stone scenario measures them
and decides.

## Determinism and co-op
No new command; hero cells come by `Enter`. Integer costs and lead, rounded as stated; Content numbers that divide are checked above 0 in a test. Fixed neighbour order; a node's parent
changes only on a strictly cheaper path; the heap orders by f, h, cell. At most one search a tick: before creatures run, a pre-pass picks the catcher
due longest (a path run out or a step blocked is due at once), then by id; the rest wait. The search reads saved grid state and Content only. Path and `Mem` saved and hashed: a
joiner follows the same path. Heroes stay client-authoritative (0002); a hero's sampled cell is its last reported one.

## Trade-offs accepted
- A cut-short search may head into a dead-end pocket; it replans within half a second.
- Ground dug from under a running catcher: it hovers in the air pose till it replans; the jetpack explains it.
- It aims at a hero's cell up to 10 ticks old (0.5 s); the pounce takes over within 12 cells.
- A lead point from a reported cell up to a second old may land behind a hero who just turned; the next leap corrects it.
- A shelter is no longer an answer on a planet except bedrock and the lifepod: the user's call; a fight or a sprint is.

## Tests
- Scenarios: flat ground, it runs (`Afoot`), arrives and pounces; a short dip stepped through; a wide ravine flown; a hero up
  a shaft, flown up; a hill with a short way round, gone round; with a long way round, cut through; a cave under a thick roof,
  cut down; a scrap hut with an open door, entered by the door; shut, a wall broken, its cost dropped, while broken rock drops
nothing;
  bedrock round the hero, held at the nearest cell; a wall built across its path, broken or gone round by cost; a run up a
  one-cell grown step leaves the step unworn; a hero running away in the open, a leap lands ahead of it; two due on one
  tick, one search, in order; saved mid-path and mid-leap, loaded and played on alike; sealed in stone 130 columns out,
  caught in time; two worlds kept alike through a hunt with cutting and a leap.
- Invariants (FsCheck, random rock, air, built walls, bedrock): consecutive nodes an allowed edge; never bedrock, lava, fog;
  at most `PathNodes` expanded; the same path from a saved and loaded world. Bites: bedrock costed passable once. The search on a fresh and a used buffer alike.
- Budget: four catchers on the reference planet, the hero sealed so each search spends all 3000 nodes, under 2 ms a tick
  (estimated 0.3–0.6 ms a search, unmeasured).

## Cost
Creatures (the search, `Hunt` following and the leap, `Flyer.Run`/`Leap`, sub-cell steps, `Path`); Building (`Wear` through
the damage rule's built share and `Spoils.Drop`); the crew client (`HunterView` run pose and drill). Content: `Run`, `Leap`,
`Replan`, `PathMargin`, `PathNodes`, `LeapAfter`, `Sample`, `LeadMost`; slice 2: the space speed. Docs: the catcher ADR
updated (shelter no longer holds it), the Creatures and Building READMEs. About 750 lines with tests.

## Open
- **Space speed** (slice 2): 500, the jetpack's top (recommended), or 400, slower than a flying hero: it never catches one, and only the hero's air running out ends the chase.
