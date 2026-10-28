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

- **A shape it flows toward.** `Cape.Flow(i, swing)` is the cut swung back and up about one point just behind its back
  pin (`Nape`), by the swing times how far down to the power `Curl` (0.7), so it curves. Swung about each column's own pin
  the sheet folded past 0.8 rad; about a point between the pins its back corner at the top rose over them; with the swing
  growing straight down the back edge, inside the curl, squeezed. Each point is pulled toward its place in it (`Pull`).
- **The swing is set by how the hero moves, not where the air goes:** `Calm` (0.1) standing, up to `Sweep` (1.0) at a
  sprint, whichever way the hero goes, and `Soar` (0.7) more falling, capped at `Most` (1.6). It follows as a spring
  (`Spring`, `Damp`), so it overshoots a little and settles.
- **A breeze always runs down it** (`Ripple`, `RippleRun`, `WaveSteps`, `RunWaveSteps`), across the sheet, slow and small
  standing, quicker and wider on a run.
- **The air is gone:** no push or lift; gravity is a light 0.06, which the shape holds up.
- **It goes mostly with the hero across** (`Follow`, 0.7), so the shape, not world inertia, says how fast it goes and a
  backward run cannot press it into the body; up and down it lags in full, so a jump still tosses the hood and a fall throws
  it up.
- **A fold is drawn as cloth, not as lining.** Thrown up past the shoulders, its top rows swing over the line of its pins:
  in a flat picture a cape rising over the shoulders must fold there. It was drawn with its lining, on top of the collar
  and with the hood hidden (`TurnedUp`), which read as the cape turned inside out. `CapeView` now draws a folded part with
  its outside, under the rest; `TurnedUp` is gone. Keeping each row beyond the one above instead (tried) held the cape
  under the shoulder line: on a jump it stuck out back and down like a board.
- **Its far side wraps the body only as it hangs:** the inside's front edge, carried forward round the body, goes as the
  sheet leaves hanging straight down (`Tuck`), so swung away no strip of lining shows past its lower edge.
- **Kept:** no point more than `Give` forward of its cut; the floor under the feet; turning about; afloat, now the same
  shape at `Spread` on a slow wave.
- The `--cape` demo walks and runs backward facing on; with no pointer, `HeroView` lets a script's `Face` stand.

## Deliberately not handled

- **No real air:** the cape streams the same in a calm room, in fog or on a planet's wind.
- **A fold's picture is mirrored:** drawn with the outside, its stripes run the other way; at a few pixels it reads as cloth.
- **The green along a swung cape's lower edge** is the outside picture's front-edge trim, as hanging, not its lining.
- **No collisions:** walls, steps, the legs and held items pass through it.

## Consequences

- Tests in `game.tests/CapeTests.cs`: a run lifts it far higher than a walk; going backward it streams behind as going
  forward, never forward of its cut; a fall throws it up; standing it stirs without coming to rest; its inside wraps the
  body hanging but not swung away on a run. Each fails with the cape as it was.
