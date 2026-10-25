# Render: the backdrop

## Files

| File | Purpose |
| --- | --- |
| `AsciiArt.cs` | Small pictures drawn as ASCII in code, made once under a key and kept. |
| `logic/Blueprint.cs` | How plans join the plans beside them when drawn. |
| `BlueprintArt.cs` | What a placement looks like built: for plans, ghosts and build menu thumbnails. |
| `logic/Bolts.cs` | The electric arcs in the world (0075). |
| `logic/Breath.cs` | How what is aimed at breathes: its outline and a tool's glowing faces, faint to bright on one clock. |
| `BuiltParts.cs` | What of the plans being built is already real (0063). |
| `logic/Buttons.cs` | The mouse buttons as the world reads them: presses and releases since the last take. |
| `logic/CableShape.cs` | A cable as drawn: where it meets its nodes, how it hangs, where it goes through the hull. |
| `Cracks.cs` | One damage filter for every piece of debris (0034), drawn by `Lit`'s shader. |
| `logic/DoorShape.cs` | One door, every shape (0067): a doorway's pictures by the joins of its cells. |
| `DriftLayer.cs` | A plane of the backdrop behind the ship, things scattered over its tiles by `Scatter`. |
| `DrillLight.cs` | A drill's own light while it runs (0054, 0075, 0094). |
| `Dust.cs` | Faint dust between the far wrecks and the ship. |
| `FarWrecks.cs` | Wreckage far behind the ship, from the wrecks' own pictures. |
| `logic/Flares.cs` | Glints, shock lines and flashes of the world's key moments (0075). |
| `Glow.cs` | A point that lights what is round it a little while it is shown. |
| `GroundArt.cs` | A planet's natural ground pictures made here rather than drawn (0077, 0079). |
| `logic/GroundDetails.cs` | Pictures laid over a planet's ground, a rule of drawing (0082). |
| `logic/GroundShadow.cs` | The soft shadow walls cast on a natural back wall (0088). |
| `HeldArt.cs` | A held tool's pictures with its modules' overlays (`HeldParts`, 0098). |
| `logic/HitBurst.cs` | A hit's look thrown where it lands (0094). |
| `logic/IconCanvas.cs` | A glowing line icon's pixels (`IconLooks`). |
| `logic/IconShow.cs` | Where and how a glowing line icon is drawn (0086, 0097). |
| `ItemIcons.cs` | Each item's 16 px picture and its white silhouette. |
| `Layers.cs` | Where each thing in the world is drawn, back to front. |
| `Lit.cs` | How light falls on everything the ship draws, one model. |
| `logic/Luminous.cs` | How one kind of luminous particle looks and moves (0075). |
| `MarchingDashes.cs` | Dashes marching along `OutlineRun`s. |
| `Outline.cs` | A thin line round the silhouette of what is aimed at. |
| `PixelArt.cs` | The ship's pictures from `game/art`, a pixel of art a pixel of the world. |
| `logic/PlanCut.cs` | Where a plan being built stops being a plan (0063). |
| `logic/PlanOutline.cs` | The dashed line round each group of plans and marks (0064). |
| `PlanShapes.cs` | The silhouettes of plans and marks, for their dashes. |
| `PlanTiles.cs` | The ship's tile layers again, for plans and ghosts. |
| `PlanetSky.cs` | A planet's sky behind its ground (0079). |
| `logic/Rope.cs` | A rope between two ends: points under speed, gravity and a slow drift. |
| `logic/Scatter.cs` | Where the backdrop's things lie, by a hash of seed and tile. |
| `ShadowTiles.cs` | `GroundShadow`'s shadows as tiles of one `TileSet` (0088). |
| `Shimmer.cs` | Heat shimmer (0075). |
| `ShipCamera.cs` | The camera over the ship: centring and zoom steps with `-` and `=`. |
| `ShipTiles.cs` | Every picture of the ship's cells in one atlas with one normal map. |
| `Sketch.cs` | A node drawn by a callback: lines, boxes and dots a view works out itself. |
| `SoftDots.cs` | Soft round pictures luminous things are drawn with (0075). |
| `Stars.cs` | Space behind the ship: a tile of stars over the whole screen. |
| `Strokes.cs` | Many soft-edged strokes of light in one draw call (0075). |
| `Style.cs` | The panels' colours and boxes as Godot style boxes and label settings. |
| `WorldButtons.cs` | The mouse over the world: feeds `Buttons`, a press only when no panel took it. |

The backdrop behind the ship, drawn by the client only: nothing of it reaches the sim. Numbers live in
`game/Content/logic/Backdrop.cs` and the files' constants. Everything else in `render/` is described where it is used (the
features' READMEs) and in its own `///` summaries.

## Rules

- **A planet's sky** (`PlanetSky`, 0079) replaces space on a surface with daylight, shown by `Stars`: gradient, gas
  giant, drifting cloud and sun, then far mesas, a ridge and hills with trees (`art/planet`, the set its kind's
  `PlanetLooks.Backdrop` names, 0095), each layer repeating. The land lies on the world's surface, each layer following
  its share of the camera; sun, giant and clouds hang at fixed shares of the screen. Each layer's bottom colour fills
  the screen under it (no ADR).
- **Three planes in space, far to near** (no ADR), on `ScreenLayers.Stars`, under the world and every panel; the ship's
  back walls hide them. None is lit by `Lit`.
  - *Stars*: a filtered tile repeated over the screen, a screen pixel a star at any zoom, streaming on local time.
  - *Far wrecks*: each a core of one of the wrecks' pictures (`art/debris`) with up to five pieces gathered round it.
    One grey mipmapped atlas made at start, lightness lifted to `WreckLift`, tinted between space's clear colour and
    `WreckTint` by size; opaque and soft, never outlined, overlaps not darkening.
  - *Dust*: specks one or two screen pixels wide at every zoom, drawn filtered so they glide by fractions of a pixel;
    faster than the wrecks, slower than the debris.
- **A plane** (`DriftLayer`, no ADR): a point `(x, y)` is on screen at `screen/2 + ((x, y) − centre) × zoom`, the centre
  the camera's times its parallax plus its drift (`Scatter.Centre`): it moves its parallax at every zoom and zooms about
  the screen's middle without reshuffling. Past the player's farthest zoom (`ShipCamera.FarthestZoom`), as only a dev
  view goes, it is drawn as at that zoom.
- **Placed by a hash** (`Scatter`, no ADR): each tile holds `Mean` things, rounded down or up by a hash of the plane's
  seed and the tile; each thing's place and looks come from its own hash, so panning away and back shows the same
  (`ScatterTests`). Wrecks and dust drift by the world's time (`Stars.Seconds`, set by `Main`), so every peer sees
  the same; the stars keep their local stream.
- **Drawn once** (no ADR): a plane is drawn into its canvas item and carried by the node's position and scale; it
  redraws only when the screen reaches past the tiles drawn (a tile of margin each way) or the zoom changes.

## Deliberately not handled

- Wrecks do not turn, so a plane never redraws for them; they all lie at one depth, size and colour suggesting more.
- No larger wreck art of its own; the title screen's space is its own (`menu/TitleScreen.cs`).
