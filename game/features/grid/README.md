# Grid (client)

Draws the ship's cells and machines in pixel art, a pixel of art a pixel of the world, sixteen to a
cell (TypeScript `features/grid` and `render/ship-art.ts`).

- **Reads:** `IGridQueries` only — `AfterToggles` through `Predicted.Grid`, `ChangedSince`, `StructureLayer`, `MaterialLayer`, `ConduitLayer`, `BackLayer`,
  `BackMaterialLayer`, `Machines`, `Version`, `Built`; the machine's `Plug` from `Content.Machines` for its socket.
- **Draws:** a `TileMapLayer` per kind of picture, back to front: back walls (scrap cut from its
  picture, iron a panel with a grille in about one cell of seven), pipes reaching toward their
  neighbours, valves, walls cut from their material's picture, their edges on every side that faces no
  wall, floors and ladders, doors and hatches (shut or slid open; a door cell joined to one above or below
  is a door's, every other a hatch's, its pieces joined to either side: `render/DoorShape.cs`, 0067),
  machines over their footprints; a sprite per power socket.
- **Heard:** a door or valve that moved, where it stands (0033); a doorway once, at its cell with no
  door above it or to its left (`DoorShape.Leads`).
- **Kept current:** `GridView.Sync(shown)` runs whenever the grid as this peer shows it changes
  (`game/shared/Predicted.cs`'s `Grid`, `predictedGrid` of `session/predicted.ts`): after a tick, and
  on the frame this peer sends a door or valve toggle, which shows at once, before its tick runs (0014). It
  brings what it drew (`DrawnLayers`, plain C#) up to the grid and redraws only the changed cells and
  their four neighbours; machines are redrawn when the list changes. Nothing is rebuilt per frame.
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

## Deliberately not handled

- Pipes overlay, sockets where conduits join machines: they belong to the pipes feature (G-08);
  `socket.png` is exported for it. The furnace's fire is the furnace feature's (`fire_*.png`).
- A grid that changes size: the sim's grid never does.
