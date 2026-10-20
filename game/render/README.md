# Render: the backdrop

A planet's sky (0079) is `PlanetSky.cs`, shown by `Stars` in place of space on a surface with daylight: the "Stillwake"
style's sky gradient, a pale gas giant, high cloud drifting, the sun added over it with its glint, then far mesas, a ridge
and hills with spindly trees (`art/planet`), each layer repeating across every 2048 pixels. A pixel of art is a pixel of
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
