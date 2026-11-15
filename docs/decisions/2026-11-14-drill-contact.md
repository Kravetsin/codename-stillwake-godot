# The drill shows its reach as a turning double helix, and its sparks ride it out of a wreck

- Status: accepted; extends [grinder-contact](2026-11-14-grinder-contact.md) to the drill; supersedes where the drill's sparks
  start in [damage-looks](2026-11-05-damage-looks.md)
- Date: 2026-11-14

## Context

Once the grinder's sparks rode its haze, the user asked for the same on the other tools. The drill threw its sparks from one
point, where its ray first met a wreck, fanned back toward the tool, with a white bead there. Its zone, a cone to its reach,
was never drawn.

A first pick page gave «Витки марева»: an open spiral from the bit's point, sparks riding it back out of the bore. Built, it
failed on two counts. Pushed close, the bore had no mouth past the bit, and the sparks went back to a point; walked from the
shoulder instead, they rode the spiral up the arm. The user: «искры по спирали до самого локтя идут… у болгарки прям идеально
сделано, что искры летят только по мареву». A second page asked for a haze that reads as a drill; the user picked
«Двойная спираль».

## Decision

- **The haze** (`ToolContact.Helix`): two strands of pale air (`AirInk`) round the drill's axis from its chuck out to the fan's
  reach, opposite each other, `HelixRadius` at most, grown over the first `HelixGrow` of the length and narrowing toward the
  reach as a bit does, a turn every `HelixPitch` px, turning `HelixTurns` times a second. Drawn in three dry-brush strokes
  (`HelixStrokes`), the strand behind the axis paler. It eases in and out with the zone and shows whenever the drill runs.
- **One loop:** the strands meet at the chuck and at the reach, so the helix is a closed loop, out along one strand and back
  along the other, a place on it a share from 0 to 1. The drill's sparks then follow the grinder's rule: the loop walked in
  `HelixSteps` against each touched wreck's drawn pixels, each stretch inside ends at an exit (at most `WheelExits`, the longest
  first); each spark is born anywhere on the loop, rides it at `HelixRide` px a second along the axis to the next exit, and
  flies on along the loop there, spread `HelixSpread`. The loop's turn is the drawn haze's, so the sparks ride what is seen.
- **Always off the haze**, for the grinder too: a loop with no exit (all in a wreck, or missing what an outer ray touched)
  throws from its point nearest the touch (`ToolContact.Nearest`), its sparks riding the loop to it. This supersedes the no-exit
  fallback of grinder-contact. The haze runs from the chuck, never the arm, so no spark rides the arm.
- **No bead** for the drill, as for the grinder. The plasma cutter keeps its own.
- The grinder's wheel and the drill's helix are one module, `ToolContact`: where a running tool's sparks come out of what it cuts.

## Deliberately not handled

- A spark riding the helix follows the helix it was thrown on; a drill moved mid-ride leaves it on the old one for the few
  pixels left.
- The ground's chips are unchanged. The helix passes through walls: it shows reach, not what is hit.
