# Grid (client)

Draws the ship's cells and machines in pixel art, a pixel of art a pixel of the world, sixteen to a
cell (TypeScript `features/grid` and `render/ship-art.ts`).

- **Reads:** `IGridQueries` only — `AfterToggles` through `ShownGrid`, `StructureLayer`, `MaterialLayer`, `ConduitLayer`, `BackLayer`,
  `BackMaterialLayer`, `Machines`; the machine's `Plug` from `Content.Machines` for its socket.
- **Draws:** a `TileMapLayer` per kind of picture, back to front: back walls (scrap cut from its
  picture, iron a panel with a grille in about one cell of seven), pipes reaching toward their
  neighbours, valves, walls cut from their material's picture, their edges on every side that faces no
  wall, floors and ladders, doors (shut or slid open, joined to the door cells above and below),
  machines over their footprints; a sprite per power socket.
- **Kept current:** `GridView.Sync(shown)` runs whenever the grid as this peer shows it changes
  (`game/shared/ShownGrid.cs`, `predictedGrid` of `session/predicted.ts`): after a tick, and on the
  frame this peer sends a door or valve toggle, which shows at once, before its tick runs (0014). It
  compares the layers with what it drew and redraws only the changed cells and their four neighbours;
  machines are redrawn when the list changes. Nothing is rebuilt per frame.
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
