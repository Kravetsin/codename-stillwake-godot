# The drill traced from a picked reference, its bit drawn to turn

- Status: accepted; redraws the drill of [0054](0054-asteroids-and-the-drill.md)
- Date: 2026-11-14

## Context

The drill was still the pixel art's shape inked over: a steel box with heat bands, a cone of a bit. Weapons move one by
one to pictures traced from a picked reference, as the [fire axe](2026-11-14-fire-axe-redrawn.md) did.

## Decision

- **Picked from five generated references, traced.** GPT drew five drills (from a pipe, mining, auger, core, the current
  one redrawn); the user picked «Из трубы»: a motor in a length of pipe in flaking red primer, two bolted clamps, copper
  wire round the nose, a taped pistol grip and a bolted battery. It is traced as the axe: `refs/drill.png` →
  `traced/drill.json`, the primer an overlay on the steel.
- **The bit is drawn, not traced.** A traced picture cannot turn, so the palette mends the bit out of the reference
  before tracing and `zfittings.js` draws it in its place: a shank, a fluted body, a point. Its flutes move a third of a
  groove each of the three spin frames.
- **33 × 16 pixels**, the old length, at three quarters (tool-size), now with the grip and battery under the pipe. The
  grip, the lamp on the rear cap, the coil's mount on the bare pipe, the bit's edge and the clamp under the motor are
  measured from the trace. On the arc bench the battery hangs over the table's edge, as the rule allows.

## Not handled

- The bit's twist is a few slanted grooves, not the reference's fine spiral: at 33 pixels a finer one is noise.
- The grip is tilted in the picture; the hold is the old one's. If the fist reads wrong in play, the hold changes, not
  the picture.
