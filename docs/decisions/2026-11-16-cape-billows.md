# The cape is painted raw and billows slowly at a run

- Status: accepted; supersedes [the-cape-flows](2026-10-28-the-cape-flows.md)'s sweep at a sprint, its breeze and its wrap
- Date: 2026-11-16

## Context

The cape was drawn in code and its sheet posed by [the-cape-flows](2026-10-28-the-cape-flows.md). The user wanted it
redrawn with the raw GPT paint the body skins use, and running, the hero's most common action, still looked wrong: at a
sprint the cape stood out nearly level, its hem curled up, and it was held in one pose. A first fix flapped it with each
stride; at about eight ripples a second it jittered.

## Decision

- **Painted raw.** The cape's four pictures (`hero_cape`, `_lining`, `_hood`, `_collar`) are the GPT redraw the user
  picked, «the same, in the new style», laid on the same sheet; the code that drew the old one is gone. Three other
  candidates ship as free cape skins: `tarpCape`, `blanketCape`, `woolCape` (Tarp, Thermal blanket, Wool). No brass
  clasp at the throat: it read as a bug.
- **It trails at a slant.** At a sprint it swings `Sweep` (0.45 rad) back, not 1.0: the cut already flares about 37°, so
  more stood it out level. Afloat it still opens to `AfloatSweep` (1.0).
- **It sags.** The swing grows fast below the shoulders (`Curl`, 0.5) and less again toward the hem by `Droop` (0.45), so
  swung back its lower part hangs under its weight instead of curling up. Falling it streams up and does not sag.
- **One slow wave, never the feet.** A wave runs down it from the shoulders to the hem, once every `WaveSteps` (100)
  steps standing, a gentle sway of `Ripple` (0.8 px), quickening to once every `RunWaveSteps` (45) and `RippleRun`
  (1.6 px) at a sprint. Running, the same wave also swings the hem up and back by `Flap` (0.18 rad), `FlapLag` of a wave
  behind the shoulders, its strength drifting by `Gust` over `GustWaves` waves, so a sprint never holds it still.
- **The wrap goes by distance.** The lining's front edge wraps the body while the row's front point is near where it hangs
  still, and is gone `Tuck` (8 px) away from it, so a sagging lower part swung back shows no lining past its edge.
- **`--cape-film=<png>`** films the hero standing, running and stopping, every third step on one sheet (`game/dev/CapeFilm.cs`).

## Rejected

- A fur collar redrawn to match the new paint, the whole cape painted as it hangs and mapped onto the sheet, and the cape
  as three rigid pieces: the collars read as foreign objects, the whole painting stretched, the pieces could not bend.
- A ripple and a flap with each footfall: the jitter this ADR removes.
- No sway at all standing: the user asked it back.

## Deliberately not handled

- **The stretch over the shoulders** as the sheet swings stays; a texture that hides it is an idea, not built.
- Still no real air, inertia or collisions, as [the-cape-flows](2026-10-28-the-cape-flows.md) says.

## Consequences

- `game.tests/CapeTests.cs`: a run sweeps its hem far farther than a walk; standing it sways gently and walking more; at
  a sprint its hem rises and falls slowly, turning at most six times in 90 steps (22 with a wave a footfall); its lining
  wraps the body hanging, not swung away. Each was proven to bite by breaking its rule once.
