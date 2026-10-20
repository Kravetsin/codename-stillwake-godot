# 0080 — Caves glow by depth: what grows in them is the surface's flora, held by floor, ceiling or rock

- Status: accepted
- Date: 2026-10-20

## Context

The first planet's caves (0077) were dark stone lit only by a helmet's beam, a lamp or something glowing. A concept page of
what fills them offered three directions: A, teal bioluminescence (glow moss, glowing mushrooms, alien threads); B, amber
geodes and crystals; C, light that changes with depth. The user picked C — cold blue near the surface, teal in the middle,
warm amber and red near the bottom — with A's glow moss and threads in the middle band ("the cave concepts are just
beautiful"). Glowing things give light (the rule the surface's plants follow, 0079), but caves are to stay mysterious and the
light budget is a lit room of lamps. Glowcaps, crystals, geodes and ore veins will give things later; nothing yet.

## Decision

- **Cave growth is flora (0079), grown by the generator, held by what it grows on.** `Content.Flora.Cave` lists its kinds,
  each with the anchor it holds on to (`Flora.Anchor`): a floor (glowcaps teal and violet, crystals ice, amber, rose and
  ember, stalagmites), a ceiling (roots, stalactites, threads teal and violet, hanging caps, hanging ice and amber crystals)
  or the rock beside a cave (glow moss, mica, ice and copper veins, fossils, a rare geode, the heat's cracks). `Terrain` sets
  them with the ground, after the surface's plants, into the same `Layout.Flora`: a pure function of the seed, so a save
  holds only what was felled since (`Felled`, by cell), as for plants, and a fresh planet saves none.
  - *Rejected: a kind of its own in the sim for cave decor.* What it needs is what plants needed — derived from the seed,
    felled by a hit or with its ground, a `Yields` hook — and Building already did it; only "its ground" generalises, to
    the cell it holds on to (`BuildingModule.HoldOf`).
- **Depth bands are strata:** rows in thousandths of the way from the surface's middle row to the bedrock
  (`Flora.Bands`: 250, 600, 850), so a band is level across the planet as rock layers are; `Terrain.BandOf` answers it for
  the sim and the client alike. Each kind has a chance in each band (`Chances`, in ten-thousandths of a slot): ice crystals,
  roots and green moss in the cold band; the garden's teal and violet caps, threads and moss in the middle; amber and rose
  crystals and copper in the deep; ember crystals and heat cracks over the bedrock.
- **Placement is a draw a slot,** in rows from the top: every open cave cell over a whole wall (a floor; never a slope, as
  Terraria grows nothing on one, and a growth whose hold is later cut to one goes as if dug), every open cell under a whole wall (a ceiling) and every stone wall beside a cave cell (the rock) draws one
  number under 10000 and walks its anchor's kinds in order, their chances scaled by a smooth patch noise (a swell every 12
  cells, nothing to 220 %), so they gather in clusters and patches, and in a great hall by 700 % (a wall facing one too): halls
  are gardens, tunnels sparse. A kind keeps `Apart` cells from its own along the row, a floor's or a ceiling's growth one cell
  from any other of its anchor, its `Tall` cells open and free; roots only within `Shallow` (24) rows of the surface, heat
  cracks only in floors. The rarest are listed first so a dense patch does not crowd them out, but mica last, so moss fills a
  hall's walls.
- **Felled as plants are:** a hit on an open cell a growth stands in fells it; one in the rock is never hit itself — a `Dig`
  digs its stone — and goes when the stone breaks. Building's sweep now looks only round the cells the grid's log names since
  it last looked (`ChangedSince`): with some thirty-eight thousand growths it would otherwise walk them all at every break.
- **The client draws by chunks** (`features/flora/CaveView`): 32 × 32 cells, two canvas items each — pictures lit, glows
  added — from one atlas of every cave picture packed at the first look, near the view only. The pictures are cut from the
  concept's own drawing code by an export, as the surface's were (0079).
- **Light:** each glowing growth is one small lamp of its kind's colour and reach (5 cells for a cap or a crystal), through
  the light feature's existing sources (`LightView.Caves`); at most 56, the nearest the view's middle.
- **Mica, veins, crystals and geodes glint** where a helmet's beam falls on them with nothing between; spores, drips and faint
  embers drift from what is in view (0075's toolkit), at most 48 alive.
- **The stone is tinted by band:** a multiplied quad a band over what is drawn under the walls' edges — cool blue-grey, as it
  is, warmer, red-brown — so the ground's tiles and the grid's drawing are untouched, and the growths and the light keep
  their colours.

## Measured

On this machine (RTX 4060 Ti), 1280 × 720, `--ground=hall --measure` (the first planet's lushest garden hall):

| What | Cost |
| ---- | ---- |
| the hall at zoom 1 / 2 | 804–827 / 924–925 fps against 868–875 / 965–980 with no cave growth drawn; render CPU 0.39 ms against 0.37 at zoom 1; 82 draws against 75 |
| the cave view's own work | 0.05–0.08 ms a frame, nothing allocated (`--frames --allocs`) |
| cave lights at zoom 1 / 2 | 56 / 32 (the cap is 56) |
| without the atlas | 772 draws, 630 fps at zoom 1 |
| growing the first planet (Release) | ~84 ms, ~55 without the caves' growth |
| the first planet's cave growth | 37 604 (seed 1): mica 12 391, moss 10 719, stalactites 2 699, … glowcaps 1 573, crystals 2 426, geodes 51 |
| a fresh planet's save | under 16 KB, no growth in it; a felled one adds its cell |

## Deliberately not handled

- **Nothing is given yet:** glowcaps (biomass, spores), crystals (shards), a geode (its crystals) and the veins (copper, ice)
  keep an empty `Yields` till those items exist; a vein is stone to dig.
- **Moss grows only on whole stone,** not on slopes or soil, and shows on whichever faces of its stone are open now.
- **A cave growth's other cells holding something** (a wall built into hanging threads) leave it standing, as a tree's crown.
- **Halls stay dark in their middle:** a hall is mostly air, its growth on its rim; lights past the 56 nearest the view's
  middle are left out.
- **The bands are rows, not depth under each column:** a cave under a high range is a little colder than one under a valley
  at its depth. Roots alone follow the surface.
- **No growth spreads, regrows or moves;** drips fade as they fall; glowcaps do not pulse.
- **Growing the planet costs some 30 ms more** where it grows: as the surface opens and as its save loads.
