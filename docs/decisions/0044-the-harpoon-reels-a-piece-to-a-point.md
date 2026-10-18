# 0044 — The harpoon reels a piece to a point

- Status: accepted; its recipe superseded by [0046](0046-recipes-show-what-a-thing-is-made-of.md); what others see
  of a shot, by [0065](0065-friends-see-the-harpoon.md)
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

### The gun in the hand

- **The item:** `harpoon`, a stack of one, made at the Fabricator of 2 plates, 4 wire and 4 scrap in 80
  ticks, opened by analysis as every craft is (0027); a tool, its recipe amber as the axe's. Its card tiles
  its range, reload and reel speed (RANGE 12 · RELOAD 5s · REEL 5/s) where the axe's tiles damage.
- **The spring gun,** held as a pistol (`HeldLook.Harpoon`: hold 0, gripped by its plank stock, its
  edge the rod seated in the tube, ending at the muzzle). Out in space and aboard alike the straight arm
  turns so the barrel's line, not the arm's, runs through the pointer, and the hero keeps facing it.
  Its picture shows the rod in the barrel only while the gun is ready.
- **Presses:** a fresh left click fires, aboard as out in space (the grapple, below). In flight, pulled,
  on the way back and in the cooldown a click does nothing. Hooked, a click always reels, even
  on a small piece in reach; a held button never reels. Reeling, a click reels again from where the hero
  is now (the last reel wins).
- **The point reeled to** is `Harpoon.Stop` before the muzzle on the way to the piece's middle as drawn.
  **It follows the hero:** while its reel is on, the client sends `Reel` again whenever that point is more
  than `Harpoon.FollowBeyond` (1 cell) from the last sent, at most every `Harpoon.FollowTicks` (0.5 s);
  the last reel wins, so the piece comes to where the hero is, not where it pressed.
- **A miss** flies out to the full range and snaps back at `Harpoon.ReturnSpeed`, 80 cells a second,
  twice the shot's; so does a line come free. No sound of the drum on a miss.
- **The line comes free** when the piece is gone or the sim would refuse its reel (the hero gone aboard,
  say), or when it drifts past the range unreeled; put away or passed out, the rod is back in the gun at once.
  Reeling, it rides the piece in till the sim's reel is over (or a refused `Reel` leaves none), then
  snaps back. **The piece is caught with E** where it stops: no catch on arrival.
- **The cooldown is the hero's,** 5 s from the shot, hit or miss, one clock for every harpoon it holds.
- **Heard, through the suit** (0042), by the shooter only: the shot; the rod biting at its contact frame
  (metal, a wreck or salvage, the axe's chop; a crate or a pod, the glove's thump); the drum, looped from
  the press that reels till the piece is in; and **a ready cue when the cooldown ends,** tied to the gun
  being ready rather than to the rod coming back, played only with a harpoon in hand. No sound of the line
  going taut.
- **Others** (superseded by 0065) see the gun hang in a friend's hand by `Seen.Wields`, and a taut line from its muzzle to any
  piece the sim says it reels (`Reel.By`). Its shots, flights and hooks are not sent: nothing on the wire
  changes.

### The grapple

- **The rod grips the ship's structure:** a wall, a floor or a closed door, as this peer shows them. Not a
  back wall, behind the hero's plane, nor an open door or a ladder, which stay passable. Each frame the
  rod's path is walked cell by cell as it is tested against the pieces, and **the nearest hit along it
  wins,** piece or structure (of two pieces too, where it was the first in spawn order). It sinks 0.15 of a
  cell in.
- **Hooked in the structure, the next click pulls the hero:** its body's middle goes straight toward the
  point `Harpoon.PullStop` (half a cell) short of the hook, at `Harpoon.PullSpeed` (10 cells a second),
  keys and gravity aside; walls stop it as anything does. Where it gets there, or a wall or the tether
  stops it short, it **hangs**, still, aboard as in space, till the player jumps, holds a movement key or
  clicks again; then the line frees and its own physics take over, a hanging hero dropping. A press during
  the pull does nothing; a held button never pulls.
- **The tether holds:** in space the pull is cut back at the tether's length as a swim is, and the hero
  hangs at the limit.
- **Client only:** the hero is client-authoritative (0002); the pull is its controller's
  (`HeroInput.Pull`), replicated by `SentHero` as any movement. No command, nothing in the sim.
- **The same gun:** one cooldown for the hero, the same ready cue, the miss snapping back. The bite on
  structure is the axe's chop; the drum plays while the hero is pulled. The line frees when its cell stops
  gripping (a door opened, a wall taken down) or the hook is past the range unpulled.
- **Ladders keep their place:** the cooldown and the look of being hauled up are cost enough; no other rule.

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
- (Superseded by 0065.) Others see a friend's line only while the sim reels: not its shot, its rod in flight, nor the piece it
  hooked; its gun hangs, unaimed, empty while it reels. Later, with a kind of work of its own.
- A wreck reeled to `Stop` by its middle overlaps the gun, being three cells long: within the axe's reach.
- A movement key held as the pull arrives lets go at once: the hero hangs only with the keys let go.
- A jump that lets go is not a jump: the hanging hero has nothing under its feet, so it drops.
- Hanging, the figure is in its falling pose, the gun aimed at the pointer again; friends see it hang
  still in the air, its line to the hook taut (0065).
- The pull goes up through a floor from below, as a jump does.
