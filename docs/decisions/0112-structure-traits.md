# 0112 — Systems ask a structure's traits, not its kind

- Status: accepted; amends [0111](0111-doors-and-hatches.md)'s open hatch, climbed only over a ladder
- Date: 2026-10-24

## Context

Rules branched on structure codes: about 120 lines in 34 files compared a cell to `Wall`, `Floor`, `DoorClosed` and the
rest. "Solid" alone was written five times and meant three things: items, creatures and the aim stood on walls, floors and
shut doorways; the hero and the cave view stopped at walls and shut doors; light and air at walls and shut doors. A new
structure, even a plain one, touched twelve to fifteen files, and each missed one was a quiet rule of its own.

## Decision

- **One table.** `sim/Content/Structures.cs` holds a `StructureSpec` per code, a flat array indexed by code. The codes
  (`Structure`, moved here from the grid) are unchanged, so saves are too.
- **Traits are flags.** `Blocks` (a body stops at it from every side), `Supports` (stood on from above), `Climbable`,
  `Seals` (airtight), `BlocksLight`, `Terrain` (carries a material), `Opening` (an open doorway, for air) and
  `BlocksFromBelow` (a shut hatch: stood on as a floor, but a hero's body stops in it as in a wall). `Solid` is `Blocks` or
  `Supports`.

  | Kind | Traits |
  | --- | --- |
  | Wall | Blocks, Seals, BlocksLight, Terrain |
  | Floor | Supports |
  | Ladder | Climbable; its top holds a hero not climbing |
  | Door, shut | Blocks, Seals, BlocksLight |
  | Door, open | Opening |
  | Hatch, shut | Supports, BlocksFromBelow, Seals, BlocksLight |
  | Hatch, open | Opening, Climbable |

- **Per-kind data beside them.** `Leak` (a doorway's; a terrain's is its material's), `Toggle` (the code its doorway turns
  into), `Group` (door or hatch: doorways join and open within their group), `Join` (a door's segments join in a column),
  `Build` (the build id that places it), `Char` (its map character), `Draw` (autotile, cut, paired, door shape) and `Tile`
  (its art), `Drag` (a line or an area).
- **The API.** `IGridQueries` answers `Blocks`, `Supports`, `Climbable`, `Seals`, `BlocksLight` and `Solid` per cell; past
  the grid everything blocks. Code-only callers (the hero over toggles in flight, rooms over a layer, the client over what it
  drew) read `Structures.Of(code)` and `Structures.Is(code, trait)`.
- **An open hatch is a ladder on its own.** Climbable, it is climbed and stood on whether or not a ladder lies past it.
- **Only three places name a kind:** `Structures.cs` itself, `Doorways` (what a doorway is and how it joins) and the grid's
  door toggle. `StructureCodeGuardTests` fails on a comparison to a concrete code anywhere else; `Structure.Empty`, the
  absence of a structure, is not a kind.

A new plain structure is a row in `Structures.cs` (its build's cost in `Builds.cs` while builds keep their own table), its
art and its name. A new behaviour is a trait and the one rule that reads it.

## Rejected

- **A class per kind with virtual methods:** the rules belong to the modules that apply them; a kind's class would gather
  rooms, light and movement in one place that none of them owns.
- **Traits computed from the kind in each module:** that is what we had.
- **A `Shut` trait shared by doors and hatches for the hero:** a shut door is a wall to a body; a shut hatch is a floor
  over it. One flag would bring back the guess 0111 removed.

## Accepted limitations

- Machines and materials keep their own tables; their traits (`Hurts`, `Glow`, …) are fields there.
- The analyzer's and the smelter's screens still name their kinds: each is one screen for one machine.
