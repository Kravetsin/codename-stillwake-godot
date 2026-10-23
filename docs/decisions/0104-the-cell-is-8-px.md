# 0104 — The cell is 8 px

- Status: accepted; supersedes [0005](0005-terraria-scale.md)'s 16 px cell, its reference world and its doorway "three `D`s", and its air
  as [0105](0105-a-room-airs-four-cells-a-beat.md) has it; amends [0007](0007-spacewalks-and-airlocks.md)'s tether anchor
- Date: 2026-10-22
- Design: `designs/cell-8px.md` (the session's scratchpad); its amendments, the user's answers, override its top

## Context

At 16 px a cell is 38% of the hero's height. Building is coarse, the ground is blocky, and walking needs slopes (0069), which
have cost three rounds of foot IK, creature jerks and special collision. Halving the cell gives building and digging the
grain of the hero's own body, and lets a later change replace slopes with steps.

## Decision

- **A cell is 8 px** (`Camera.Cell`, which `ShipCamera.Cell` and `Rig.Cell` read). Every picture keeps its pixels: the hero is
  about 42 px tall as before, a tank 32 px.
- **One rule for every number: lengths double, quantities per area ×4, quantities per cell unchanged.**
  - ×2: the hero (`Hero`: width 1900, height 5250, every speed, gravity 42, jump 830, reach 12000; ratios and ticks kept), the
    creatures (4×4 cells, every speed, reach and box), stacks thrown and falling, the harpoon, the jetpack, debris (margins,
    leads, views, drift, tiers, the asteroid band), the field's margin, the pod's reach, lava's, the resonator's, drones' reach
    and speed, a cable's span, every row, column and wave of a planet type (the first planet 4096×1536; its noise marks, bands
    and chances kept), growths (2×2 at the least: `Flora` `Across` 2, `Tall` ×2), machine footprints and ports (a tank 4×4,
    a vent 2×2, the lifepod 6×6), the camera's farthest view (192×108), lights' reaches; in the client every length in cells
    or thousandths of a cell.
  - ×4: oxygen counted by area (0105).
  - ÷4, rounded to the nearest, a half up: a wall's or a back wall's build ticks a cell (scrap 3, iron 7, soil 3, stone 5,
    alloy 10). Halved, rounded up: a wall's cost a cell (scrap 1), so a wall of the same length, one cell thick, costs about
    what it did.
  - Unchanged, per cell: strength, leak, what a dug cell yields, weapons' damage and pace, every tick count, hit points.
  - A per-cell falloff in the client (a light's keep) becomes its square root: light falls off over the same pixels.
- **Pieces: a ladder and a door segment are built whole, the size and the price of a 16 px cell** (the user, after playing the
  8 px build: "shrinking these objects was a bad idea"). A ladder is a piece 2×2, a scrap and 7 ticks, as a 16 px ladder cell
  was; a door segment 2×2, two plates, a gear and 20 ticks, as a 16 px door cell was (`Builds`' `Across` and `Tall`,
  `Builds.PieceOf`). A hatch segment (`Builds.Hatch`) is a door segment laid flat, 2×1 as its art is, of the door's blueprint
  and at its cost, so a ladder meets it flush and the hero climbs through it as through a drawn map's hatch (a 2×2 one left a
  row empty under its picture, and the hero fell through that gap). The door's card lays hatch segments by a click or a
  stroke across, along their row, and door segments along a stroke down as far as a second (`Drag.Oriented`); its ghost shows
  which. A run down is a door, a run across a hatch, a segment alone a hatch two cells wide, which the hero (1.9 cells) fits
  through. A run is one doorway: it opens, shuts, leaks, lights and is drawn as one, its plans and ghosts too, joined over
  what is planned and built together (`Tools.DoorLinks`, `PlanTiles.LayDoor`). (A door 2×6 and a hatch 6×2 built whole were
  tried first and dropped: a hatch was always huge.)
  One blueprint covers a piece's footprint (`IGridQueries.Footprint`), refused if any of its cells is taken; building it fills
  all of them. A mark on any of its cells takes the piece apart, kept at its first cell (`IGridQueries.PieceOf`): a ladder's or
  a door's two by two, its columns paired from the left end of its row's run and its rows from the top of its column's, the
  rest of a doorway standing; it gives back its piece's cost for as many pieces as its cells make, each item rounded down, a
  piece in one row a hatch segment's, so a drawn map's lone door (2×2) and a hatch segment give a segment's, and a ladder
  piece a meteor broke gives nothing.
- **A doorway is two cells thick** (`Doorways.Thick`), a built hatch one: a door cell in a run of more than two up and down
  stands in a door; every other lies in a hatch; a cell joins the door cells above and below it and those either side that
  stand as it does, so a door's two columns open and shut as one, a drawn hatch's two rows too, and a hatch still stops
  beside a door (0067). The client draws each 16 px door or hatch picture whole over two by two cells, by where a cell lies
  along its doorway and across it (`DoorShape.SpotOf`, `TileOf`); the hatch's art fills the top cell, so a built hatch
  shows all of it and a drawn one leaves its lower row the back wall's. A hero climbs through an open hatch as far as it is
  thick.
- **Maps are drawn a character a 16 px cell and doubled** (`Layout.Doubled()`): walls, back walls, machines, ladders and doors
  fill the 2×2 cells of their character, so a doubled doorway of three `D`s is a door of three segments, 2×6, standing in a hull two cells thick;
  a floor fills its block's top row; `S` its bottom-left; a conduit its
  top-left and the cells toward neighbouring conduits, so pipes stay one cell wide and meet the doubled ports; materials,
  cables, the zone and plants double with the map. Slopes throw: they are not doubled. The wreck, the strip, the ruins'
  prefabs, the dev maps and test scenarios (by default; `cells: true` reads a map a character a cell) are doubled.
- **The ship world stays 2048×1024 cells**, so it is physically half as big; its zone stays 1024×512.
- **A wall two cells thick still leaks** (Rooms): a solid cell beside a room is hull when the outside is beyond it, or beyond
  the solid cell behind it away from the room; a wall of three leaks nothing. So a doubled hull, a 16 px one, leaks through
  its inner layer, hull leak per length ×2 with the cells, as 0005's ratio had it.
- **Creatures step up and down `Fauna.StepUp` = 2 cells**, the one 16 px cell they stepped, and move in parts of under a cell, so
  a flight of 1500 a tick never passes a wall one cell thick.
- **The reference world is 512×256** (the 256×128 drawing doubled); CLAUDE.md's line is the user's to change.
- **Pictures drawn at 16 px:** what fills a character's cells (a floor, iron's back walls) is cut by where it lies; a ladder by
  its run's columns from its left end; a door's and a hatch's whole over its pieces' cells; what is one cell (pipes, valves, the
  hull's edges) is drawn at half its size till it is drawn at 8 px. A plan, a ghost or a mark of a piece is its whole picture
  from its top-left, as a machine's is. A build card shows its thing at the art's 16 px, never `Camera.Cell`. A
  planet's living edge is a 16 px column's half by its column's parity, laid from two cells over the ground; its details lie
  by 16 px cells.
- **Saves are format 28**; every save before is refused, its steps and fixtures deleted (0031's list shows them grey).

## Rejected

- **Keeping the planet's cell count (2048×768), a quarter of its area:** the user kept its physical size; the measurements were
  within the budgets (the PR's report).
- **Hull walls doubled to one cell thick:** the wreck would lose its 16 px walls; a hull two cells thick leaking through its
  inner layer keeps both the look and the leak.
- **Doors one cell thick, and ladders and doors laid a cell at a time:** first chosen to match a one-cell hull; the user overruled
  it after playing: two thin halves side by side read as broken, and a door a hero barely fits read as a slot.
- **A door costing per cell:** a doorway of twelve cells cannot split six plates and three gears; one blueprint a piece can.
- **Light tuned at the old scale behind a conversion in the shader:** every number would then mean 16 px cells while the
  sim's mean 8; reaches and keeps were converted where they are written instead.

## Consequences

- A breath is a cell of air, now a quarter of what it was: four heroes in the wreck's first room no longer outrun its
  generator; breathing from the suit weighs less against a tank. Retuned after a playtest, as 0005 was.
- The planet's gravity on the hero rounds to 29, not the 28 twice the old 14 would be: a held jump there rises 11.6 cells, not
  12, and clears twelve with the ledge assist.
- Planet surfaces grow fewer trees, bushes and rocks: level ground over their doubled `Level` columns is rarer at the finer
  grain (`FloraTests`); flowers are unchanged.
- One-cell gaps between walls, tiny rooms, become common: visible and harmless. A ladder is climbed centred between its
  two columns.
- Two doors built side by side, or end to end, open as one doorway and come apart as one, giving back both. A hatch built over
  a door's column, touching it, gives the door its cells where they meet, as 0067 had it a cell thick.
- A ladder cell or a door cell lost alone (a meteor) leaves its piece drawn as it was till something beside it changes.
- The planet costs some four times what it did: grown in about 0.6 s, opened on a tick of about 1.7 s, some 290 MB held by the
  sim (the PR's measurements).
- The tether ties at the body's middle as it leaves the hull (0007), not the middle of its cell: at 8 px a cell's middle lies
  below a standing hero's.
- Digging is four times slower per volume till the brush (2×2 a stroke) lands with it.
