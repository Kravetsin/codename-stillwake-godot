# Render: the backdrop

## Files

| File | Purpose |
| --- | --- |
| `AsciiArt.cs` | Small pictures drawn as ASCII in code: a character a pixel, coloured by a palette, made once under a key and kept. |
| `Blueprint.cs` | How plans join the plans beside them when drawn. |
| `BlueprintArt.cs` | What a placement looks like built: the ship's own pictures, for plans, ghosts and the build menu's thumbnails, so a … |
| `Bolts.cs` | The electric arcs in the world (0075, slice 2), the electric spear's look the user picked on the lightning page: a … |
| `BuiltParts.cs` | What of the plans being built is already real (0063): pieces of the ship's own pictures, each up to its plan's … |
| `Buttons.cs` | The mouse buttons as the world reads them: the left and right buttons' presses and releases since the last take, in … |
| `CableShape.cs` | A cable as drawn: where it meets its nodes, how it hangs, and where it goes through the hull. |
| `Cracks.cs` | One damage filter for every piece of debris (0034): pixel-art cracks drawn by the lit pictures' shader (`Lit`) on … |
| `DoorShape.cs` | One door, every shape (0067): the pictures a doorway is drawn with by the joins of its cells, as the sim decides … |
| `DriftLayer.cs` | A plane of the backdrop behind the ship: things scattered over its tiles by `Scatter`, drawn … |
| `DrillLight.cs` | A drill's own light (0054, 0075, 0094), one a figure: while it runs, the arc's crackle over every mount of it an … |
| `Dust.cs` | Dust between the far wrecks and the ship: faint specks a screen pixel or two wide whatever the … |
| `FarWrecks.cs` | Wreckage far behind the ship: each wreck a core of one of the wrecks' own pictures with up to … |
| `Flares.cs` | The glints, shock lines and flashes of the world's key moments (0075): the features stamp them, the effects layer … |
| `Glow.cs` | A point that lights what is round it a little, as an ember does, while it is shown: the planner's screen in a … |
| `GroundArt.cs` | The pictures of a planet's natural ground made here rather than drawn (0077, 0079): the edges of a cell by its open … |
| `GroundDetails.cs` | The pictures laid over a planet's ground (0082), a rule of drawing, never the sim's: a pebble, a root or a crack in … |
| `GroundShadow.cs` | The soft shadow walls cast on a natural back wall along their open edges (0088), a rule of drawing: the walls' … |
| `HeldArt.cs` | A held tool's pictures with its modules' overlays laid over them (`HeldParts`, 0098), put together once a picture … |
| `HitBurst.cs` | A hit's look thrown where it lands (0094): each of its throws out of the struck face, its glint, shock line and flash. |
| `IconCanvas.cs` | A glowing line icon's pixels (`IconLooks`), painted as the arc icon page's canvas strokes them: each line in … |
| `IconShow.cs` | Where a glowing line icon is drawn (0086, 0097), and so how: each as the arc icon page draws it. |
| `ItemIcons.cs` | Each item's picture, sixteen pixels square: in slots, in the hand, on the floor; and its silhouette in white, the … |
| `Layers.cs` | Where each thing in the world is drawn, back to front, as the TypeScript game stacked its feature layers: every … |
| `Lit.cs` | How light falls on what the ship draws, one model for all of it — cells, machines, heroes, items, debris: every lit … |
| `Luminous.cs` | How one kind of luminous particle looks and moves (0075), in pixels of the world, milliseconds and seconds. |
| `MarchingDashes.cs` | Dashes marching along `OutlineRun`s, in the node's `CanvasItem.Modulate`, as wide as the aim's outline … |
| `Outline.cs` | A thin line round the silhouette of what is aimed at: the pictures added are drawn together as masks, each in its … |
| `PixelArt.cs` | The ship's pictures from `game/art` (exported from the TypeScript ASCII art, a pixel of art a pixel of the world) … |
| `PlanCut.cs` | Where a plan being built stops being a plan (0063): on one side of `Line`, in pixels of the world, it is drawn as … |
| `PlanOutline.cs` | The dashed line round each group of plans and marks (0064): a strip just outside the silhouette of their pictures … |
| `PlanShapes.cs` | The silhouettes of plans and marks, for their dashes (`PlanOutline`): the rim of the opaque pixels of what a … |
| `PlanTiles.cs` | The ship's tile layers again, for what is not built yet: plans and ghosts are laid here, each picture on the layer … |
| `PlanetSky.cs` | A planet's sky behind its ground (0079), in the "Stillwake" style (`art/planet`): the day's gradient, a pale gas … |
| `Rope.cs` | A rope between two ends: points carried on by their speed, pulled by gravity and nudged by a slow drift, its links … |
| `Scatter.cs` | Where the backdrop's things lie: each tile of a plane holds what a hash of its seed and the … |
| `ShadowTiles.cs` | The shadows walls cast on natural back walls (`GroundShadow`, 0088) as tiles of one `TileSet`, made the first time … |
| `Shimmer.cs` | Heat shimmer (0075): what is drawn under a box of the world, sampled from the screen again with each row nudged … |
| `ShipCamera.cs` | The camera over the ship: it centres a world point, zooms in steps with `-` and `=` from the farthest, `Farthest` … |
| `ShipTiles.cs` | Every picture of the ship's cells packed into one atlas, with one normal map beside it, as the single source of one … |
| `Sketch.cs` | A node drawn by a callback (a Pixi `Graphics`'s place): lines, boxes and dots a view works out itself, redrawn on … |
| `SoftDots.cs` | The soft round pictures luminous things are drawn with (0075), white to tint, drawn with linear filtering. |
| `Stars.cs` | Space behind the ship (the stars of features/debris in TypeScript): a tile of stars over the whole screen, a screen … |
| `Strokes.cs` | Many soft-edged strokes of light in one draw call (0075): each a run of segments, a quad apiece in one multimesh … |
| `Style.cs` | The Tailwind colours and boxes the React panels used, as Godot style boxes and label settings. |
| `WorldButtons.cs` | The mouse over the world: feeds `Buttons` from Godot's input, a press only when no panel took it, and says where … |

A planet's sky (0079) is `PlanetSky.cs`, shown by `Stars` in place of space on a surface with daylight: the "Stillwake"
style's sky gradient, a pale gas giant, high cloud drifting, the sun added over it with its glint, then far mesas, a ridge
and hills with spindly trees (`art/planet`, the set its kind's `PlanetLooks.Backdrop` names, 0095), each layer repeating across every 2048 pixels. A pixel of art is a pixel of
the world at the camera's zoom; the land's ground line lies on the world's surface (the spawn's ground), each layer
following 0.1, 0.18 and 0.3 of the camera, the gradient 0.04; the sun, the giant and the clouds hang at fixed shares of
the screen, so they stay in the sky however near the camera is. Under each layer its bottom colour fills the screen.

Space behind the ship, drawn by the client only: nothing of it reaches the sim and it costs the sim
nothing. Numbers from `game/Content/Backdrop.cs`. Everything else in `render/` is described where it is
used (the features' READMEs) and in its own `///` summaries.

- **Three planes, far to near,** on the stars' canvas layer (`ScreenLayers.Stars`), under the whole
  world and every panel, so they never cover the hero, the ship, debris or the UI; the ship's back walls
  hide them. None takes a material of `Lit`: no lamp, fire or star lights them.
  - *Stars* (`Stars.cs`): a filtered 512 px tile of stars repeated over the screen, a screen pixel a star
    whatever the zoom, following 0.08 of the camera and streaming 8 screen px a second on local time.
  - *Far wrecks* (`FarWrecks.cs`, parallax 0.3): each a core of one of the wrecks' pictures, the asteroids' and derelicts' among them, with
    up to five pieces of scrap, hull, crates and pods gathered round its rim, from `art/debris` — the same
    wrecks, far away. Made once at start into one grey atlas with mipmaps: each pixel's lightness lifted
    to at least `WreckLift`, so the outlines soften, then drawn in a colour between space's own clear colour
    and `WreckTint`, the bigger nearer it. Drawn opaque and filtered down their mipmaps: soft, dark,
    never outlined; pieces that overlap do not darken, and the stars behind are hidden. About one a tile
    (560 px of the plane): two or three on screen at zoom 1, often none at 2.5.
  - *Dust* (`Dust.cs`, parallax 0.6): faint specks one or two screen pixels wide at every zoom, a dot in
    a clear border drawn filtered, so it glides by fractions of a pixel as the stars do; about eight a
    256 px tile. It drifts faster than the wrecks, slower than the debris, so space has depth and the
    ship moves through it.
- **A plane** (`DriftLayer.cs`): a point of it at `(x, y)` is on screen at
  `screen/2 + ((x, y) − centre) × zoom`, its centre the camera's centre times its parallax plus its drift
  (`Scatter.Centre`). So it moves its parallax of the world's movement at every zoom and zooms about the
  screen's middle with the world, without reshuffling. Past the player's farthest zoom (1), as only a dev
  view such as `--perf` goes, it is drawn as at 1.
- **Placed by a hash** (`Scatter.cs`): each tile of a plane holds `Mean` things, rounded down or up by a
  hash of the plane's seed and the tile; each thing's place and looks are drawn from its own hash. Nothing
  else decides it, so panning away and back shows the same things (`ScatterTests`). The wrecks and dust
  drift by the world's time (`Stars.Seconds`, ticks and the progress between them, set by `Main`), so
  every peer looking at the same place sees the same; the stars keep their local stream.
- **Cost:** a plane is drawn once into its canvas item, in its own pixels from the corner of the tiles
  drawn, and carried by the node's position and scale; it draws again only when the screen reaches past
  the tiles drawn (a tile of margin each way) or the zoom changes. A frame costs about a microsecond of
  CPU; a redraw 0.4–1.1 ms (dust at zoom 1 is most of it), a few times a minute while drifting or
  flying, and the quads are a few hundred in two batches.
- **Not done:** wrecks do not turn, so a plane never redraws for them; they all lie at one depth
  (their size and colour suggest more); no larger wreck art of its own; the title screen's space is its
  own (`menu/TitleScreen.cs`).
