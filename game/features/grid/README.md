# Grid (client)

Draws the ship's cells and machines in pixel art, a pixel of art a pixel of the world, sixteen to a
cell (TypeScript `features/grid` and `render/ship-art.ts`).

- **Reads:** `IGridQueries` only — `AfterToggles` through `Predicted.Grid`, `ChangedSince`, `StructureLayer`, `MaterialLayer`, `ConduitLayer`, `BackLayer`,
  `BackMaterialLayer`, `ShapeLayer`, `Machines`, `Version`, `Built`; the machine's `Plug` from `Content.Machines` for its socket.
- **Draws:** a `TileMapLayer` per kind of picture, back to front: back walls (scrap cut from its
  picture, iron a panel with a grille in about one cell of seven), pipes reaching toward their
  neighbours, valves, walls cut from their material's picture, their edges on every side that faces no
  wall's whole side; a wall cut to a shape (0069) drawn from the same picture, each cell cut by its mask at load
  (`render/ShapeMasks.cs`, `ShipTiles.WallName`), its diagonal edged in the material's own edge colours and its
  edges on its whole sides only (`ShipTiles.EdgeName`); the light reads it as a whole wall for now,
  floors and ladders, doors and hatches (shut or slid open; a door cell joined to one above or below
  is a door's, every other a hatch's, joined as the sim's `Doorways` joins them, one straight run a doorway;
  pictures by `render/DoorShape.cs`, 0067),
  machines over their footprints; a sprite per power socket.
- **Heard:** a door or valve that moved, where it stands (0033); a doorway once, at its cell joined
  neither above nor to its left (`DoorShape.Leads`).
- **Slid:** a doorway seen to open or shut slides its panel over 0.2 s, whatever its length (`DoorSlides`, plain C#,
  tested): one panel across all its cells, a door's rising into its top, a hatch's sliding into its left end. Each
  cell draws the part of the panel over it, two pixels a step (`DoorShape.Picture(hatch, index, length, shift)`): a
  picture by the cell's piece, the piece whose panel is over it and the one after, made at start from the shut and
  open pictures (`DoorShape.Slid`) and packed in the atlas, so a slide is tiles swapped, never a node; doorways
  longer than five cells repeat their middles' pictures. Turned back part way, it slides back from where it is.
  Eased (0075): slow off, quick through the middle, slow into its slot (`DoorLight.Ease`), still two pixels a step.
  In light, told once a doorway from its leading cell (`DoorLight`, 0075): while it slides, a thin line of light along the
  panel's leading edge, brightest halfway; opened, a few fine motes shaken loose from the slot drifting down and a
  small glint there; shut, a faint flat ring along the floor (a door's), a small glint at the seal, a little dust
  sideways and a pressure door's hiss of vapour.
  Only the look (0071): the sim moved the door at once, and air, rooms, feet and light follow the sim.
- **Kept current:** `GridView.Sync(shown)` runs whenever the grid as this peer shows it changes
  (`game/shared/Predicted.cs`'s `Grid`, `predictedGrid` of `session/predicted.ts`): after a tick, and
  on the frame this peer sends a door or valve toggle, which shows at once, before its tick runs (0014). It
  brings what it drew (`DrawnLayers`, plain C#) up to the grid and redraws only the changed cells and
  their four neighbours, a door cell come or gone its corners too (a hatch beside the cell above or below it
  joins anew); machines are redrawn when the list changes. Nothing is rebuilt per frame.
- **Costs the cells changed, not the ship or the world** (F-7, 0051). When the grid's `Version` moved, the
  cells its log names since the last look (`ChangedSince`) are compared with what was drawn; machines
  are compared only when the log names a machine's cell. The first drawing, or a look the log no
  longer reaches back to, compares within what is built now and was at the last look
  (`shared/BuiltRows.cs`, row by row, F-6 slice 0): past both every layer is empty, in the grid and in
  what was drawn, and draws only the cells with anything in them. The cells a toggle in flight shows
  apart (`ShownGrid` holds just those, from `AfterToggles`), now or the time before, are looked at
  every time. `BuiltBoundsTests` checks the changes found against a whole-grid compare over random
  ships, edits inside, on the hull and out in space, toggles in flight and logs outrun. A door toggled
  every tick on a fully built 1024×512 world, whose built box is the world, cost the grid 0.93 ms a
  frame; now 0.03 ms (slice 0 took one on a 2048×1024 world with a 192×96 ship from 19 ms to 0.004 ms).
  The grid tests of `session/predicted.test.ts` are ported (`game.tests/ShownGridTests.cs`); the light
  follows the world, not toggles in flight, as in TypeScript.
- **Pictures:** every PNG of `game/art` that cells use is packed at start into one atlas with one
  normal map (`render/ShipTiles.cs`), so a layer's piece of the screen is one draw call. Walls, edges,
  doors, machines and sockets are lit with relief from their normal maps; back walls, pipes, floors
  and ladders flat (`render/Lit.cs`).
- **A planet's ground** (0077, 0078): soil, stone and bedrock walls and back walls are drawn in the "Stillwake" style,
  `art/planet/wall_*` and `back_*`, 8 × 4 cell tiles that repeat seamlessly, cut a cell at a time as scrap's; their edges
  are made at start (`render/GroundArt.cs`, lines in `Content/GroundLooks.cs`): a dark outline by the open sides and inside
  it a lit line on top, shaded elsewhere. Slopes are cut from them as from the hull's.
- **Moss** (0078) on soil open to the sky (`Grass.cs`, plain C#, tested: a soil wall, whole or a floor slope, under an
  empty cell with no back wall), a rule of drawing, never the sim's: one of four pictures a top (`art/planet/grass_{shape}_{v}`,
  by a hash of the cell), two cells tall, laid from the cell above so its stems rise into it, over the edges. Dug, built
  over or roofed, the soil is bare; its cell's drawing alone lays and clears it, so a change above or below redraws it.
- **Drawn near the camera** (0078): the grid is drawn in 32 × 32 chunks, each the first time the view or a chunk round it
  reaches it; a cell changed in a chunk not drawn is drawn with it, from `DrawnLayers`. A planet is built in nearly every
  cell: drawing all 1.5 million at its first look froze it for a second; now some 11 ms that frame at the default zoom,
  31 ms zoomed far out, and 2–4 ms a frame as a walk reveals a column of chunks.

## Deliberately not handled

- Pipes overlay, sockets where conduits join machines: they belong to the pipes feature (G-08);
  `socket.png` is exported for it. The furnace's fire is the furnace feature's (`fire_*.png`).
- A grid that changes size: the sim's grid never does.
