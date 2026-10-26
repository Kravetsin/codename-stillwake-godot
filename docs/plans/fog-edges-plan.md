# Plan: Fog edges on planets

## Why

A planet ends at a bedrock wall, on the surface and underground, and the camera stops at the grid's edge. The design
([fog-edges.md](fog-edges.md)) hides each side in a band of fog open at every height: the hero glides in weightless at its
own height, is moved unseen at the band's seam to the other side's fog, and drops out of it from over the ground, as if
the planet wrapped. The sim stays a finite grid; the glide and the move are the client's, as all hero movement is (0002).

**Order and PRs.** F-1 is merged. F-1b, a small sim PR on `feat/fog-hollow` (this plan's revision rides in it), hollows
the bands. F-2, on `feat/fog-move`, is the client: the glide, the move, the fog, the sound, a dev demo, the ADR. Each
merges before the next starts.

**Words.**
- A *band* is the `Fog` columns at one side of a planet, outside its build zone (`IGridQueries.Zone`), the full-height
  bedrock columns (`Ground.BedrockSide`) at the grid's edge among them. Only a planet (`Rules is PlanetType`) has
  bands; the ship's zone (`Layout.WorldZone`) and test maps' zones make none.
- *In the zone*: for a cell, its column in `Zone.Left..Zone.Right`; for a hero, the column of its body centre,
  `floor(X / Unit)`. `Fog` is `Zone.Left`, and a band's zone is symmetric (`Width - 1 - Zone.Right == Zone.Left`).
  A band's *zone side* is the zone's column next to it: `Fog` on the left, `Width - 1 - Fog` on the right.
- The two *seams* are vertical lines in thousandths of a cell (`Hero.Unit`, the unit of a hero's X): the left at
  `Fog / 2 * Unit`, the right at `(Width - Fog / 2) * Unit`. The *period* is their distance, `(Width - Fog) * Unit`; a
  shift by it puts a point on one seam exactly on the other.
- *Body centre*: the hero's X, the middle of its body. *Feet*: the hero's Y. *View centre*: the point at the screen's
  middle (`CameraFollow.X`). In `HeroView`, `_previous` and `_current` are the hero before and after the last step,
  and `_snap` makes the camera look straight at it next frame.
- A band column's *ground line* is the first row of it with a back wall: sky above it, back-walled open air from it
  down. It is grown from the seed alone (nothing is built or dug in a band), so it is the same before and after any
  play. The *core* is the bottom `CoreRows` rows (lava over bedrock).

## Tasks

### F-1 — Bands of bedrock outside the build zone, creatures kept out ✅

Merged (#225): `Ground.Fog` (64, 8); `Terrain` keeps landmarks, ruins and growth out of the bands, opens no cave mouth
within a band's width of one, seals each band solid bedrock under the sky, and sets the zone between them;
`CreaturesModule.Solid` makes every column outside a planet's zone solid to creatures; `FogBandsTests` and three
`CreaturesTests`, each seen failing once with its rule broken.

### F-1b — The bands hollow

- **Content:** `Ground.FogDrop`, rows over a band's ground line beside the zone a moved hero's feet arrive at: 12 for
  `First`, 6 for `Small`. A content test: at least the hero's height in rows (`Content.Hero.Height` in rows).
- **Terrain** (`sim/Core/Terrain.cs`):
  - `Seal` makes each band column, between the full-height bedrock columns, sky above its `top[x]` (`kind` sky,
    `codes` 0: no back wall) and open air from it to the core (`kind` open below the surface, `codes` bedrock: a
    bedrock back wall, natural, so the rooms count it outside), its `hall` flags cleared; the core's rows are left
    as grown.
  - `CaveFlora` grows nothing whose block or the cells it looks at reach a band: its columns run from `Fog + 1` to
    `Width - Fog - 3` instead of from `BedrockSide`, so no rock growth faces the hollow from the zone side.
  - Landmarks' margins are not changed: moving them would move every chasm and hall of every seed under the saves
    made since F-1. A chasm near a band is crossed as anywhere.
- **Nothing else changes in the sim.** A hit whose fan reaches no breakable cell (in a band its cells are open and its
  back wall bedrock) is refused with `building.nothingToDig`; a fan from a band can still reach the zone's ground
  within its reach, as from anywhere. Plans are refused outside the zone. Creatures find outside the zone solid.
- **Tests** (`FogBandsTests`, over both types and three seeds):
  - Each band column is sky with no back wall above `top[x]`, open with a bedrock back wall from it to the core; no
    growth, ruin, landmark, vein or hall in a band; no growth within one column of a band. Replaces F-1's bedrock check.
  - At each band, the ground line of its column beside the zone less `FogDrop` leaves at least the hero's height of
    rows above it, and lies within a row of the zone side's ground top.
  - A hit from a band cell on its ground line aimed outward, away from the zone, is refused with
    `building.nothingToDig`; a plan there with `grid.outsideZone`.
  - FsCheck, kept: no command changes a band's cell; hits aimed along the ground line, plans anywhere in a band. Proven
    to bite by letting plans through once (the zone widened in the test's own layout).
  - The zone kept across a load, no cold anywhere, `Fog` even and narrow: kept from F-1. F-1's walk-to-the-middle check
    is dropped: the band has no ground to walk.
- **Docs:** this plan and the design, revised. No README holds the terrain rule (`sim/Core` has none); the ADR in F-2.
- **Done when:** the suite and the docs guard are green; a frame of the left band shows the hollow: sky over the ground
  line, dark back wall below, the zone's ground ending at its side (`tools/snap.ps1 … -- --ground=shaft
  --ground-offset -2017`: `--ground-offset` is columns right of the mode's spot, here the first planet's spawn, so the
  hero stands at column 32, the left seam's).

### F-2 — The glide, the move, the fog, the sound

- **The band, from one source.** `FogEdges.Of(grid, rules)` (Godot-free, `game/features/crew/logic/FogEdges.cs`) gives
  `Fog`, the seams, the zone sides, each band column's ground line (read from the grid's back layer once) and `FogDrop`
  on a planet with a symmetric zone narrower than the grid, else none. The ship, wrecks, test maps and planets saved
  before F-1 (whose saved zone is the whole grid) get none: no glide, no fog, no move. `Ship.Of` carries it, as it
  carries the surface's gravity.
- **The glide** (`game/features/crew/logic/Hero.cs`): a branch of `Hero.Step` taken while the hero is out of the
  zone: no gravity; `Vy` is 0 and Y does not change; Up, Down, jump, ladders and the jetpack do nothing; X is paced by
  left and right as on the ground (`Walk`, `Run`), stopped by a wall as a walk is. Entering it lets go of the harpoon,
  so no hook pulls a hero that cannot rise or sink. It is drawn in the pose a hero floating in space takes (reused;
  nothing on a planet floats, `Rooms.Afloat` stays false). A passed-out hero glides too (no input), so it never falls
  into a band. Leaving the band, the next step is the usual one: on the ground it walks on, in the air it falls.
- **The move** (`FogEdges.Shift` and `FogEdges.Arrive`, called from `HeroView.Frame` inside the step loop after each
  `Step`):
  - `Shift(fromX, toX)` gives `+period` when a step takes the body centre from at or right of the left seam to left
    of it, `-period` when from at or left of the right seam to right of it, else 0. After a `+period` shift the hero is
    just short of the right seam on its zone side, still heading left, toward the zone; after `-period` likewise at
    the left seam.
  - `Arrive` sets the feet to `max(groundLine(inner) - FogDrop, heroRows) * Unit`, where `inner` is the landing band's
    column beside the zone (`Fog - 1` on the left, `Width - Fog` on the right): grown from the seed, never moved by
    play, and within a row of the zone side's natural ground (the surface is a stair, 0106), so the hero glides out
    over it; X and `Vx` kept, `Vy` 0.
  - A seated hero (`Seating.SeatOf`) is never shifted, arrived or snapped. Any other, on a shift: `_previous` and
    `_current` take the new X and feet, the harpoon is let go, `_snap` is set (`CameraFollow.Snap`), and `_reportedAt`
    is cleared so the next `Report` sends the new cell this frame (else both seams being outside, it would wait up to
    `ReportEvery`). A frame of several steps is checked step by step; a frame of none checks nothing. Other peers snap
    a hero that jumps more than 6 cells (`Remote.Snap`).
- **The fog** (a new feature, `game/features/fog/`, with its README and Files table):
  - `FogView`, a `CanvasLayer` at a new `ScreenLayers.Fog = 1` in `game/render/Layers.cs` (Frost moves from 1 to 2,
    Hud from 2 to 3; Bars, 10, and above unchanged), one full-screen `ColorRect` with an unshaded shader. Made by
    `SurfaceViews` when `FogEdges.Of` gives a band.
  - `logic/FogShare.cs` (Godot-free), positions in cells as doubles (a hero's X over `Unit`):
    - `Column(x)`: 0 from `FogLooks.Lead` columns inside the zone inward; from there a smoothstep to `FogLooks.Edge`
      at the zone side's outer edge; from there a smoothstep to 1 at the seam; 1 outward and past the grid's edge.
    - `Screen(viewX)`: 0 while the view centre is in the zone, a smoothstep to 1 at the seam, 1 past it.
    - `Flash`: when the view centre moves more than half the grid's width between two frames and `Screen` is above 0
      before or after the jump (a move; not a beam-down, a wake at the spawn, a load), it holds 1 for
      `FogLooks.FlashMs`, then eases to `Screen(viewX)` over `FogLooks.FadeMs`. The first frame never flashes.
  - The shader takes the view's world rect each frame (uniforms from the viewport's canvas transform), the screen
    share and the band; each pixel's fog is `1 - (1 - Column(x)) * (1 - screen)`, the fog colour laid over at that alpha.
  - Colour per planet type: `PlanetLooks.Look.Fog`; `FogLooks` (`game/Content/logic`) holds `Lead`, `Edge`, `FlashMs`,
    `FadeMs`.
- **Sound** (`game/features/sound`): `Hearing.At(floating, fogged)`, both shares 0 to 1: the World bus's cutoff is
  `Cutoff(ClearHz, MuffledHz, max(floating, fogged))`; its gain and the Suit bus follow `floating` alone, which stays
  0 on a planet. `SoundView` passes `fogged = FogShare.Column(bodyX)` at the own hero's body centre, eased over
  `MuffleMs` as `floating` is.
- **Dev:** `--ground=fog` in `GroundDemo`: beam down at column `Fog + 20` on the ground, plan a wall and swing the axe
  at a band cell (both refused, for the recording), autopilot walking left into the left band, gliding to the seam,
  dropping out of the right band, landing, then walking back right across the right seam. `--ground=fog-cave` the same
  from a tunnel dug at `Ground.CaveFrom` rows under the ground to the left band's zone side. A two-peer recording runs
  two instances on this machine, `--host` and `--join` (ENet, 127.0.0.1).
- **Tests** (`game.tests`):
  - Glide: out of the zone Y and `Vy` stay put whatever the input, X paces as on the ground, the harpoon is let go on
    entering; in the zone the same input walks, falls or jumps as before; a passed-out hero glides.
  - `FogEdges`: crossing each seam outward shifts by exactly the period and sets the feet `FogDrop` rows over the
    landing column's ground line; standing on a seam or turning back before it does not shift; cross left, then turn
    at once and cross back right, and the hero is at the left seam's column again at the drop height; a seated hero is
    never shifted; a shift lets go of the harpoon and sends a report that frame; a ship world, a test map with a zone
    and a planet whose zone is the whole grid give no band.
  - Arriving, the hero glides out of the band and, back in the zone, falls to the ground and stands.
  - `FogShare`: `Column` and `Screen` 0 inside, 1 at a seam and past it, monotonic between; a jump of over half the
    width with `Screen` above 0 holds 1 for `FlashMs` and then eases, one with `Screen` 0 on both sides never
    flashes; the first frame never flashes.
  - `Hearing.At`: `fogged` dulls the World bus's cutoff only.
- **Docs:** ADR `docs/decisions/2026-10-26-fog-edges.md` (the hollow bands, the zone as their source, the client's
  glide and move, what is not handled, saves: planets are reached from the dev panel only until the drop pod's launch
  slice, and a later change of `Fog` leaves saved planets with their old zone); `tools/adr-index.ps1`; `docs/map.md`
  line for the fog feature; `game/features/crew/README.md` and `game/features/sound/README.md` a line each. This plan
  and the design are deleted in this PR.
- **Done when:** the suite, the docs guard and `dotnet format --verify-no-changes` are green; recordings of
  `--ground=fog --autopilot` and `--ground=fog-cave --autopilot` (`tools/snap.ps1 … -Movie 12`) show the refused plan
  and hit with their text (the prompt bar and tips sit above the fog), the walk in, the glide, the screen gone to fog,
  the drop out of the other band and the landing, the camera steady and no pop; a two-peer recording shows the friend
  fade into one band and out of the other.

## Not handled, on purpose

- Creatures, stacks or thrown items crossing; markers or positional sound across the seam; the camera's lead taking
  the short way across it.
- Air: the suit breathes in the fog as on the planet.
- In by a tunnel, out from the sky: the far band is always left at the drop height. Turning back just past a seam
  crosses again and leaves at the drop height over the first band, not into the tunnel.
- A wall built at the zone side across the drop height stops a hero gliding out: it breaks it from the band (a fan
  reaches it) or turns back.
- A hero in a band is out of reach of walkers: a safe pocket, accepted.
- A hero who crosses mid-action (E held, drilling, a hit in flight): the sim reads it against the old cell until the
  next `Enter`, sent that frame; it is refused or lands on the side left.
- A stack dropped or thrown into a band falls to the core and is lost until it expires. A hero who passes out in a
  band (its suit run dry while gliding) leaves its body's stacks there to fall the same way: its whole carried load,
  rare, accepted.
- Other peers draw a gliding friend in whatever pose its samples give; the fog lies over it.
- A loaded or joining hero stands at the spawn (`HeroView`), never in a band.
- Planets saved before F-1 keep their full-grid zone: no glide and no fog, and a hero walking off their ground into a
  hollow band falls to the core. Planets saved since F-1 have the narrow zone and get the glide and fog; their bands,
  never edited, regrow hollow. Regenerate dev planets made before F-1.
