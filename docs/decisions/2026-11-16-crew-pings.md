# Pings: a tap of F marks the spot

- Status: accepted
- Date: 2026-11-16

## Context

A pointing gesture (gestures-for-words) is seen only by a friend who has the hero on screen, and only for about a second. A
friend looking elsewhere, or out of view, never learns where "there" was.

## Decision

- **A tap of F pings** the spot under the pointer, as well as pointing at it. The ping is placed even while the hands are
  busy (a ladder, a drill, the plan out), when no point can play.
- **One ping a player** (`Ping`): the next replaces it, and it goes after `Ping.LastsMs` (8 s), fading in over 150 ms and
  out over the last 1.2 s. With at most one dot a player there is nothing to spam, so there is no cooldown.
- **Sent with the hero** (`Seen.Ping`, a `PingSeen` of the spot and a count), in every packet while it lasts, as the rod's
  point is. A friend shows it once per new count, on its own clock. Nothing enters the sim.
- **Drawn as picked** (`PingView`, "ripple", 2026-11-16): a dot of radius 2 px in the player's colour with a dark rim, and once a
  second a thin ring of that colour spreading from 2.6 to 8 px and fading. It is unlit, so the scene's light never dims it.
- **Marked from the edge** off screen: a `MarkerIcon.Ping` badge, a dot in a ring in the player's colour, for this player's
  ping and each friend's.
- **Home's marker is a house** drawn in code (a roof, walls and a door), in place of the pod's silhouette, which did not
  read at badge size.

## Considered

- **A cooldown of about 10 s**: it blocks a fix right after a misplaced ping, while one ping a player already bounds the dots.
- **A soft additive glow** and a 7 px blinking dot: the glow vanished on bright ground and in shadow; the dot was too
  plain. Seven effects were drawn for a pick.

## Not handled

- Pings are not saved or replayed; a friend who joins after one was placed sees it from its start for its full time.
- No sound: most play is in vacuum, and the ring says enough.
