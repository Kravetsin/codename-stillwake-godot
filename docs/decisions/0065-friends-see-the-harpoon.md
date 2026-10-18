# 0065 — Friends see the harpoon

- Status: accepted
- Supersedes: of [0044](0044-the-harpoon-reels-a-piece-to-a-point.md), "Others see ... a taut line from its
  muzzle to any piece the sim says it reels. Its shots, flights and hooks are not sent", and its consequences
  on a friend's line seen only while the sim reels
- Date: 2026-10-18

## Context

In a co-op playtest a friend's harpoon showed only as a gun in its hand: no rod flying, no line, no hook,
no grapple. The shot lives on the shooter's client (0044); only `Reel` reaches the sim, so peers saw at
most a line to a piece the sim reeled, and nothing while the rod flew, hooked or pulled its hero.

## Decision

- **The rod goes with the hero, not with the sim.** `Seen.Rod` (`RodSeen`): the rod's state while out of
  the gun, and one point that holds through that state. Flying, the end of the range it flies toward;
  stuck in a piece, where it sticks from the piece's middle, with the piece's id; stuck in the structure,
  where it sticks; on its way back, no point. In the gun, nothing: an idle gun is written as before.
  Cosmetic, client-authoritative as the hero is (0002); nothing reaches the sim, no determinism at stake.
- **Sent at once when it changes.** The hero sender sends the frame the rod fires, sticks, is reeled or
  pulled, comes free or seats, not at the next 50 ms; otherwise as ever. Each state's point holds, so a
  rod adds no sends of its own while it flies.
- **Each peer plays it out** (`RemoteRod`), taken from the sample drawn, 150 ms behind with its hero:
  fired, the rod flies from the gun drawn here at `Harpoon.RodSpeed` toward the point sent, never past
  `Harpoon.Range`; stuck, it sits where sent, riding its piece as this peer draws it, so the line ends on
  the piece the sim moves here; free, or its piece gone here, it snaps back at `Harpoon.ReturnSpeed`;
  sent in the gun, it is there at once. The line, the rod and the puff are drawn as the shooter's own;
  the gun aims at the rod while it is out, and is drawn loaded only 5 s after the shot seen here.
- **Heard where it is, on World,** as a friend's steps are: the shot at the gun, the bite where the rod
  sticks (the chop, the glove's thump on a crate or pod), the drum at the gun while it reels or pulls,
  at `Sound.ToolReach` (5 → 20 cells). Not the ready cue: that is the shooter's. While this hero floats,
  World is silent (0042), so a friend's harpoon in space is heard only from aboard.
- **The wire:** about 45–55 bytes of JSON on each hero update while the rod is out
  (`"harpoon":{"rod":"reeling","at":{"x":-500,"y":120},"piece":7}`), none while it is in the gun, and a
  few extra sends a shot (one per change).

## Consequences

- The line to a piece follows the shooter's line, not the sim's reel: a reel the sim keeps running after
  its shooter's line came free (gone aboard, passed out) shows no line, as on the shooter's screen.
- The flight is the shooter's played again from the gun drawn here: where the friend's figure is drawn
  differs from where it shot from by the interpolation's slack, so a rod can stop a little short of or
  past where it sticks, then sit there. Within a frame or two at real time.
- A rod first seen stuck (a dropped sample, a join mid-shot) fires and bites at once, pointing from the gun.
- The rod's way back is this peer's, toward the gun drawn here; a friend sent back in the gun before it gets
  there has its rod vanish where it is.
- An aboard listener hears a friend's harpoon out in space through the hull, as World carries it.
- The drill's and jetpack's sounds and the axe's swing stay silent for others (0042); the drill aims and
  spins, the axe swings and the jetpack blows its plume as before.
