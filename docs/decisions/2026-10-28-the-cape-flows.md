# The cape flows

- Status: accepted; built
- Date: 2026-10-28
- Builds on [2026-10-27-the-cape-is-worn](2026-10-27-the-cape-is-worn.md); replaces the cloth of
  [2026-10-27-the-cape-floats](2026-10-27-the-cape-floats.md) with a pose

## Context

The cape (`game/features/crew/logic/Cape.cs`) was a Verlet cloth that hung by gravity and blew in the air the hero ran
through. Walking backward the air pressed it against a limit through the body and it stood as a plate across the hero.
Keeping it off the body's silhouette (PR #247, closed) crumpled it against the legs. A shape the cloth was pulled toward
fixed the running, but the cloth left under it still tossed it unpredictably on jumps, folded it over the shoulders and
squashed it under every guard added against that. The user wants the cape to read as a hero's: flowing behind, nothing
unforeseen.

## Decision

- **Posed, not simulated.** Every point is placed each frame, nothing integrates: `Cape.Flow(i, swing)` is the cut swung
  back and up about `Nape`, a point just behind the back pin, by the swing times how far down to the power `Curl` (0.7).
  Swung about each column's own pin the sheet folded past 0.8 rad; about a point between the pins its top back corner
  rose over them; with the swing growing straight down its back edge, inside the curl, was squeezed. As built it never
  folds up to the most it swings.
- **The swing is set by how the hero moves:** `Calm` (0.1) standing, up to `Sweep` (1.0) at a sprint whichever way the
  hero goes, `Sink` (0.5) less rising from a jump so it trails down, `Soar` (0.7) more falling so it streams up, within 0
  and `Most` (1.6). Afloat it opens to `Spread` (0.7) instead of `Calm`.
- **Each row eases toward it as a spring** (`Spring`, `Damp`), the lower the slower (`Lag`, 0.75): it overshoots a little
  on a stop or a landing and a change runs down it as a whip. Turned about, every row is flung back toward the body
  (`Flung`) and swings out afresh.
- **A breeze always runs down it** across the sheet (`Ripple`, `RippleRun`, `WaveSteps`, `RunWaveSteps`, afloat `Wave`):
  slow and small standing, quicker and wider on a run.
- **The hood** takes the swing of the cape's row under it (`HoodOn`), `HoodWind` of it, and the breeze.
- **Its far side wraps the body only as it hangs:** the inside's front edge, carried forward round the body, goes as the
  sheet leaves hanging straight down (`Tuck`), so swung away no lining shows past its lower edge.
- **Kept:** no point more than `Give` forward of its cut; never under the feet but afloat.
- **Gone:** `Cloth` (its only user was the cape), `TurnedUp` and its drawing of the lining over the collar, the folded-part
  drawing in `CapeView`.
- The `--cape` demo walks and runs backward facing on; with no pointer, `HeroView` lets a script's `Face` stand.

## Deliberately not handled

- **No real air and no inertia:** the cape is the same in a calm room, in fog or on a planet's wind, and a stop does not
  bunch its hem on the floor: only the poses' springs move it.
- **No collisions:** walls, steps, the legs and held items pass through it.
- **The green along a swung cape's lower edge** is the outside picture's front-edge trim, as it is hanging.

## Consequences

- Tests in `game.tests/CapeTests.cs`: a run lifts it far higher than a walk; going backward it streams behind as going
  forward, never forward of its cut; rising from a jump it trails below a flat run; a sprint runs down it as a whip; a fall
  throws it up; it never folds through a run, a jump, a fall, turning about and afloat; standing it stirs; its inside wraps
  the body hanging, not swung away. Each was proven to bite by breaking its rule once.
