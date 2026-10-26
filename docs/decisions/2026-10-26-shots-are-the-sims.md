# Shots are the sim's

- Status: accepted; the nail built (N-1 of `docs/plans/nailer.md`); the nailer in hand and the sentry's bolt to come
- Date: 2026-10-26

## Context

Nothing fought at range: the axe reaches 4.5 cells, the drill 6.5, the sentry only pounces. The first ranged weapon is
to come almost at the start: a nailer, a nail a shot every 3 s, nails made of scrap. The sentry is to answer a shooter
past its pounce with an electric bolt. The player's word: every peer sees every shot, as in Terraria, and a nail breaks
blocks as the axe does. [Creatures are composed](2026-10-26-creatures-are-composed.md) had placed shots in Creatures, to
come with their first user ("hero guns will send their `Fire` here").

## Decision

- **The sim flies every shot, in Creatures.** `Fire(Tool, X, Y, Heading)` spends one of the gun's ammo (`Content.Shots`;
  none in creative) and adds a `Shot { Id, Projectile, By, X, Y, VX, VY, Left }` at the muzzle. Shots are saved with the
  slice, so every peer and a joiner see the same shots; none in flight writes nothing.
- **One flight, shared with the client.** `ShotFlight` in Core, whole numbers: `Launch` (the speed along the heading,
  rounded toward zero; `Reach` cells' ticks, rounded down), `Gravity` (`Creatures.Gravity × Weight` under the surface's
  gravity, none where it floats: open space and the ship, aboard too) and `Step` (a tick less left, then a walk by `Rays.Walk`
  from the old point, its own cell first, falling by its fall and half the gravity, rounded down, the fall then gaining the
  whole: the tick's mean, so the arc is the parabola's at every tick, ≈30.6 cells and not 28). The walk only finds: at an
  exact corner the x cell first; nothing changes during it, so one shot meets one thing; it also gives the last cell it
  asked, where a shot out of reach or off the grid ends. The shooter's client draws its own shot by the same step.
- **What a shot meets**, in the first cell its walk stops at, in that order: a solid cell (a creature's solid: walls,
  floors, closed doors, the grid's edge, a planet's fog); then the lowest-id live creature whose box overlaps the cell; then
  the first wreck a hit there strikes (`IDebrisQueries.Wrecks`: wreckage, its middle in space, with its box, taken once a
  tick, as 640 far pieces scanned per cell cost 6 ms a tick). Heroes are passed. Shots fly in id order after the creatures
  move, one fired this tick included; one out of reach or off the grid ends. `Ended` lists the tick's ends for the client,
  derived. The creatures a shot killed are swept out as the shots end: `Dead` is never saved, so one left listed till the
  next tick's sweep came back alive in a save made that tick.
- **What a hit takes**, from the projectile's row: a creature the `Damage` through the hit path, knocked by `Pace` along
  the shot's `VX` (straight up or down, away from the shooter's hero); a wreck the `Damage` through `IDebrisOps.Hit`, the
  body `HewWrecks` now calls too; a solid cell the `BlockDamage` through `IBuildingOps.Hit`, Building's first interface:
  `Hew`'s path for one cell, where the surface's tools break things, on a wall that breaks or a built piece that stops a
  body or a floor, its yield to the shooter if its hero is on this surface (Building judges), else falling there for
  anyone. Creatures depends on Building, which runs before it and reads nothing of it.
- **A blow says when it is answered.** The sentry answers blows on its wreck by the debris clock. A hit now carries
  `Answered`: the clock and one from a command (it lands before the debris drifts), the clock from a system (a shot lands
  after), so a nail on the sentry and an axe on its wreck the next tick are both answered.
- **The nail:** 60 cells a second; weight 7, so 294 a tick on the first planet and about 30 cells at 45°; gone at 288 cells,
  half a screen and a whole one past it at the farthest zoom (192 cells); 6 off a creature or a wreck, 1 off a block, a
  full knock. The nailer: 1 tube, 2 plates, 2 wire, 15 s at the pod; nails 2 scrap each, 1 s there, 100 a stack.

## Consequences

- The client's word stays where 0034 put it: the muzzle (only checked on the grid) and the 3 s pace. Friends are trusted.
- A drawn nail may part from the sim's by a tick, or fly on with no shot behind it when its `Fire` is refused as it
  applies: cosmetic.
- A world's hash covers shots in flight; a save mid-flight plays on alike.
- The fuzz draws `Fire`, arming its sender now and then (creative mode, a nailer, nails) so shots fly in the generic
  suites; its new draws shifted one desync test's: its seed 9 became 10.
- A creature never overlaps a solid cell, nor a clamped sentry its wreck, so the order solid → creature → wreck is a
  tie-break the game rarely meets: a sentry in flight over another wreck is the one case.

## Rejected

- **The nail flown by the shooter's client** with hits reported as `Strike`/`Hew`/`HewWrecks` and a `Shot` in the hero's
  sample for friends: two flights, one per kind of shooter, and friends' replay landing where the shooter's did not.
- **A shot that breaks no blocks** to spare Creatures a dependency on Building: the player's word is that it breaks them.
- **A third damage number for wrecks:** the nail's 6 breaks a wreck at half the axe's pace, from afar; the harpoon still
  tows a big one to friends.
