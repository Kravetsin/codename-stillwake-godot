# 0112 — Systems ask a structure's traits, not its kind

- Status: accepted; amends [0111](0111-doors-and-hatches.md)'s open hatch, climbed only over a ladder; its machine traits as fields and its limitation that a system working one kind names it superseded by [0113](0113-machine-traits.md)
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
  into), `Group` (door or hatch: doorways join and open within their group), `Join` (the way a doorway runs: a door up a
  `Column`, its stacked segments one doorway; a hatch along its `Row`, a piece of its own), `Build` (the build that places
  it; its piece size is the build's), `Char` and `Means` (its map character, the legend's line), `Flat` (a deck a cell thick,
  the top row of a doubled map's block), `Draw` (autotile, cut, paired, door shape) and `Tile` (its art), `Drag` (a line
  or an area). The build menu, the fuzz, `Grid.Place` and `PieceOf`, costs, plans and ghosts read these.
- **The API.** `IGridQueries` answers `Blocks`, `Supports`, `Climbable`, `Seals`, `BlocksLight` and `Solid` per cell; past
  the grid everything blocks. Code-only callers (the hero over toggles in flight, rooms over a layer, the client over what it
  drew) read `Structures.Of(code)` and `Structures.Is(code, trait)`.
- **An open hatch is a ladder on its own.** Climbable, it is climbed and stood on whether or not a ladder lies past it.
  Up takes hold of a ladder or an open hatch within `Hero.HoldReach` (a cell) over the head and climbs on into it, so a
  hatch in a deck is climbed from the room under it; climbing down still lets go where the body leaves the ladder.
- **Only the table names a kind.** `Doorways` and the door toggle read groups, joins and toggles; nothing outside
  `Structures.cs` (and tests) names a concrete code, which `StructureCodeGuardTests` enforces. `Structure.Empty`, the
  absence of a structure, is not a kind.
- **Materials and machines say what they do too.** A material's `Hurts` (lava), `Grows` (moss on soil), `Footing` (its
  boots' sound) and `Look` (cut, iron's panels, lava's molten surface); a machine's `Container` (crate), `Gas` (tank),
  `Vents`, `Zone` (the drone bay) and `Glow` (lamps). The lifepod is the `Fixed` machine; a fire smelts where its machine
  has `Inputs` and powers where it `Makes`.

A new plain structure is three files: its spec and its build (cost, ticks, piece) in `Structures.cs` (`Builds.All` is
`Structures.BuildRows`), its art and its name. The grate,
a test fixture, is one row: rooms, light and the hero handle it with no line of theirs changed (`GrateTests`). A new
behaviour is a trait and the one rule that reads it.

### Where the old "solid"s disagreed

The table decides: a top surface for chips, dust and drips (`Impacts`, `BuildingView`, `DigView`, `Planting`) is anything
`Solid`, so a shut door's or hatch's top now catches them, and a ladder no longer catches a drip. Everything else kept its
cells: the hero's and the cave view's blockers are `Blocks` (and the hero's `BlocksFromBelow`), light's and air's
`BlocksLight` and `Seals`, stacks', creatures', the grapple's and drones' `Solid`.

## Rejected

- **A class per kind with virtual methods:** the rules belong to the modules that apply them; a kind's class would gather
  rooms, light and movement in one place that none of them owns.
- **Traits computed from the kind in each module:** that is what we had.
- **A `Shut` trait shared by doors and hatches for the hero:** a shut door is a wall to a body; a shut hatch is a floor
  over it. One flag would bring back the guess 0111 removed.

## Accepted limitations

- Machines and materials keep their own tables; their traits are fields there. A system that works one kind (`MachinesOf("vent")`)
  still names it: it is that kind's rule.
- The analyzer's, furnace's, generator's and smelter's screens and the charger's light still name their kinds.
- Terrain generation names its materials; the hero's and the door's art keep their `door_`/`hatch_` names.
- The guard reads code, not meaning: a code compared as a number would pass it.
