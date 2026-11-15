# Catcher loot

- Status: accepted; built for planets, space waits for its catchers ([catchers-space](../plans/catchers-space.md)). Supersedes in
  part [tracker-catchers](2026-11-09-tracker-catchers.md) (what fills the meter, the bracelet's blink)
- Date: 2026-11-10

## Context

The meter filled from activity: moving, running, thrusting, slower under rock. That punished walking about as much as mining, and
the bracelet blinked from two thirds, so a player read the hidden meter and left before it filled. The user wanted light tension:
a player should not farm forever unpunished, hunts should read as random encounters, and nothing should show the meter. After
several rejected schemes (weights per action, dice per action, a visible fast meter, decay with a cap), the user's own idea: fill
it by the worth of what the player gets, so digging soil barely stirs it and greed does.

## Decision

- **Loot fills the meter:** `ICrewOps.Loot(peer, yield)` adds the yield's `Market.WorthOf` (soil and rock 1, ore 30, an opal 150)
  to the hero who broke or killed it, as it yields, whether picked up or not: a tool's hit on a wall or piece (`Digging`), a felled
  plant (`Flora`), a burst to its thrower (`Bursts`, a meteor's to no one), a wreck broken or caught (`Debris`), a creature killed
  (`Creatures`, by the striker). Counting what enters a pack was rejected: chests, furnaces, trades and friends' gifts would fill it.
- **Everything counts but taking apart** by plan or screwdriver (`Deconstruct`): the user's call, simplest. Breaking one's own
  walls with an axe counts. Moving counts for nothing; `Stirring`, `Wanted.Open`, `Under` and `ActiveFor` are gone.
- **The need** is a worth drawn per hunt, `NeedLeast..NeedMost` (4000..8000): placeholders for 10–20 minutes of greedy mid-game
  salvage, to be measured in play. Richer ground brings hunts sooner. One catcher at a time; a surplus past full is lost (several
  meters bringing several catchers: skipped for now).
- **The alarm:** filled, the meter draws `Coming`, `Wanted.ComingLeast..ComingMost` (30..60) breaths, saved on the hero, counted
  down each breath whatever the hero does. `Signal` is `Calm`, `Coming`, `Hunted` (full and `Coming` 0); the bounty sends a
  catcher only then. No blink before: the bracelet is steady while the meter fills, then blinks urgently (`TrackerBlink.Hunted`)
  from the alarm on; the hunted player's helmet beeps `CatcherBeep` as it turns `Coming` (the alarm's own sound is a later pick).
- **Not yet in space:** `Loot` fills nothing where things float until space has catchers, so no hunt waits for one that never
  comes. Leaving a surface still empties the meter.
- **Dev:** `Track(Short)` (`--catcher[=N]`) sets the meter that much short of full; 0 fills it, its alarm drawn.

## Consequences

- Save format 62: the old meter counted breaths; its step empties every hero's (`Wanted`, `Need`) and drops `Stirring`.
- A hero in creative mode fills nothing, as before. Loot no hero's hand broke (nets, drones, machines) fills nothing.
- The numbers are placeholders for a play pass.
