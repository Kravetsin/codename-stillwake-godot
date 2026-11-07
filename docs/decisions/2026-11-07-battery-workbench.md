# The workbench makes batteries

- Status: accepted; adds to [0053](0053-charge-belongs-to-the-item.md) and [0054](0054-asteroids-and-the-drill.md)
- Date: 2026-11-07

## Context

At a playtest a player built the drill long before any battery: the drill is a workbench craft, the battery an
assembler one, and the assembler needs its blueprint and glass, so a smelter or a crusher first. Batteries do not drop
in debris. The drill did nothing in the hand, and nothing in reach made it work. The user asked for batteries at the
workbench.

## Decision

- The workbench lays the battery out beside the coil it is wound of, in the row of coil and rotor. Same cost and ticks:
  at the bench's fifth of full speed, 600 ticks, half a minute; with no power, as everything there.
- The assembler keeps it, five times as fast, as the Former keeps the bench's parts.
- It is understood as any craft is: its parts obtained and a machine that makes it standing. The circuit it costs drops
  in debris, so the battery unlocks with the drill.

## Deliberately not handled

- The charger still needs power and a frame from the Former: a battery from the bench is made empty, as before, and the
  first charge waits for a charger. Not changed here; a player with a drill and no charger is told so by the flat bolt
  ([no-charge-flash](2026-11-07-no-charge-flash.md)).
