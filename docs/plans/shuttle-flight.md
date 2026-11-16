# Plan: The crew shuttle's flight — pictures and sounds

Status: slices 1 (seats) and 2 (the trip) are built and shipped, 2026-11-16, the trip's fuel checked and burnt from the
shuttle's store since fuel-liquid; what stays true of them is in the
[shuttle-flight ADR](../decisions/2026-11-16-shuttle-flight.md), sim/Landing, sim/Places and game/features/landing. Slice 3
is left, after the user's pick. Delete this plan once it ships.

## Words

- *course lamp*: the shuttle's lamp, lit while a course is set. *course line*: the radar's line from the ship to the
  course's contact.
- *placeholder*: the plain look drawn in code now: a thin line in the radar's green (`RadarScreen`). The shuttle vanishes
  and appears; its riders see the pod's flight screen (`Ride`).
- *deck*: the ship's floor; *run*: the launch platform pieces under the shuttle, opened and shut whole.

## Slice 3 — pictures and sounds (`feat/shuttle-pictures`), after the user's pick

Outline only. A pick page (CLAUDE.md "Art and sound picks"): the flame, the drift-in, the lift-off, the course line's
colour, at the game's scale through every state. Once picked, exported through
`tools/anime-art`: the deck slides and the shuttle drops through; it drifts in from the left on the vessel and lifts off to
the left; docking heard as the pod's, sounds borrowed from `PodSounds`. Ship: the pick in its ADR, README, plan deleted.
