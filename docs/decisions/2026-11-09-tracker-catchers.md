# Catchers

- Status: accepted (slice 1, the planet); slices 2 and 3 are in [the plan](../plans/catchers.md)
- Date: 2026-11-09

## Context

Every hostile waited for the hero to come near: walking round them was always safe, and a hero far from home never felt
hunted. The user asked for enemies that come on their own, fitting the lore: the hero still wears its former owners' tracker
on its shin, and the owners hire catchers, a third party neutral to the warring factions, to bring it down.

## Decision

- **A hidden meter per hero, on Crew's `Hero`** (it travels between surfaces; Crew already charges place costs each breath):
  `Stirring` counts down `Wanted.ActiveFor` ticks (3 s) from the last `Enter` that moved its cell, run or thrust; each breath
  an awake, not creative hero off the ship that is stirring adds `Wanted.Open` (2) to `Wanted`, `Wanted.Under` (1) with a back
  wall behind its cell, up to `Need`, drawn from `NeedLeast..NeedMost` (1200..1800) when 0: a hunt at least 10 minutes of
  open-sky activity apart, up to 30 under rock. An idle hero's meter never fills. "Off the ship" is `!Rules.Floats`.
- **`Signal`**, what the bracelet shows, on every hero for everyone: `Calm`, `Blinking` from two thirds, `Hunted` full.
- **Any end empties the meter** (`ICrewOps.CallOff`): passing out, departing the surface (launching home), the catcher's
  time out or its death.
- **The bounty** (Creatures, on the beat): a hunted, awake hero here with no catcher gets one `RingFrom..RingTo` (124..136)
  columns to a drawn side, else the other, on the row nearest its own where the body fits out of every hero's view, so a
  deep hero is reached too; none, a later beat. Planets narrower than twice `RingFrom` get none.
- **The catcher** (`Lifetime.Hunter`, faction `catchers`, hunting crew), one species: a `Flyer` body 3×5 cells, `Hunt` drive,
  the sentry's pounce and shot. It hovers its warning (`Warn`, 10 s), flies at its quarry at 8 cells/s, pounces within
  `Strike` in line. Blocked, its body `Wear`s every cell of its next box every `CutEvery` ticks if all of them `Wears`:
  grown rock as it grew (`IGridQueries.GrownWall`, `Blasts`), never a built wall, a machine or bedrock. Out of `Ticks`
  (90 s) it calls the hunt off; its quarry out or gone, the same; then it flies up through anything, gone once unseen. A blow
  from another hero it answers with its shot, once per `AnswerEvery`, then turns back. Hp 40. Killed: scrap, a circuit, a crystal one in 5, an opal one in 10, and its `Purse`, 150..300
  credits earned by the ship (`IKnowledgeOps.Earn`): what it was paid for the hero. Catchers are worth hunting back.
- **`Release`** of a catcher hunts its sender; **`Track(Breaths)`**, a dev command as `Release` is (`--catcher`), brings a
  hunt that many breaths near.
- **Drawn as a hero** (the user, 2026-11-09): not a new creature but the hero's own figure and rig (`CreatureLooks.Heroes`,
  drawn by the crew feature's `HunterView`), no tracker, on a jetpack, a gun in hand. The gear page's four outfits all went in
  (the user: «все шикарные»), «Следопыт», «Упряжь», «Латы», «Клюв» (`subjects/catchergear.js`); a catcher wears one by its id, so
  every peer draws the same with nothing saved. Its receiver blinks as its quarry's bracelet, in its colour; its goggles light as
  the warning ends. The gear may later drop as a skin of the starting suit up to rare pieces with properties; not yet.
- **Warning picks** (the warning page): the bracelet «Маяк» (`TrackerBlink`), the beep «Трекер движения» (`CatcherBeep`, Freesound CC0),
  once in the helmet as a new catcher comes.
- **Client:** the bracelet blinks by `Signal` on every figure; the hunted player alone gets a beep as its catcher comes
  and a red reticle at the screen's edge pointing at it; a cut is heard by the material it broke or bit (`CutHeard`).

## Consequences

- No format bump: the meter's fields and the catcher's `Mem` are saved with "none yet" defaults; the brain order is pinned.
- A hero walled in by built walls outlasts its catcher: shelter is a real answer, paid in materials and a trip home.
- Not handled: space (slice 2), varied kits (slice 2), wearing built walls (waits for unified damage), paths (it flies
  straight and cuts), a number for the meter.
