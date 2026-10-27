# The sentry strikes with an arc

- Status: accepted; built, the look and sounds the user's picks
- Date: 2026-10-27
- Supersedes the creature's shot of [shots-are-the-sims](2026-10-26-shots-are-the-sims.md)

## Context

The sentry answered a hero past its pounce with a bolt flying at 30 cells a second. In play it was no threat: seen
leaving, it was stepped out of. The player's word: a discharge of static, an arc from the sentry to the point it shoots
at, with no flight, striking at once, drawn with the electric arcs the game already has (the relic's, 0075).

## Decision

- **An arc, not a shot.** `Shoot` keeps its wind-up (0.6 s, the eye flickering), then strikes at once: an `Arc` from the
  sentry's middle to its aim, cut at its `Reach` (60 cells) on the longer axis and where the line enters the first solid
  cell. Arcs are their own list in the creatures' slice, saved while they linger; shots in flight are guns' alone.
- **The aim is taken `Lock` ticks before the strike** (0.2 s) and held, kept in the sentry's `Mem`: a hero standing or
  walking straight is struck; one that darts off in time is not. The eye holds bright once it is taken. The user's pick
  over aiming at the wind-up's start (too easy to step out of) or at the strike (no way out but a wall).
- **It hurts every hero on its line**, the user's pick: each hero's client judges once, the tick it first sees the arc,
  whether the line crosses its body (`Rams.Crosses`), and sends `Bolted(Arc)`; its suit pays the projectile's damage, once
  an arc a hero (`Paid`). An arc walked into afterwards is past. The client is the word on contact, as for a ram (0103).
- **It lingers `Lingers` ticks** (0.5 s) only to take that word across the network; a later word is refused
  `creatures.gone`.
- **Drawn as a chain of arcs** (`Bolts`, hairlines with an afterimage, [arcs-are-hairlines](2026-10-27-arcs-are-hairlines.md)),
  each link at most 40 px of the world: one long arc's folds stray by a share of its length and would hide where it struck. A flash and cyan sparks where it struck, a smaller flash where it left.
- **Sounds, the user's picks:** its charge a laser charging up, through its wind-up; the strike the resonator's crackle,
  lower; where it strikes and through the hit hero's suit, a stun gun's tenth of a second. On the Suit bus: it is in space.

## Deliberately not handled

- **Wrecks do not stop it**, as they did not stop the bolt: only solid cells do. A hero cannot hide behind a drifting
  wreck. Revisit if play asks for cover in space.
- **A word later than 0.5 s is lost**: a hero on a link that slow is not hurt.
- **Saves from before** may hold a bolt in flight among the shots: it flies out harmlessly, as no `Bolted` names a shot.
