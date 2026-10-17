# 0044 — The harpoon reels a piece to a point

- Status: accepted
- Date: 2026-10-17

## Context

A hero on the 12-cell tether catches or chops only what drifts within reach: most of the stream
passes too far, and a cable rack four cells beyond reach is simply lost. A hand harpoon shoots a rod
into a piece and reels it in, close enough for the hands or the axe. Pieces move in the sim, so a
reeled piece must move there too, the same on every peer; the shot itself lives only on screen.

## Decision

- **The sim moves the piece toward the client's point.** One command, `Reel(Piece, ToX, ToY)`, in
  thousandths of a cell as pieces are: the point the client puts `Harpoon.Stop` (1.5 cells) before the
  gun, inside the hands' reach. The sim does not know where the gun is, nor need it: the point is the
  client's word, among friends (principle 6), and one number pair is all a reel needs to replay alike on
  every peer. The sim keeps the point on the grid, so a piece never leaves it.
- **A straight pull at a whole speed, landing exactly.** `Apply` stores `Reel { By, ToX, ToY, Left }`
  on the piece, saved and hashed, with `Left` the ticks its longer axis takes at `Harpoon.ReelSpeed`
  (250 a tick, 5 cells a second), rounded up, at least one. Each tick a reeled piece moves its share of
  the way still to go, `(To − X) / Left`, truncated toward zero, instead of drifting; the last tick
  covers the rest, so it lands on the point, and the reel clears there. It drifts on from where it is.
- **The last reel wins.** A later `Reel` of the same piece, by anyone, replaces the reel; two on one
  tick are taken in the host's order. No `debris.reeling` refusal: a refusal would need a rule for whose
  line holds, and a hero pressing again to correct a stale point is what the player means. Refusals are
  those of a grab: `debris.gone`, `debris.notOutside` (the sender's hero or the piece's middle not in
  space).
- **Grab and Hit see a reeled piece as any other.** One caught or broken mid-reel is simply gone, its
  reel with it.
- **The shot stays on the client:** the rod's flight (`Harpoon.RodSpeed`, 40 cells a second, out to
  `Harpoon.Range`, 12 cells), the hit test against the pieces, the hooked rod before the reel, the line
  and the cooldown (`Harpoon.CooldownTicks`, 5 s). **The cooldown is the hero's:** every harpoon it
  holds waits for it, so swapping guns does not dodge it. The sim checks none of these, nor that a
  harpoon is held: as the axe (0034).
- **Format 7.** Its step gives every piece of an older save no reel.

## Consequences

- A hook not yet reeled is never saved: a save or a join while the rod is stuck in a piece drops it.
- A diagonal reel is up to 41% faster than a straight one: the longer axis sets the time, with no
  square root in the sim. Accepted.
- A reel keeps going after the reeler leaves, goes aboard or passes out: the sim knows no line.
- Reeled again and again, a wreck can be kept near the ship: skill, not a bug.
- A piece reeled over the hull cannot be grabbed (`debris.notOutside`); it drifts back out.
- A piece reeled left of its path's end is gone on that tick, as a drifting one would be. A hero on the
  tether never gets that far left of the hull.
- A friend's hooked rod is drawn about 0.15 cells late, from 20 Hz samples, once others see shots.
