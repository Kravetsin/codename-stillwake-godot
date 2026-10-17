# Rooms

Rooms: the spaces air fills, closed by walls and closed doors, found from the grid's structure. Each
has a volume, the outer hull it leaks through, and the openings it vents through to space. Ported
from `packages/sim/src/modules/rooms`; its rules hold here unchanged.

- **Depends on:** `Grid`, through `IGridQueries`.
- **State:** nothing saved. Derived: `RoomOf` per cell (a room's id, `Space` or `Solid`), the rooms in
  id order, the copies of the structure and back layers they were found in, the grid's `Version` and
  built box last looked at (`Looked`), the box last flooded (`Flooded`), and the rooms' own `Version`
  with the ids found again at it (`Changed`, null for all).

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
  the structure and back layers are compared with the copies, row by row within the grid's built box
  (`IGridQueries.Built`) now and at the last look, outside which both are empty, for the cells changed.
  - Back walls alone: each changed cell joins or leaves its room's openings, no flood.
  - Structure alone, no changed cell within two cells of space or the grid's edge: only the rooms
    beside the changed cells are flooded again. Space cannot move there: a change moves openings at
    most a cell away, and space floods in only from a cell beside one.
  - Otherwise, or structure and back walls together: every room is flooded again within the built
    box, the ship. Every cell outside it is empty, so space, reached straight away from the box; so a
    cell on the box's side sees outside beyond and nothing solid, as one on the grid's edge does, and
    no ring around the box is needed. The ids named are the rooms whose cells, hull or openings
    differ, gone and new, and those at the back walls changed with it, which may leak differently.

  Found around a change or within the ship, the rooms are exactly what `Find` gives over the whole
  grid: ids, volumes, hull and openings in cell order, rooms in id order. Each time they change,
  `Version` is bumped. A load floods the built box too.

## API

- **Queries** (`IRoomsQueries`, what a module depending on rooms receives): `RoomAt(cell)` — a room's id, `Space`, or null for a solid cell or off the grid;
  `RoomById(id)`; `All`, in id order, a new list whenever the rooms or their openings are found again;
  `RoomOfCells` — every cell at once, live and read-only: a room's id, `Space` or `Solid`, for a
  module walking every cell (`atmosphere`); `Version`, bumped each time rooms are found again, never
  saved; `ChangedSince(version)` — the ids found again since then, gone and new, ascending: empty
  while `version` is current, null when every room was found again (a load) or `version` is older
  than the last change. A module keeping something per room (`atmosphere`'s orders, survey and
  leaks) redoes those ids and keeps the rest. `Find(...)` is the flood itself over the whole grid,
  static, for tests and tools.

## Invariants

- Every open cell is in exactly one room or in space; solid cells in none.
- A room is closed: an open neighbour of a room cell is in the room, or is space beside one of its
  openings.
- What the rooms are depends on the grid alone: the same after a load as before it, and the same
  found around a change or within the ship as over the whole grid. Every room that differs from the
  version before is among the ids `ChangedSince` gives.

## Deliberately not handled

As in the TypeScript module: rooms have no identity across a rebuild; a hole two cells wide floods a
room into space; a wall two cells thick leaks nothing; a notch in the hull is a tiny room open to
space.

A change within two cells of space or the grid's edge, a breach, floods the whole built box: it costs
the ship, not the build area, and anything built far out (a back wall or a pipe in space) widens the
box and so the cost. The grid changes only through its commands and operations, which bump its
`Version`; nothing else writes its layers, so no change goes unseen.
