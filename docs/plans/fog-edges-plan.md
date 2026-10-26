# Plan: Fog edges on planets

## Why

A planet ends at a bedrock wall and the camera stops at the grid's edge. The design ([fog-edges.md](fog-edges.md)) hides each
side in a band of fog: the hero walks in, is moved unseen at the band's seam to the other side's fog, and walks out, as if
the planet wrapped. The sim stays a finite grid; the move is the client's, as all hero movement is (0002).

**Order and PRs.** Two tasks, F-1 on `feat/fog-bands`, F-2 on `feat/fog-move`, F-1 merged before F-2 starts. F-1 is sim
and content only: bands that cannot be dug or built in and that creatures keep out of, no fog drawn yet. F-2 is the
client: the move, the fog, the sound, a dev demo, the ADR.

**Words.**
- A *band* is the `Fog` columns at one side of a planet, outside its build zone (`IGridQueries.Zone`). Only a planet
  (`Rules is PlanetType`) has bands; the ship's zone (`Layout.WorldZone`) and test maps' zones make none.
- *Inside* means the zone's columns, `Zone.Left` to `Zone.Right` inclusive; `Fog` is `Zone.Left`, and a band's zone is
  symmetric (`Width - 1 - Zone.Right == Zone.Left`).
- The two *seams* are vertical lines in thousandths of a cell (`Hero.Unit`, the unit of a hero's X): the left at
  `Fog / 2 * Unit`, the right at `(Width - Fog / 2) * Unit`. The *period* is their distance, `(Width - Fog) * Unit`; a
  shift by it puts a point on one seam exactly on the other.
- *Body centre*: the hero's X, the middle of its body. *View centre*: the point at the screen's middle (`CameraFollow.X`).
- *Ground top* of a column: its first solid row from the top. The *core* is the bottom `CoreRows` rows (lava over bedrock).

**One change from the design.** The fog is a screen layer like `Frost` and lies over everything in the world, the
player's own hero included — the user's "туман поверх всего". Keeping the own hero over it would need a free z between
`Layers.OtherHeroes` (13) and `Layers.Hero` (14). The design's "Decided" section says so.

## Tasks

### F-1 — Bands of bedrock outside the build zone, creatures kept out ✅

**Built:** as below, and one more rule found on the way: no cave mouth opens within a band's width of a band, so the walk
into the fog meets no pit (`Terrain`'s columns). The spawn test runs on a wide open map with a zone (`CreaturesTests`), as a
small planet has no ground out of a hero's view to spawn on. Each new test was seen failing once with its rule broken.

- **Content:** `Ground.Fog` (columns per band, `required`), 64 for `PlanetTypes.First`, 8 for `Small`. A content test:
  `Fog` is even, at least `BedrockSide + 2`, and `2 * Fog` under a quarter of `Width`.
- **Terrain** (`sim/Core/Terrain.cs`, `Generate`):
  - `Landmarks`' `Column(...)` margins (chasms, halls), `Spires` and `Homes` start from `Fog + 1` and end at
    `Width - Fog - 2` inclusive instead of from `BedrockSide`, so no landmark or ruin reaches a band. Counts unchanged.
  - After the mouths' fade and before `PaintVeins`, a new pass `Seal`: in every band column each row from its ground
    top down to the core is bedrock (`kind` solid, `codes` bedrock), every row above it sky (`kind` open sky, `codes` 0,
    so the grid gives it no back wall), and its `hall` flags are cleared. Veins then find no stone there, `Flora` no
    soil, `CaveFlora` no open cell or stone.
  - The layout's `Zone` is `new Box(Fog, 0, Width - 1 - Fog, Height - 1)`.
  - The ground changes near the sides and where ruins and landmarks land: tests pinning their positions or a planet's
    hash are updated in the same commit, each named in the PR.
- **Creatures** (`sim/Creatures`): on a planet, `CreaturesModule.Solid` (`Live.cs`) answers solid for every column
  outside the zone. Walking, leaps, hops, bolts, a hero's knockback and a release into a band all collide there, as at
  the grid's edge, and a spawn whose body reaches a band is refused. `Spawn` keeps its three draws before any refusal,
  so the world's `Rng` runs as before. Existing creature scenarios on planet maps are run; any whose result moves is
  named in the PR.
- **No other module changes.** Grid already refuses plans outside the zone (`grid.outsideZone`); `Hew` strikes nothing
  in bedrock (`building.nothingToDig`). No command, query, event or saved field; no `SaveFormat` bump.
- **Tests** (scenarios on `Small`; terrain over both types and three seeds):
  - Every band cell is sky above its column's ground top and bedrock from it to the core; no plant, vein, ruin,
    landmark or hall cell lies in a band.
  - The walk to a seam: every column from the zone's side to the band's middle has its ground top within one row of
    its neighbour's, so a hero walks there without a pit or a wall.
  - `Zone` is the columns between the bands, and is the same after a save and a load.
  - A hew into band ground is refused with `building.nothingToDig`; a wall planned on a band cell with `grid.outsideZone`.
  - A walker driven to a side turns at the zone's side; a creature knocked toward a band stops at its side; a hundred
    spawn draws place nothing in a band.
  - `Crew.EdgeCost` is 0 on every cell of both planets.
  - FsCheck: fuzzed commands never change a cell outside the zone (proven to bite by letting one wall through once).
- **Docs:** `sim/Creatures/README.md`, one rule line (on a planet, outside the zone is solid to creatures).
- **Done when:** the suite and the docs guard are green; a frame of the left band (`tools/snap.ps1 … -- --ground=sprint
  --ground-offset -2035`, which stands the hero 14 columns in from the left edge) shows bedrock ground, no ruin.

### F-2 — The move, the fog, the sound

- **The band, from one source.** The client reads bands from the grid: `FogEdges.Of(grid, rules)` gives `Fog` and the
  seams on a planet with a symmetric zone narrower than the grid, else none. The ship, wrecks, test maps and old planet
  saves (whose saved zone is the whole grid) get none: no fog, no move.
- **The move** (`game/features/crew`):
  - Godot-free `logic/FogEdges.cs`: `Shift(fromX, toX)` gives `+period` when a step takes the body centre from at or
    right of the left seam to left of it, `-period` when from at or left of the right seam to right of it, else 0. A
    hero standing on a seam, or turning back before it, is not moved.
  - `Land`: the feet keep their height over the ground. `dy = (topAfter - topBefore) * Unit`, where each top is the
    highest ground top under the body's columns, before and after the shift (from `Ship`'s solid query); Y += dy.
    X and Y speed and facing are kept.
  - `HeroView.Frame`, inside the step loop after each `Step`: if `Shift` is not 0 and the hero is neither seated
    (`Seating.SeatOf`) nor passed out, both `_previous` and `_current` move by it and by `dy`, the harpoon is let go
    (`_harpoon = new HarpoonShot()`), and `_snap` is set so the camera snaps (`CameraFollow.Snap`). A frame of several
    steps (after a hitch) is checked step by step; a frame of none checks nothing. The next `Report` sends the new
    cell (`Enter` takes any cell). Other peers already snap a hero that jumps more than 6 cells (`Remote.Snap`).
- **The fog** (a new feature, `game/features/fog/`, with its README and Files table):
  - `FogView`, a `CanvasLayer` at a new `ScreenLayers.Fog = 1` (Frost moves to 2, Hud to 3; Bars and above unchanged),
    one full-screen `ColorRect` and a shader, unshaded. Made by `SurfaceViews` when `FogEdges.Of` gives a band.
  - The shader takes the view's world rect each frame (uniforms from the viewport's canvas transform) and a 1D R16
    texture of the `2 * Fog` band columns' ground tops: the left band's columns from 0, then the right's from
    `Width - Fog`, made once from the grid at `_Ready` (band ground never changes: nothing digs or builds there).
  - *Column fog*, per screen column: 0 at the zone's side, rising to 1 at the seam, 1 outward and past the grid's
    edge. It covers rows down to the column's ground top and fades out over `FogLooks.Under` rows below that top.
  - *Screen fog*, over the whole screen, from `logic/FogShare.cs`: `Screen(viewX)` is 0 while the view centre is on a
    band's inner half and rises to 1 at its seam. When the view centre moves more than half the grid's width between
    two frames (the move's snap), it holds 1 for `FogLooks.FlashMs`, then eases to `Screen(viewX)` over
    `FogLooks.FadeMs`. The first frame after the view is made never flashes. The fog feature learns of the move only
    from the camera, never from the crew feature.
  - Colour per planet type: `PlanetLooks.Look.Fog`; `FogLooks` (`game/Content/logic`) holds `FlashMs`, `FadeMs`, `Under`.
- **Sound** (`game/features/sound`): `Hearing.At(floating, fogged)`: the World bus's cutoff is
  `Cutoff(ClearHz, MuffledHz, max(floating, fogged))`; its gain and the Suit bus follow `floating` alone. `SoundView`
  passes `fogged = FogShare.Column(bodyX)`, the column fog at the own hero's body centre, eased over `MuffleMs` as
  `floating` is.
- **Dev:** `--ground=fog` in `GroundDemo`: beam down at column `Fog + 20`, autopilot walking left through the left band
  to the right one, then turning and walking right back across the right seam to the left band.
- **Tests** (`game.tests`):
  - `FogEdges`: crossing each seam outward shifts by exactly the period; standing on a seam, turning back before it,
    or a jump falling back short does not; arrival lies just past the other seam on its outer side; cross left, turn
    at once and cross back right, and the hero is where it started; speed kept; the feet's height over the ground
    kept; a ship world, a test map with a zone and a planet whose zone is the whole grid give no band.
  - `FogShare`: 0 inside the zone and on a band's inner half, 1 at a seam and past it, monotonic between; a jump of
    over half the width holds 1 for `FlashMs` and then eases; the first frame never flashes.
  - `Hearing.At`: `fogged` dulls the World bus's cutoff only.
- **Docs:** ADR `docs/decisions/2026-10-26-fog-edges.md` (the bands, the zone as their source, the client's move,
  what is not handled, saves: planets are reached from the dev panel only until the drop pod's launch slice, and a
  later change of `Fog` leaves saved planets with their old zone); `tools/adr-index.ps1`; `docs/map.md` line for the
  fog feature; `game/features/crew/README.md` and `game/features/sound/README.md` a line each. This plan and the design
  are deleted in this PR.
- **Done when:** the suite, the docs guard and `dotnet format --verify-no-changes` are green; a recording of
  `--ground=fog --autopilot` (`tools/snap.ps1 … -Movie 12`) shows the walk in, the screen gone to fog, the walk out of
  the other band and back, the camera steady and no pop; in it a plan and a hit in a band show their refusal text
  (the prompt bar and tips sit above the fog); a second recording with a friend in a local two-peer session (host and
  guest on this machine) shows the friend fade into one band and out of the other.

## Not handled, on purpose

- Creatures, stacks or thrown items crossing; markers or positional sound across the seam; the camera's lead taking
  the short way across it.
- A hero in a band is out of reach of walkers: a safe pocket, accepted.
- A hero who crosses mid-action (E held, drilling, a hit in flight): the sim reads it against the old cell until the
  next `Enter`; it is refused or lands on the side left.
- A stack dropped past a seam cannot be reached and is lost until it expires.
- Loading with a hero past a seam: walking outward meets the bedrock wall at the grid's edge, as today.
- Old planet saves: no save step. Their band columns regrow as bedrock under their edits; their saved zone is the whole
  grid, so they get no fog and no move. Regenerate dev planets.
