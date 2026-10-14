# Rooms

Rooms: the spaces air fills, closed by walls and closed doors, found from the grid's structure. Each
has a volume, the outer hull it leaks through, and the openings it vents through to space. Ported
from `packages/sim/src/modules/rooms`; its rules hold here unchanged.

- **Depends on:** `Grid`, through `IGridQueries`.
- **State:** nothing saved. Derived: `RoomOf` per cell (a room's id, `Space` or `Solid`), the rooms in
  id order, and the copies of the structure and back layers they were found in.

## Rules

- Air passes empty cells, floors, ladders, machines and open doors; walls and closed doors stop it.
- An open cell is an **opening** when it sits in a wall line: an open door, or a cell with walls or
  closed doors on both its left and right, or both above and below.
- **Space** is a flood from the open cells on the grid's border through open cells that are not
  openings. **Rooms** are the components of the open cells left, openings included.
- A room's **id** is its smallest cell. Its **openings** are its cells touching space or the grid's
  edge, or with no back wall; its **hull** is the walls and closed doors next to it with space or the
  edge beyond. Both in cell order.
- Rooms are found again on the tick the structure changes: a derivation system compares the layers
  with its copies every tick. A change of back walls alone only finds the openings again, no flood.

## API

- **Queries:** `RoomAt(cell)` — a room's id, `Space`, or null for a solid cell or off the grid;
  `RoomById(id)`; `All`, in id order. `Find(...)` is the flood itself, static, for tests and tools.
- **Not yet ported:** `roomOfCells` (every cell at once), added with `atmosphere`.

## Invariants

- Every open cell is in exactly one room or in space; solid cells in none.
- A room is closed: an open neighbour of a room cell is in the room, or is space beside one of its
  openings.
- What the rooms are depends on the grid alone: the same after a load as before it.

## Deliberately not handled

As in the TypeScript module: rooms have no identity across a rebuild; a hole two cells wide floods a
room into space; a wall two cells thick leaks nothing; a notch in the hull is a tiny room open to
space.
