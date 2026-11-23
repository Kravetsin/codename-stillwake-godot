# Bursts push debris, and hurt farther than they dig

- Status: accepted
- Date: 2026-11-23

## Context

Step 4, the last, of the debris-physics plan ([debris-host-physics](2026-11-23-debris-host-physics.md),
[debris-coop](2026-11-23-debris-coop.md)). With debris on the host player's physics, a burst should throw it. Played first with
the push within a burst's reach, only small pieces moved: a charge broke most wrecks it reached and the rest stayed put, and a
drum in a pile of wrecks broke one. The user asked for a far bigger wave in space, and a bigger burst too, so a drum is a danger
to the player and a bait for breakers.

## Decision

- **Three reaches a blast** (`Content.Blast`): `Reach`, how far it digs (Building's wave through walls and soil, unchanged);
  `Wave`, how far it hurts wrecks, heroes and creatures; `Push`, how far it throws debris. Charge and drum: Wave 12, Push 30;
  missile 4 and 10; shell 3 and 6. Meteors and lightning push `PushPerReach` (3) times their reach.
- **One log of bursts:** `BuildingModule.Burst`, which every burst already goes through, keeps the last `BurstsKept` (32) as
  `{ Serial, Cell, Power, Push }` (`Bursts`), unsaved and unhashed: only the host player's client reads it.
- **The push** (`features/debris/logic/BurstPush`): every body within `Push` of a new burst takes an impulse of
  `power × BurstPush × (1 − gap / Push)`, away from the burst along the line to its box's nearest point and put on there, so a
  light piece flies far, a heavy one drifts, and one struck off its middle turns. `BurstPush` 300, no body faster than
  `BurstFastest` (36 cells a second): a cargo container just past a drum's Wave drifts off at 2.5 cells a second. Guests see it
  through the feed, eased.
- Every burst pushes, one rule: charge, drum, missile, shell, meteor, lightning.

## Deliberately not handled

- **Heroes and creatures are not pushed,** only hurt within the Wave.
- **More than 32 bursts between two reads** push only the last 32.
- **A burst on a planet** pushes nothing: the bodies are the ship's.
