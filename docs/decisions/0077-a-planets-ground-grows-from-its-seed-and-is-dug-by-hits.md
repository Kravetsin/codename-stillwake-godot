# 0077 — A planet's ground grows from its seed and is dug by hits

- Status: accepted; slices A–C built (D, art and sound, later)
- Date: 2026-10-20

## Context

A planet (0076) was a hand-drawn strip of hull walls: nothing to explore, nothing to dig, nothing to bring home. Sorties
need ground worth going down into: hills of soil over stone, caves that get richer deeper, and a way to break it with
the tools heroes already carry, since tools are weapons and damage works alike on everything (0054). Caves are dark, so
digging and a light to dig by come together.

## Decision

- **Ground is a pure function of the kind and the seed.** A kind of surface may name a `Ground` (`Content.Ground`)
  instead of rows; `Terrain.Generate(ground, seed)` (core) fills a `Layout` directly with the legend's characters (`#`,
  `/`, `\`, `.`, space, `S`), which the modules' `Init` already read, and one new per-cell layer, `Layout.Materials`,
  the material codes characters cannot express: Grid's `Init` builds walls and back walls of them (0 is open sky, with
  no back wall). The world opens the surface with its seed drawn from the world's Rng as before. Test maps draw a material map beside
  their rows (`Layout.WithMaterials`, `Legend.Materials`: `s` soil, `t` stone, `b` bedrock).
- **Integer noise only.** Value noise over an unchecked `uint` hash of the seed, the noise's id and the lattice point,
  eased by an integer smoothstep and mixed with non-negative weights, rounded down. No floats, no banned math. It is worked
  out a row at a time (each 2D noise's lattice blended down once a row, then along it), so 2048 × 768 cells grow in some
  50 ms (Release; 80 Debug).
- **What it grows** (every number in `Content.Ground`, to tune by eye):
  - *Surface:* three octaves of 1D noise round `Surface`: ranges and valleys ±44 rows over 384 columns, terraced in steps
    of 5 rows so their sides are cliffs; hills ±12 over 64; bumps ±2 over 12.
  - *Soil* 3–8 rows deep (its own noise) over *stone* to the bedrock.
  - *Caves* from `CaveFrom` (12) rows under a column's surface: winding tunnels where a 2D noise, stretched to run more
    along than down, is near its middle, and caverns where another passes a mark. The tunnels widen and the caverns'
    mark falls with depth, so the bottom third is more open than the top third on every seed tried. Where a 1D *mouth* noise
    passes its mark, caves begin nearer the surface and open as if deeper: a few break through to the sky.
  - *Landmarks* at places the seed chooses, each in a band of its own so they spread, none within `Clear` (120) columns
    of the middle: 4 *chasms*, clefts up to 12 cells across cut from the surface 130–260 rows down, wandering and
    narrowing; 6 *great halls*, caverns up to 72 × 28 cells either side of their middle, their walls ragged by noise,
    from 80 rows under the surface to the bedrock. Walking across takes minutes; there is something to find.
  - *Bedrock:* the bottom 3 rows and a 1-wide column at each side, the whole height.
  - *Slopes:* a soil or stone cell open above with one side open and the other solid is cut to ◢ or ◣ (0069), rising
    toward the solid side; the surface and cave floors alike.
  - *Back walls* of each cell's own material from the surface down, caves included, so the sky's light falls off into
    them (0070) and a cave is dark a few cells in.
  - *Spawn:* the flat column nearest the middle, on whole ground, open above.
- **The first planet** (`Surfaces.Planet`, `--planet` and the dev panel) is **2048 × 768** cells under the strip's rules,
  the ship's world's size class, its surface some 160 rows down: a walk across is over four minutes. A smaller one
  (384 × 192) felt like a sandbox: finding things must take real searching.
  `Surfaces.SmallPlanet` is the same ground at 64 × 48, which the world's fuzz opens beside the strip. The strip stays,
  for tests and `--strip`.
- **A grown surface saves its seed and its edits** (Grid): a dense ground saved whole as runs was some 870 KB a planet,
  in every save, hash and welcome. A layout knows it was grown (`Layout.Grown`); Grid notes what each cell held as grown on
  its first write (derived, never saved) and saves, in place of its layers, its size, zone, machines and `edits`: the
  cells that differ now from the ground as grown, ascending, each `cell, structure, material, conduit, back,
  backMaterial, shape`. A cell changed and changed back is no edit. Loading, the world grows the ground again from the
  surface's kind and seed (`World.Load`, handed to each module's `Load` as `ground`) and Grid lays the edits on it. The
  ship and the strip save as ever. Hooks: `Module<TState>.Save()` and `Load(saved, options, ground)`.
- **Caves are outside, never rooms.** On grown ground an open cell under the sky (no back wall) or before natural ground
  (a natural back wall) is outside wherever it is: natural ground holds no air. So the cave network, one room spanning
  much of the planet, is space, and a dig costs what is round it, not a flood of the network (it cost 100 ms a dig).
  A room on a planet is one built under the sky with back walls of hull; on a planet no vent may be built anyway.
- **Natural materials** are `Materials` flagged `Natural`, leakier than any hull: soil (strength 4, built and yielding
  `soil` 1), stone (strength 12, `rock` 1), bedrock (unbreakable, never built). `Strength`, unused till now, is a natural
  wall's hit points. Walls of soil and stone are blueprints like any wall, known once soil or rock is analysed (0027),
  allowed on planets (`wall` is in their build list) and aboard as leaky decor; bedrock and natural back walls are no
  blueprint, refused `building.unknown` even in creative mode, since nothing would take one down. `rock` is "Rock" to
  players: one item for asteroid ore and stone.
- **Digging is `Dig(Cell, Tool)` in Building,** which owns plans and taking things apart, and now `Cracks`: a saved list of
  (cell, damage) sorted by cell. A dig adds its weapon's damage (axe 2, drill 1, arc drill 3 a hit; 4, 10 and 15 a
  second at their paces); reaching the wall's strength clears the wall, and only the wall — its back wall stays — and
  gives what it is made of (its `Cost`) to the sender, what does not fit falling at the cell (`GiveOrDrop`). Refusals:
  `building.nothingToDig` (no natural wall: open ground, hull, off the grid), `building.bedrock` ("Too hard"), then
  what the weapon says, the same for debris and ground through one Items query (`WeaponProblem`): `items.flat`, and as the
  command applies `items.notRunning` (0055). Debris' own `debris.flat` and `debris.notRunning` are gone into it. A crack
  whose wall is gone (a meteor) is swept in derivation, as marks are.
- **A mark on natural ground,** wall or back wall, is refused `building.natural`: a mark would take stone apart in half
  a second of work.
- **The client digs at the pointer's cell, in reach,** as plans are reached (`Aiming.Reaches`): with the axe or a drill
  held and natural ground under the pointer, the aim says "[Hold LMB] Dig" (or why not) and outlines the cell; the
  axe's strike sends one `Dig` at the cell of its swing's point, the drill one at its pace (`DrillPace`), whose works
  flag, sparks and sound now fire on ground too. Nothing traces a blade or a bit, which only work in open space: a
  shaft, a cave or the shelter dig alike. A press of E digs nothing.
- **Seen:** `DigView` (building) draws each crack in three stages by its share of the strength, chips in the material's
  colour fly from each hit and fall, dust and a slow cloud on a break; a friend's digs show alike, read from `Cracks`.
  The ground's pictures are made in code (`GroundArt`, colours in `GroundLooks`) till slice D's audition.
- **The helmet lamp** (`helmetLamp`): worn in the head slot (0047), crafted at the lifepod of a plate, two wire and a
  silicon; free, drawing no power, so a hero lost underground is never left in the dark. Worn, a soft warm light
  shines from a bulb on the helmet's brow (`Light.HeadLamp`, 6 cells), a lamp source of the lightmap following the
  head, for this hero and every friend shown on the surface. It works everywhere, aboard too.
- **Save format 21:** Building's `cracks`, empty in every older save, the ship's and every surface's; a grown surface's grid
  as its edits (no older save holds one).

## Measured

On this machine, Debug build (`BudgetTests.GrowsAPlanetQuicklyTicksItIdleCheaplyAndSavesItSmall`):

| What | Cost |
| ---- | ---- |
| growing the 384 × 192 planet | 10 ms mean (budget 15) |
| the tick it opens on, every module created and rebuilt | 30 ms, once a planet |
| an idle tick with a hero on it | 0.01–0.02 ms |
| a tick digging a shaft, a hit a tick | 0.06 ms mean; 11 ms the tick a shaft opens into a big cave (rooms found again) |
| what the planet adds to a save | 46 KB |

## Deliberately not handled

- **Ice, copper and other ores:** each comes with its first use. Biomes, plants, ruins, closing a planet, chunking it.
- **Digging back walls,** reshaping slopes after a dig, ceiling slopes, falling blocks, liquids, fall damage.
- **Hitting hull with weapons:** hull is taken apart with marks; natural ground is not marked.
- **Healing half-dug cells:** a crack stays until its wall breaks; the list grows by a few entries.
- **Digging more than a cell a hit,** and showing one's own dig before the sim confirms it (no prediction, 0002): a dig
  shows a round trip late.
- **Reach, pace and which cell is hit** are the client's word (principle 6).
- **Deep shafts:** a hero jumps six cells on the planet; deeper ones need planned ladders.
- **A planet's first tick** stalls by its generation and every module's rebuild, some 350 ms (Debug), and the client's first
  view of it by a second, most of it drawing every cell of the grid; once a planet, to be hidden by the pod's descent.
  Drawing the grid near the camera only, in chunks, is the fix if it shows.
- **Air in caves:** caves are outside; an aired cave base waits for vents on planets and a way to replace natural back
  walls.
- **Meteors reading cracks:** the next user of `Strength`, in hazards.
- **The lamp drawing power, its art, the dig's sound and the ground's art:** slice D, through audition pages.
