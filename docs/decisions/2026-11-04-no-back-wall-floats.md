# The back wall holds the ship's gravity: before none, things float

- Status: accepted
- Date: 2026-11-04
- Changes [0007](0007-spacewalks-and-airlocks.md)'s zero gravity in space: it also holds in a room's cells with no back wall

## Context

Gravity aboard came from being in a room: walls all round and no back walls behind still held the hero to the floor, though such
a room shows the stars through it and holds no air. The user asked for gravity to go there as well.

## Decision

- **A cell floats** (`IRoomsQueries.Afloat`) in space, as before, and in a room where the cell has no back wall, on a surface whose
  kind floats (the ship's). Per cell, not per room: as a player lays back walls in a walled room, gravity comes in cell by cell where
  the stars stop showing; a back wall broken lets it go there. A planet is unchanged: gravity everywhere.
- **Still the ship:** a room's cell is the hull for the tether, so a hero floats untied there, and a stack floating there drifts
  nowhere: only a stack out in space with no back wall drifts back past the moving ship.
- Everything that asks `Afloat` follows: the hero swims, stacks and bodies float, shots fly straight, sprays scatter as in space.

## Deliberately not handled

- A room whose cells are partly backed has gravity in patches; walking across one missing back wall at chest height lets the hero
  float a step. It is what the back walls show, and the fix is the back wall.
- Air is untouched: a room with no back walls already leaks through its openings.
