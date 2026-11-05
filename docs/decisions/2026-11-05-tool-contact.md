# A running tool is seen at work all along

- Status: accepted; the stream's and jet's placeholder look superseded by [damage-looks](2026-11-05-damage-looks.md)
- Date: 2026-11-05

## Context

A drill, grinder or plasma cutter hits every few ticks. Each hit threw its share of the axe's look where it landed, so a running
grinder showed plainly when a hit went in: informative, but it looked like a hammer tapping, not a wheel grinding. The user wanted
the tool to look as if it worked without a break, sparks and all. The cutter also showed nothing, its jet had no picture, and the
grinder's and cutter's reach, 2.5 and 3 cells, made the player fly into a wreck to use them.

## Decision

- **Seen where it touches, every frame** (`ToolWork`): while an electric tool runs, the client finds where its fan works
  (`Aiming.ContactOf`): along the aim from the hand, the nearest entry into what the fan strikes, a wreck's box or a struck cell,
  else the tool's whole reach. There it throws what it touches looks hit, `WorkLooks.ThrowsPerSecond` throws a second, as much
  in all as its hits threw one by one; the cutter's jet runs from its nozzle to that point. A friend's too, from its sent aim.
- **Hits only crack and sound.** A running tool's hit on a wreck no longer jolts, flares or throws; on the ground it throws only
  when it breaks the wall or fells the growth. Cracks, chops and breaks stay as they were.
- **The shared bits are the contact points**, not the muzzles: where a wreck a running tool bites is struck.
- **Reach:** the grinder 6 cells, a little short of the drill's 6.5; the cutter 8, the farthest hand tool.

## Consequences

- The moment a hit lands is no longer seen on a running tool, only in its cracks deepening and its sound.
- The look of the stream and of the cutter's jet is a placeholder, the hit looks thrown steadily, till the user picks one.
- Deliberately not handled: a tool touching only a creature shows no stream there; the creature's own flinch is its feedback.
