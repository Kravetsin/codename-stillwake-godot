# A blade leaves its own trail, a haze

- Status: accepted; supersedes in part [tool-reach](2026-11-05-tool-reach.md): a swing shows no fan
- Date: 2026-11-10

## Context

Swinging, digging and breaking is what a player does most, so a swing has to please the eye. Its cut of the air was a fan
of dust drawn by the stroke's own clock, to the weapon's range. Six looks of that fan were tried and none kept: the fan
is drawn apart from the blade, at an angle the blade is not at, and ahead of it. Wider swings, a rebound of the arm by
material and particles shed off the blade were tried too and dropped.

## Decision

- **The blade draws its trail.** Every frame the two ends of a swung tool's edge are taken where they are drawn, in the
  world (`BladeTrail`); the stretch between two frames is the trail. So it is always at the blade's angle, never ahead of
  it, and it stays where it was swept while the hero moves on.
- **Whatever moves the blade.** By the edge's speed: nothing under 60 px a second, whole from 280. A swing leaves a
  whole trail, a run a trace, a still hand none. Gone in 170 ms.
- **It shows no reach.** A swing's fan of dust is gone, and so are a thrust's dust and a running drill's cone: a haze
  thrown on from a point or a bit that does not sweep read as spat out (the user, in play). A running grinder bends the air
  round its wheel. Reach is a rule of play, longer than the model ([tool-size](2026-11-10-tool-size.md)).
- **The base look is a haze** («Марево», the user's pick, in play, over a torn haze and a torn stroke): the world behind
  the trail bent as through hot air, a breath paler, no line and no colour of its own. Form only.
- **Where it is heading.** The trail is the form a weapon's effects are laid over: a module (the arc first) or a skin
  gives it matter and light, and draws no arc of its own. Nothing is built for that yet.

## Cost

Twenty seconds of chopping, vsync off: 2.08 ms a frame without the trail, 2.37 with it, the GPU 0.03 ms more; one run each.

## Not handled

- Another player's hero leaves a trail too, from its figure as drawn here; nothing is sent.
- The haze is drawn at the shimmers' layer: what is drawn over the world there is not bent.
- An aimed tool, the spear among them, leaves no trail: its edge moves along itself, and a line of haze reads as nothing.
