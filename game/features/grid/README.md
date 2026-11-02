# Grid (client)

## Files

| File | Purpose |
| --- | --- |
| `logic/ChunkReveal.cs` | Which of the grid's cells `GridView` has drawn, by chunks `Chunk` cells square drawn a row at a time, and which to … |
| `DoorLight.cs` | A doorway's slide in light (0075), one doorway at a time, from its leading cell: while the panel moves, a thin line … |
| `logic/DoorSlides.cs` | The door cells sliding open or shut, each taking `Seconds` from one end to the other; the cells of a doorway move … |
| `logic/DrawnLayers.cs` | The grid's six layers as `GridView` last drew them, this peer's toggles in flight made (0014), and the cells that … |
| `logic/Grass.cs` | Where a planet's ground shows its living edge (0079, 0082), a rule of drawing, never the sim's: moss on a wall of … |
| `GridView.cs` | The ship in pixel art, a `TileMapLayer` per kind of picture: back walls behind … |
| `LavaView.cs` | The planet's lava (its core, `Content.Lava`), drawn as the cave concepts drew it (0087): over its own tiles, unlit … |
| `logic/Grid.En.cs` | Its player text (`En`) |
| `logic/WreckMotion.cs` | What still moves on the wreck: wires, arcs and a thread of vapour, each while its tear shows (2026-10-31-the-scoop). |
| `WreckMotionView.cs` | Steps `WreckMotion` and paints its wires, unlit, over the hull. |

Draws the grid's cells and machines in pixel art, a pixel of art a pixel of the world, `Camera.Cell` (8) to a cell (0104).

- **Reads:** `IGridQueries` only: `AfterToggles` through `Predicted.Grid`, `ChangedSince`, `StructureLayer`,
  `MaterialLayer`, `ConduitLayer`, `BackLayer`, `BackMaterialLayer`, `Machines`, `Version`, `Built`; the machine's `Plug`
  from `Content.Machines` for its socket.

## Rules

- **A machine held back** (`HeldBack`, `HeldBackVersion`, set by `SurfaceViews`): one another view draws moving, a pod coming down,
  is left out of the machines' layer till it is let go; the layer is drawn again when the held set changes.
- **Draws** a `TileMapLayer` per kind of picture, back to front: back walls (scrap cut from its picture, iron a panel with
  a grille in about one 16 px cell of seven), walls cut from their material's picture with edges on every side facing no
  wall (`ShipTiles.WallName`, `EdgeName`; every wall whole, 0106), floors and ladders, doors and hatches (shut or slid; a
  door's stacked segments one doorway, a hatch its own, joined as the sim's `Doorways`; `render/logic/DoorShape.cs`, 0067,
  0111), machines over their footprints, a sprite per power socket.
- **Heard** (0033): a door that moved, where it stands, once a doorway at its leading cell (`DoorShape.Leads`); a valve
  is the pipes' own (0109).
- **Slid** (0075): a doorway seen to open or shut slides one panel across all its cells over `DoorSlides.Seconds`, a
  door's rising into its top, a hatch's into its left end, two pixels a step, eased (`DoorLight.Ease`). Each cell draws
  the part over it (`DoorShape.Picture(hatch, index, length, shift)`), pictures made at start from the shut and open ones
  (`DoorShape.Slid`) and packed in the atlas, so a slide is tiles swapped, never a node; long doorways repeat their
  middles. Turned back part way, it slides back from where it is.
- **In light** (0075), once a doorway from its leading cell (`DoorLight`): a thin line of light along the moving edge;
  opened, motes and a glint at the slot; shut, a faint ring on the floor, a glint at the seal, dust, a pressure door's
  vapour. Only the look (0071): the sim moved the door at once; air, rooms, feet and light follow the sim.
- **Kept current** (0014, 0051): `GridView.Sync(shown)` runs whenever the grid as this peer shows it changes
  (`game/shared/logic/Predicted.cs`'s `Grid`): after a tick, and on the frame this peer sends a door or valve toggle, shown at
  once. It brings `DrawnLayers` up to the grid and redraws only changed cells and their four neighbours (a door cell's
  joins too); machines when the list changes. Nothing is rebuilt per frame.
- **Costs the cells changed** (0051): when `Version` moved, the cells `ChangedSince` names are compared with what was
  drawn; machines only when the log names a machine's cell. A first drawing, or a log outrun, compares within what is
  built now and was at the last look (`shared/logic/BuiltRows.cs`). Cells a toggle in flight shows apart (`ShownGrid`) are
  looked at every time. `BuiltBoundsTests` checks this against a whole-grid compare. Light follows the world, not
  toggles in flight.
- **Pictures** (no ADR): every PNG of `game/art` cells use is packed at start into one atlas with one normal map
  (`render/ShipTiles.cs`), a layer one draw call. Walls, edges, doors, machines and sockets are lit with relief; back
  walls, pipes, floors and ladders flat (`render/Lit.cs`).
- **A planet's ground** (0077, 0079, 0088): soil, stone and bedrock walls and back walls from `art/planet/wall_*` and
  `back_*`, seamless tiles cut a cell at a time; edges made at start (`render/GroundArt.cs`, `Content/logic/GroundLooks.cs`):
  a dark outline by open sides, lit lines inside. The ruins' alloy (0085) is drawn the same way (`wall_alloy`,
  `back_alloy`, `top_alloy`, `detail_alloy`; placeholders till the user picks).
- **Back walls recede** (0088): a natural back wall's picture is paled, cooled and darkened at load (`GroundArt.Recede`);
  walls cast a soft three-tone shadow on it along open edges (`render/logic/GroundShadow.cs`, `GroundShadow.Of` of the three by
  three round a cell; atlas `render/ShadowTiles.cs`), on its own layer, multiplying. A changed cell redraws its corner
  neighbours' shadows too. The hull's back walls are left as they are.
- **Lava** (0087): its tiles drawn as any ground's (`art/planet/*_lava`, no living edge); over them `LavaView` draws the
  band unlit, one quad, a value-noise shader churning slowly; over open lava a warm haze and rising embers
  (`CaveLooks.Ember_`). Under rock its dark crust, cracks dimly glowing, from a row texture laid again when `Version`
  moves. It gives light where open above it: a lamp of `LavaLooks.Light` every `LavaLooks.LightEvery` cells of an open
  stretch, out from the view's middle, at most `LavaLooks.MostLights` (its `Lights`). Never dug nor built, so the band is the
  ground's rows (`Ground.CoreRows`, `LavaRows`).
- **Moss** (0079, 0082) on soil open to the sky (`Grass.cs`: a soil wall under an empty cell with no back wall), a rule
  of drawing, never the sim's: one of `GroundArt.TopVariants` pictures by a hash of its 16 px column
  (`ShipTiles.TopName(material, variant, front, half)`), each cell the half its column's parity picks (0104), laid from
  two cells above over the edges. Details lie by 16 px cells, cut into the cells they cover. Dug, built over or roofed,
  the soil is bare; its cell's drawing alone lays and clears it, so a change above or below redraws it.
- **Drawn near the camera** (0079): in `ChunkReveal.Chunk`-square chunks, a row at a time: every chunk the view touches
  whole at once, so nothing pops in; those within `Margin` cells ahead, nearest first, at most `Budget` cells a frame. A
  cell changed in a row not drawn yet is drawn with it, from `DrawnLayers`.

## Deliberately not handled

- Pipes, their overlay and conduit sockets are `features/pipes`'s; the furnace's fire (`fire_*.png`) is
  `features/furnace`'s.
- A grid that changes size: the sim's grid never does.
