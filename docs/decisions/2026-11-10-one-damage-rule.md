# One damage rule for every hit

- Status: accepted; built; supersedes in part [damage-scale](2026-11-09-damage-scale.md) (item 2: only a charge took a
  tenth aboard, and tools aboard broke nothing)
- Date: 2026-11-10

## Context

[hit-targets](2026-11-10-hit-targets.md) gave every hitter one answer to what a hit strikes, but each still decided how much
it took. A charge thrown aboard took a tenth; a tool aboard took nothing and was refused (`building.notHere`); a shot
aboard took nothing; tools and shots passed through stations everywhere. So a player swinging at a station or a door saw
no spark, no crack and no sound, and could not tell whether it was hurt.

## Decision

1. **One rule.** A player's hit on their own ship takes a tenth (`Damage.OwnShipTakes`), whatever made it: a tool, a shot,
   a carry or a charge. Every other hit takes in full: meteors, creatures, and anything a player hits in a vessel or on a
   planet. "Their own ship" is `Aboard(cell)` (outside a vessel's box, where the surface's tools do not break freely), and
   never a pile, which is a wreck in the hold. `Targets.cs` holds it once: `OwnShip`, `CostOf` (what breaking costs, rounded
   up) and `CrackOf` (what a hit that does not break leaves, rounded down); `Breaks` asks it for the client.
2. **Everything a hit reaches is struck.** A fan's ray strikes a station it crosses, by its top-left, and goes on through it
   as through a ladder, so one swing strikes a station and the wall behind it once each. A shot meets a station as a pass of
   its own (`ShotTarget.Machine`), cracks it and stops, or breaks it and carries the surplus on as through a wall. The
   lifepod (no strength) is never struck.
3. **What a hit breaks goes to the hitter.** A station broken by a tool or a shot spills what it held, drops its queue's
   refund and gives its cost to whoever broke it (`Unbuild`, shared with bursts); a burst drops its cost there for anyone.
4. **The numbers.** A pickaxe takes 7 off a station or a scrap wall aboard, 58 blows; 75 in a vessel, six. The axe 4 and
   40; a nail 1 and 10; the railgun 14 and 140, three shots to a station in a vessel.
5. **The client shows it.** Every struck station, door, hatch and built wall sparks as metal and rings as one; a door's or
   hatch's crack is drawn over all its cells and its break throws dust from each; the breaking blow flashes whole, its
   share judged by the sim's own `Breaks`.

No save step: `ShotTarget.Machine` is an added value, and a saved shot never passed one.

## Accepted findings

- A burst passes a pile almost free (its few points read as tenths): piles are cleared in the first minutes, charges come from
  planets.

## Deliberately not handled

- Back walls keep their own crack path; only a meteor's burst breaks them (0107).
- The scanner reads piles only; a readout over a door or a station waits for a player asking how hurt it is.
- A door's crack is a built wall's, not a picture of its own.
