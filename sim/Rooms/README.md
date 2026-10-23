# Rooms

Rooms: the spaces air fills, closed by walls and closed doors, found from the grid's structure. Each
has a volume, the outer hull it leaks through, and the openings it vents through to space. Ported
from `packages/sim/src/modules/rooms`; its rules hold here unchanged.

- **Depends on:** `Grid`, through `IGridQueries`.
- **State:** nothing saved. Derived: `RoomOf` per cell (a room's id, `Space` or `Solid`), the rooms in
  id order, the grid's `Version` they were last found at, and the rooms' own `Version` with the ids
  found again at it (`Changed`, null for all).

## Rules

- Air passes empty cells, floors, ladders, machines and open doors; walls and closed doors stop it.
- An open cell is an **opening** when it sits in a wall line: an open door, or a cell with walls or
  closed doors on both its left and right, or both above and below.
- **Space** is a flood from the open cells on the grid's border through open cells that are not
  openings. **On grown ground** (0077: a planet type, 0095) an open cell under the sky (no back wall) or before
  natural ground (a natural back wall) is outside wherever it is, space flooding from it and through it as from the
  border: natural ground holds no air, so caves are never rooms, and a dig is found again round itself rather than
  through a cave network spanning the planet (which cost 100 ms a dig). A back wall changed there is found again as a
  change of structure is. **Rooms** are the components of the open cells left, openings included.
- A room's **id** is its smallest cell. Its **openings** are its cells touching space or the grid's
  edge, or with no back wall; its **hull** is the walls and closed doors next to it with space or the
  edge beyond, or with a wall or closed door beyond them away from it that has (a wall two cells thick, 16 px, 0104). Both in
  cell order.
- Rooms are looked at again, by a derivation system, only on a tick the grid's `Version` moved: then
  the grid's log names the cells whose structure or back wall changed since the rooms were last found
  (`IGridQueries.ChangedSince`, 0051). When the log no longer reaches back that far, every room is
  found again as after a load.
  - Back walls alone: each changed cell joins or leaves its room's openings, no flood.
  - Structure, with back walls or not: only what a change can move is labelled again — the changed
    cells and those beside them, whose solidity or opening may change, and every room within two
    cells of a change, which they may join or split. Space comes in from the grid's edge and from the
    space round them, through open cells that are no opening; the open cells left make the rooms,
    each its smallest cell's. The rooms whose cells stayed have their hull and openings found again
    where space came or went within three cells of them, or a back wall of theirs changed: a hull reaches two cells into a wall. A door, a
    breach or a wall built costs the rooms it touches, not the ship.
  - A change that may close space off — a cell space passed stops passing it — is followed by a walk
    through the space beside it, which stops at the grid's edge or past what is built, where all is
    space that reaches the edge. Space that reaches neither is a pocket, closed off: it becomes a room
    with every room whose openings touch it, labelled again as above.

  The ids named are the rooms gone and new, but for a room found again just as it was (its cells,
  hull and openings), and those at the back walls changed, which may leak differently. Found around a
  change or after a load, the rooms are exactly what `Find` gives over the whole grid: ids,
  volumes, hull and openings in cell order, rooms in id order. Each time they change, `Version` is
  bumped.

## API

- **Queries** (`IRoomsQueries`, what a module depending on rooms receives): `RoomAt(cell)` — a room's id, `Space`, or null for a solid cell or off the grid;
  `Afloat(cell)` — whether something there floats: in `Space` on a kind of surface that floats (the ship's), never on a
  planet, whose gravity holds everywhere (0076; `Items`' stacks and the client's hero read it);
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
  found around a change as over the whole grid. Every room that differs from the
  version before is among the ids `ChangedSince` gives.

## Deliberately not handled

As in the TypeScript module: rooms have no identity across a rebuild; a hole two cells wide floods a
room into space; a wall three cells thick leaks nothing (one of two, a 16 px wall, leaks through its inner layer, 0104); a notch in the hull is a tiny room open to
space.

A room a change touches is flooded whole: a door in a great hall costs the hall, and a pocket closed
off costs the pocket. The walk that tells a pocket goes through the space round the change until it
leaves what is built: a wall built in a courtyard of the ship, open to space only by a long way round,
walks the courtyard. A load floods within the built box, the ship: every cell outside it is empty, so
space, reached straight away from the box; a cell on the box's side sees outside beyond and nothing
solid, as one on the grid's edge does, so no ring around the box is needed. The grid logs every write
to its layers (0051), so no change goes unseen.
