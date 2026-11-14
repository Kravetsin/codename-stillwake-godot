# The drill shows its reach as a turning spiral, and its sparks ride it out of the bore

- Status: accepted; extends [grinder-contact](2026-11-14-grinder-contact.md) to the drill; supersedes where the drill's sparks
  start in [damage-looks](2026-11-05-damage-looks.md)
- Date: 2026-11-14

## Context

Once the grinder's sparks rode its haze, the user asked for the same on the other tools. The drill threw its sparks from one
point, where its ray first met a wreck, fanned back toward the tool, with a white bead there. Its zone, a cone from the bit's
point to its reach, was never drawn. A pick page showed four looks; the user picked «Витки марева»: the reach drawn as a
pale spiral, sparks riding it back out of the wreck.

## Decision

- **The haze:** a spiral of pale air (`AirInk`) from the bit's point out to the fan's reach, in three dry-brush strokes
  (`ScrewStrokes`), `ScrewRadius` either side of the axis, a turn every `ScrewPitch` px, turning `ScrewTurns` times a
  second so its waves run back toward the hand; the half behind the axis paler. It eases in and out with the zone, and
  shows whenever the drill runs, touching or not.
- **The bore** (`ToolContact.BoreOf`): the drill's axis, from the shoulder through the bit to its reach, walked every `ScrewStep` px
  against each touched wreck's drawn pixels. The first stretch inside is the bore: its mouth and its deepest point.
- **Sparks:** a wreck throws at the same rate as before. Each spark is born in the bore past the bit's point (or along the
  bit, while only the bit is in), rides the spiral back to the mouth at its own speed, and flies on along the spiral's way
  there, spread `ScrewSpread`. The spiral's phase is the drawn haze's, so the sparks ride what is seen.
- **No bead** for the drill, as for the grinder. The plasma cutter keeps its own.
- **Always off the haze**, for the grinder too. The user, after the first build, with the drill pushed in: «Предлагаю, чтобы
  искры всегда все равно шли из спирали. И надо перепроверить болгарку». So the bore is walked from the shoulder, and a
  drill pushed in past its bit has its mouth where the axis enters the wreck, or at the shoulder if all of it is in. A wreck
  the axis misses (an outer ray touched it) is bored at no depth where the axis passes nearest the touch
  (`ToolContact.Under`). A grinder with no exit (its rim all in a wreck, or a pebble inside its circle) throws off the rim's
  point nearest the touch (`ToolContact.Nearest`), its sparks riding the whole rim to it. This supersedes the no-exit
  fallback of grinder-contact.
- The grinder's `WheelContact` and the drill's contact are one module, `ToolContact`: where a running tool's sparks come out
  of what it cuts.

## Deliberately not handled

- Only the bore's first stretch throws: a second plate behind the first on the same axis cracks without its own sparks.
- A spark riding the spiral follows the spiral it was thrown on; a drill moved mid-ride leaves it on the old one for the few
  pixels left.
- The ground's chips are unchanged. The spiral passes through walls: it shows reach, not what is hit.
