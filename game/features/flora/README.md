# Flora (client)

## Files

| File | Purpose |
| --- | --- |
| `CaveView.cs` | What grows in a planet's caves, seen (0080): chunks of growths, their glows, lights, glints and drift. |
| `FloraView.cs` | What grows on a planet's surface, seen (0079): a sprite per plant standing, its glow, lights and motes. |
| `logic/BoxesByLeft.cs` | Boxes kept sorted by their left edge: the glowing plants near the view found each frame without walking every plant (planet-frame-rate). |
| `logic/Felling.cs` | Which plants were felled since the cave view last looked: the cells it kept and those standing halved by binary search, not a set of all. |
| `logic/Planting.cs` | Where what grows is drawn against what holds it (0079, 0080), in world pixels, plain C# so it is tested. |
| `logic/Shedding.cs` | When a runner shakes leaves off a plant it brushes, at most once a while each (plant-stirs). |
| `logic/Topple.cs` | A felled tree falling over about its foot, away from its feller, rebounding once (plant-stirs). |
| `logic/VeinGlints.cs` | Which vein cells glint on their open faces: one in `CaveLooks.VeinGlintEvery`, by the planet's seed and the cell. |

What grows on a planet, seen: on its surface (0079) and in its caves (0080). The plants are Building's
(`sim/Building/Flora.cs`): grown from the seed with the ground, felled by a hit or with what holds them. This feature
only draws them — the surface's in `FloraView`, the caves' in `CaveView`; the stone by depth is the grid's (`Strata`).

## Surface (0079)

- **Reads:** `BuildingModule.Plants` (`Plant(Cell, Kind)`, ascending, only ever fewer) and, for the motes,
  `IGridQueries.StructureAt` and `BackAt` down the columns in view.
- **Draws** a `Sprite2D` per plant standing (a few hundred, so no tile layer), `Lit`, at `Layers.Flora`, over the
  ground and its moss, behind the heroes. Its picture is `art/planet/{kind}_{variant}`, the variant a hash of its cell,
  planted (`Planting.Picture`): its lowest drawn row `FloraLooks.Of[kind].Sink` pixels under the top of the soil, in
  the middle of its cell, those rows not drawn, so it stands in the ground. What glows (`{kind}_{variant}_glow`) is a
  second sprite added unlit at `Layers.Flora`, never over a hero in front of it.
- **Light:** each glow picture's brightest six-pixel squares, at most `FloraLooks.MostLights` a plant, light in their
  colour (`Lights`, read by the light feature for the plants near the view), found by their boxes (`BoxesByLeft`), never by
  a walk over every plant each frame.
- **Felled:** a plant gone from `Plants` falls away in `FloraLooks.Fallen` leaves (a rock's pebbles), matter behind the
  walls (`Luminous.Low`). Only the count is compared each frame; the list is walked only when it changed (no ADR). An
  acacia (`Stir.Topples`) instead falls over about its foot, away from the hero nearest it (`Topple`), leaves flying off
  its crown as it starts and where it lands; it lies `LieSeconds`, then fades.
- **Stirred** ([plant-stirs](../../../docs/decisions/2026-11-10-plant-stirs.md)): every hero's feet push the grass round them
  (`Stirs`, fed here each frame and handed to `Bend`); the grid's grass edge and the tufts (`Stir.Bends`) bend by it. A
  runner whose body meets a bush or an acacia (its box in `BoxesByLeft`) shakes it and sheds `ScatterLeaves` off its crown,
  at most once a plant each `ScatterEvery` (`Shedding`). How grass answers is `FloraLooks.Feeling`; F8 cycles `Feels` in
  `--planet`.
- **Motes** (0075): while a glowing plant is in view, `FloraLooks.MotesASecond` soft dots, at most `MostMotes` alive:
  `NearShare` by a glowing part in its colour, the rest pale gold in the open air of a column in view (its first wall
  down, under no back wall). They drift up, curling, fade over 3.5–7 s and give no light; none off the screen.

## Caves (0080)

Light that changes with depth, glow moss and alien threads: cold blue near the surface, teal and violet in the middle,
warm amber and red near the bottom.

- **Reads:** `BuildingModule.Plants` of a cave kind (`Content.Flora.Cave`), `IGridQueries.StructureAt` and
  `ChangedSince`, `Terrain.BandOf`, and where each helmet lamp shines (`HeroView.HeadLamps`).
- **Draws** by chunks of 64 × 64 cells within a chunk of the view, let go past three: a chunk is two canvas
  items, pictures `Lit` and glows added unlit, both at `Layers.Flora`, each from one atlas of every cave picture
  (`art/caves`), so a chunk is a batch apiece; no node per growth.
- **Planted:** a floor's growth as a surface plant, `CaveLooks` `Sink` pixels into the wall under it; a ceiling's
  mirrored; one in the rock 24 × 24 round its cell. A picture is `{kind}_{variant}`; a stalactite's or stalagmite's
  `{kind}_{band}_{variant}`, in its band's stone. Moss is one picture per open face of its stone (`top` tufts, `bottom`
  strands, `left`/`right` a film), green in the cold band, teal deeper; a run of moss on one face has its pieces cut to meet at their blocks' edges
  (`Planting.Piece`, [its ADR](../../../docs/decisions/2026-11-01-moss-in-ink.md)). No other kind tiles.
- **Stirred:** a floor's moss and its glow (`Front`, `FrontShine`) bend as heroes brush them (plant-stirs).
- **Redrawn:** a felled growth's chunk (a glowing one puffing spores, a spike its pebbles); the chunks round a grid change.
- **Pictures** are cut from the cave concept's drawing code by its export, glow blooms in `_glow`: change the code and
  export again, never edit the pictures. The ruins' cache and the spire's lens (0085, 0096): the lens lit while its
  relic stands, by the ruins' view.
- **Light** (`CaveLooks`): each glowing growth's one light, from its glow's middle, in its colour, at its kind's
  strength and reach: the only light in a cave. Always from the open (`Planting.Light`): `Planting.LightOff` off a
  floor's or ceiling's face, one in the rock just past its open face nearest its glow; `CaveLooks.LightSize` wide, so
  shadows are soft. Only those whose reach touches the view, nearest its middle first, at most `CaveLooks.MostLights`,
  are its `Lights` (`ILightSource`). A glow picture fades to nothing over its last three pixels (`Atlas.Faded`).
- **Glints:** mica, crystals and geodes sparkle where a helmet's beam falls on them with nothing between,
  as bright as the beam there. Mica draws nothing else. An ore's vein cell (any `Material.In`) glints on an open face
  in its `GroundLooks` fleck, but only one cell in `CaveLooks.VeinGlintEvery`, picked by the planet's seed and the cell
  (`VeinGlints`), so dark rock stays dark and a glint means ore; a pocket near the surface (mostly 1–3 cells, 95 % at most 6, none
  over 60) may show none, though its ore still shines in lamp light (`render/Lit.cs`: every vein cell holds some).
  Found as a chunk is drawn, never a frame at a time; a chunk with veins and no growth is drawn for its glints alone.
- **Drift** (0075): spores up from glowcaps; drips from one stalactite in `DripShare`, landing on the first solid
  cell under its tip within `DripRows` and splashing (`Planting.DripFloor`), none with no open cell under it; faint
  embers from heat cracks. `MotesASecond` from the sources in view, at most `MostMotes` alive, none off the screen.

## Deliberately not handled

- Wind, idle sway, seasons: a plant moves only when a body brushes it or a hit shakes it (plant-stirs).
- A plant's light is a small lamp's in daylight too, where it hardly shows (no ADR).
- Moss grows only on stone, on whichever faces are open now, so digging beside it shows more of it.
