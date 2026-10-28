# The cape flows

- Status: accepted; built
- Date: 2026-10-28
- Builds on [2026-10-27-the-cape-is-worn](2026-10-27-the-cape-is-worn.md) and
  [2026-10-27-the-cape-floats](2026-10-27-the-cape-floats.md)

## Context

The cape's cloth (`game/features/crew/logic/Cape.cs`) hung by gravity and blew in the air the hero ran through. Walking
backward the air pressed it against a limit through the body and it stood as a plate across the hero. A realistic fix
(the cloth kept off the body's silhouette, PR #247, closed) only made it crumple against the legs. Real cloth tangles. The
user wants the cape to read as a hero's: flowing behind, never tangling.

## Decision

- **A shape it flows toward.** `Cape.Flow(i, swing)` is the cut swung back and up about one point between the pins
  (`Nape`), the more toward the hem, so it curves. Swung about each column's own pin, the sheet folded past 0.8 rad; about
  one point it stays whole to 1.7. Each point is pulled toward its place in it (`Pull`).
- **The swing is set by how the hero moves, not where the air goes:** `Calm` (0.1) standing, up to `Sweep` (1.0) at a
  sprint, whichever way the hero goes, and `Soar` (0.7) more falling, capped at `Most` (1.6). It follows as a spring
  (`Spring`, `Damp`), so it overshoots a little and settles.
- **A breeze always runs down it** (`Ripple`, `RippleRun`, `WaveSteps`, `RunWaveSteps`), across the sheet, slow and small
  standing, quicker and wider on a run.
- **The air is gone:** no push or lift; gravity is a light 0.06, which the shape holds up.
- **It goes mostly with the hero across** (`Follow`, 0.7), so the shape, not world inertia, says how fast it goes and a
  backward run cannot press it into the body; up and down it lags in full, so a jump still tosses the hood and a fall throws
  it up.
- **Kept:** no point more than `Give` forward of its cut; the floor under the feet; turning about; afloat, now the same
  shape at `Spread` on a slow wave.
- The `--cape` demo walks and runs backward facing on; with no pointer, `HeroView` lets a script's `Face` stand.

## Deliberately not handled

- **No real air:** the cape streams the same in a calm room, in fog or on a planet's wind.
- **A fold over the shoulder** may turn a thin strip of the top three rows over at the start of a sprint or a fall, showing
  the lining under the collar and the hood. Below them it never folds walking or running, either way.
- **No collisions:** walls, steps, the legs and held items pass through it.

## Consequences

- Tests in `game.tests/CapeTests.cs`: a run lifts it far higher than a walk; going backward it streams behind as going
  forward, never forward of its cut nor folded below the hood; standing it stirs without coming to rest. Each fails with the
  cape as it was.
