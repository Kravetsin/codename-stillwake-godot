# Rooms

Rooms: the spaces air fills, closed by walls and closed doors, found from the grid's structure. Each
has a volume, the outer hull it leaks through, and the openings it vents through to space. Ported
from `packages/sim/src/modules/rooms`; its rules hold here unchanged.

- **Depends on:** `Grid`, through `IGridQueries`.
- **State:** nothing saved. Derived: `RoomOf` per cell (a room's id, `Space` or `Solid`), the rooms in
  id order, the copies of the structure and back layers they were found in, the grid's `Version` last
  looked at, and the rooms' own `Version` with the ids found again at it (`Changed`, null for all).

## Rules

- Air passes empty cells, floors, ladders, machines and open doors; walls and closed doors stop it.
- An open cell is an **opening** when it sits in a wall line: an open door, or a cell with walls or
  closed doors on both its left and right, or both above and below.
- **Space** is a flood from the open cells on the grid's border through open cells that are not
  openings. **Rooms** are the components of the open cells left, openings included.
- A room's **id** is its smallest cell. Its **openings** are its cells touching space or the grid's
  edge, or with no back wall; its **hull** is the walls and closed doors next to it with space or the
  edge beyond. Both in cell order.
- Rooms are looked at again, by a derivation system, only on a tick the grid's `Version` moved: then
  the structure and back layers are compared with the copies, block by block, for the cells changed.
  - Back walls alone: only the openings of the rooms holding a changed cell are found again, no flood.
  - Structure alone, no changed cell within two cells of space or the grid's edge: only the rooms
    beside the changed cells are flooded again. Space cannot move there: a change moves openings at
    most a cell away, and space floods in only from a cell beside one.
  - Otherwise, or structure and back walls together: every room is found from scratch.

  Found around a change or from scratch, the rooms are exactly what `Find` gives: ids, volumes, hull
  and openings in cell order, rooms in id order. Each time they change, `Version` is bumped.

## API

- **Queries** (`IRoomsQueries`, what a module depending on rooms receives): `RoomAt(cell)` — a room's id, `Space`, or null for a solid cell or off the grid;
  `RoomById(id)`; `All`, in id order, a new list whenever the rooms or their openings are found again;
  `RoomOfCells` — every cell at once, live and read-only: a room's id, `Space` or `Solid`, for a
  module walking every cell (`atmosphere`); `Version`, bumped each time rooms are found again, never
  saved; `ChangedSince(version)` — the ids found again since then, gone and new, ascending: empty
  while `version` is current, null when every room was found again or `version` is older than the
  last change. A module keeping something per room (`atmosphere`'s orders, survey and leaks) redoes
  those ids and keeps the rest. `Find(...)` is the flood itself, static, for tests and tools.

## Invariants

- Every open cell is in exactly one room or in space; solid cells in none.
- A room is closed: an open neighbour of a room cell is in the room, or is space beside one of its
  openings.
- What the rooms are depends on the grid alone: the same after a load as before it, and the same
  found around a change as from scratch. Every room that differs from the version before is among
  the ids `ChangedSince` gives.

## Deliberately not handled

As in the TypeScript module: rooms have no identity across a rebuild; a hole two cells wide floods a
room into space; a wall two cells thick leaks nothing; a notch in the hull is a tiny room open to
space.

A change within two cells of space or the grid's edge, a breach, still floods the whole grid: it
costs the build area, not the ship. The grid changes only through its commands and operations, which
bump its `Version`; nothing else writes its layers, so no change goes unseen.
