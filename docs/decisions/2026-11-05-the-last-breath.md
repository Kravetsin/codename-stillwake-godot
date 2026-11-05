# The last breath

- Status: accepted
- Date: 2026-11-05
- Amends [0061](0061-death-leaves-a-body.md): a suit at zero no longer passes the hero out at once, and
  [0081](0081-the-wrist-is-one-column-of-gauges-and-keys.md): a low suit fogs the visor instead of turning the screen's edges red

## Context

A hero died on the tick its air reached zero. The gauge's last cell was a cliff: a player watching it run down
had no moment to see death coming, and a dash back to the hatch that fell a step short felt unfair. Air is also
the hero's health: lava, the cold past the field, a resonator's shock and a creature's blow all take it.

## Decision

- **Ten seconds on no air.** Air run dry starts `Hero.Gasp` at `Crew.LastBreath` (200 ticks), one off each tick.
  At nothing the hero passes out, which is death as 0061 has it. It is a counter beside the air, not hidden air:
  the gauge reads zero and the screen darkens.
- **Any air back ends it:** a canister, the pod's air, room air, or a breath the regen suit makes. The next dry
  suit starts a whole one again.
- **Time alone runs it down.** Running and the jetpack cost it nothing, so a dash for air is a dash. The cold costs
  it nothing either: it is what emptied the air, and the edge still gives ten seconds to turn back.
- **Hits cut it.** Lava, a shock or a blow past what the air held takes `Crew.LastBreathPerUnit` ticks a unit, the
  breath a unit of air is worth. Lava burns 15 a breath, so standing on it ends a last breath within a breath; a
  blow far past the air passes the hero out at once.
- **On the screen, the tunnel**, picked from five candidates (ink closing in, colour draining to grey wash, a heartbeat
  vignette, ink blooming over wet paper, the tunnel), then without the tunnel's hard circle, which was rejected in the
  game: `TunnelView`, over the HUD and under the bars, reads the screen and blurs all of it, more the farther from the
  hero's chest with no line between, the world swimming, swaying about the hero and doubled, browning as the last
  breath runs, sinking into ink in its last 15%; ink while passed out. Air back clears it in 0.7 s, a hit's cut
  catches up in 0.4 s. It replaces the plain dark of passing out.
- **A low suit fogs the visor**, picked from five candidates (visor fog, the helmet's rim, quick breathing at the edges,
  the helmet's hologram, colour draining from the edges) once the red edges were rejected in the game: red read as wounds
  while the hero still had ten seconds and more. `VisorFogView`, under the HUD: grey condensation in from the corners with
  each breath out, the world blurred under it, beads in the corners; deeper and the breath quicker as the air goes, the
  gasps flattening at no air; out of danger it evaporates in patches over 2.4 s, the beads last. The gauge stays sky blue,
  its last lamp dimming with each breath out; the beep stays. Its pale flash on letting go is gone with the red.

## Deliberately not handled

- The fog gathers about the screen's middle, not the hero: it is on the visor, and the camera keeps the hero near the middle.
- The fog's gasps run on the client's own clock from the air reaching zero; a hit's cut of the last breath does not hurry
  them. The tunnel shows the cut.
- The fog and the frost both grow from the edges; the fog is grey and breathes, the frost white, veined and still.
- A thrust at zero air is free, as it was before: the last breath counts only time.
- Other players see nothing new: no sign of a friend's last breath beyond the body falling; the bracelet could show it
  later.
- Panned away, the blur is least at the screen's edge nearest the hero, not on the hero.
- No new sound: the darkening speaks alone for now.

## Consequences

- Every death by air comes `Crew.LastBreath` ticks later than before; tests that ran a suit dry now run the last
  breath too.
- A hero at zero air in a room giving exactly one unit a breath lives on, as it did.
