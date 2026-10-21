# 0090 — Homes of the arc makers: furnished ruins in the first planet's ground

- Status: accepted; slice 1 built (three homes, their 49 pieces, plaster, cloth, the arc lamp as an item); slice 2 built (the
  arc lamp hung aboard, the spire's chamber furnished). Amends [0080](0080-caves-glow-by-depth.md) and
  [0085](0085-ruins-on-the-first-planet.md): a growth may stand over several cells, hang on a back wall, or stand on another.
- Date: 2026-10-21
- Design: the homes concept page the user approved whole ("take every set on it"): its three scenes, its catalogue of 49
  pieces with sizes, anchors, light, motion and yields, its proposal and its table of drops.

## Context

The spires (0085) are posts: alloy, a cache, a shrine, nothing that says anyone lived. The user wants the ruins to be homes of
a civilisation that used arcs in everyday life — lamps brighter and colder than ours, a kettle warmed by a coil, a music box
playing from a spark — where everything breaks and gives something, and above all it is cosy and plain that someone lived
there. The page drew three homes cut in the rock, each lit two ways ("lamps still lit", "last lights").

## Decision

- **A home is a ruin kind of its own** (`Content.Ruins.Homes`): three prefabs, the page's scenes — A, the hearth room with
  its kitchen through an arch; B, the bedroom and the child's corner behind a curtain; C, the half-collapsed study with its
  scroll cases and the resonator. Each is a room five cells high in an alloy shell, its back wall plaster over an alloy
  wainscot (the bottom row), drawn in ASCII as a spire is (`#` alloy, `p` open on plaster, `.` open on alloy, `s` a soil plug,
  `t` stone, `,` open on stone, a space the ground as it grew), and its pieces listed by kind and cell. A and B have a soil
  plug a hero's height in either end wall; C only at its left, its right end caved in, a mound of stone with the caves'
  teal and violet growth on it.
  - *Rejected: a character per piece in the ASCII.* Forty-nine kinds, most wider or taller than a cell and some in front of
    others, would need forty-nine characters and a rule for which cell of each is its own; a list says the same plainer.
- **Where:** `Ground.Homes` (3 on the first planet, one of each scheme in turn; 1 on the small one, its scheme by the seed),
  stamped after the spires, last of all: a pure function of the seed (the ground's integer hash, no world Rng). Each in a band
  of its own along the columns `Ground.HomeClear` (384) or more from the middle — on the small planet anywhere (0), too narrow for a
  home beside a clear middle — the column nearest the seed's pick in its band whose box, two columns wider each side, meets no
  spire; its ceiling `Ground.HomeDeepLeast` to `HomeDeepMost` (8 to 20, by the seed; 4 to 10 on the small planet) rows under the lowest surface across it, moved up only where the
  grid is too small, so it is found by digging, near the surface (the page: no shaft, a tunnel through a plug). Stamped as a
  spire is: what grew in its box, reached into it or stood on its walls, and what lay in the rock a cell round it, goes; its own
  pieces are set. The spires' stamp now takes growth reaching into its box by any cell or standing on it too.
- **Plaster, a natural material** (`Materials`, code 8, `p` in a test's material map): the homes' warm back walls. Never built
  (no cost), so no blueprint; its walls never stand anywhere, but it has their pictures, as every natural material does.
- **Pieces are growths** (`Flora.Piece`, listed in `Ruins.Pieces`), so Building fells, saves (`Felled`, by cell) and drops
  them as any growth, with no list of its own. What a piece needed that a growth could not do, every growth now has:
  - **`Across`:** its columns, its own and to the right, beside `Tall` rows (up from a floor, down from a ceiling or a back
    wall). A hit on any of its cells hits it; its crack is kept by its own cell (the bottom-left on a floor, the top-left
    else). It holds on to the wall under (or over) each of its columns: any of them dug fells it. Only its own cell built
    over fells it, as a tree's crown over a wall does not (0080).
  - **`Anchor.Back`:** hung on the back wall of its open cells — paintings, shelves, sconces, the window panel. A natural back
    wall is never taken apart (0084), so one goes to a hit, or to its own cell built over.
  - **`Behind`:** a back wall's pieces and a rug lie behind what stands in front: a cell holds one piece in front and one
    behind, a hit takes the front one first. No two pieces share their own cell.
  - **Standing on a piece:** a floor's piece may stand on the piece under it (dishes and a candle on the table, a kettle and
    a music box on a stool, a lantern on a table); felling that one fells it, its yields to the same hero.
  - **`Fixed`:** painted on or cut into the back wall (the mural, the crack): no hit takes it.
  - **In the rock, wider:** the fallen beam lies in two alloy walls on the study's floor, the rubble in two of stone, so
    they block the way; digging any of their cells fells the piece, its yields on top of the walls' own.
- **Hit points** (`Strength`, damage as a wall's, 0077): lamps, shelves and pictures 2 (one swing of the axe), a chair or a
  bench 4, the table, the bed, the desk, the basin and the shelves of scroll cases 6, the hearth and the resonator 12 (as a
  tree); paper, cloth, crockery, the chest (a cache, 0085) and the cobweb fall to any hit; the music box 3, so the axe's first
  swing plays it and its second breaks it.
- **Yields** are the page's table, fixed counts, no draw: Building has no Rng, and a hash of the cell would be a draw by
  another name. The page's "chance of a seed" for the herbs and the live plant is a seed each, fixed: bushes give seeds freely
  already. The chest gives its small cache: planks 2, alloy scrap 1, wire 1 and glass 1.
- **Two new items:** `cloth` (rugs, banners, the tapestry, the curtain, the bed, the bench, the wardrobe, the coat pegs, the
  basket, the cradle) and `arcLamp`, a whole lamp: the ceiling lamp, the sconce and the floor lamp drop one. Cloth burns in a
  furnace (BurnTicks 200, half a plank) till the ship has things to make of it.
- **The resonator zaps** who comes right up to it: every breath (`Crew.BreathEvery`) a hero whose reported cell lies within
  `Ruins.ZapReach` (1) of its cells pays `Ruins.Zap` (5) units of its suit, as the lava burns (0087), through crew's one new
  operation, `ICrewOps.Shock`; a suit run dry passes out as the cold's does. Building does it, in the simulation phase, as it
  keeps the resonators standing (derived); crew cannot read Building, which is built after it.
- **Seen** (`game/features/ruins`, `HomesView`): the page's pictures, exported from its own drawing code
  (`tools/homes-art`, kept in the repo: change the drawing code and export again, never the pictures), lit as the world is,
  what glows added over them, one atlas, a few hundred pieces in the first planet's homes, a node for the view. Floor pieces
  stand on the floor in the middle of their columns, ceiling ones hang from it, a back wall's in the middle of their cells.
  - **Light** (glowing things give light, 0070): each lamp's light where the page puts it, in its colour — the makers' arcs
    cold white-blue and brighter than ours, the hearth warm, the jar teal, the scroll case and the mural violet, the hologram
    cyan — at most `HomesLooks.MostLights` near the view's middle.
  - **The mood**, the page's "mix by the world's seed": what never goes out always burns (the hearth, the jar, the candle,
    the bed's night light, the garland, the toys' spark, the music box, the desk's pixel, the scroll case, the mural, the
    resonator, the broken lamp's sputter); every other lamp is lit or dark by a hash of its cell, two in three lit, so each
    home has some of both and every peer sees the same.
  - **Motion** (0075's toolkit): arcs crackle where the page's emitters put them (`Bolts`, cyan), lights pulse and flicker,
    the broken lamp sputters dark and bright, the mobile's stars circle, the hologram's scanline runs and glitches, the
    desk's last pixel blinks, the garland's beads twinkle, the live plant's spores rise, the hearth's embers fly, dust motes
    turn in the window's light, and banners, the tapestry, the curtain and the herbs sway gently, a pixel at a time from the
    top down (the page left them still; the user asked for it).
  - **The music box** plays a short tune when hit: a placeholder one-shot (a synthesised music-box tine) at eight notes,
    till the user picks its sound; a felled piece puffs dust.
- **Dev:** `--ground=home-hearth`, `home-bedroom`, `home-study` stand the hero in the middle of that home nearest the spawn;
  `--homes lit` and `--homes dark` light every lamp that may go out, or none, the page's two moods; `--no-homes` leaves them
  undrawn, to measure what they cost.

## Slice 2

- **The arc lamp aboard** (`Machines`: `arcLamp`, 1 × 1, drawing 1 as the starter lamp does): its blueprint costs one arc lamp,
  so it opens once one is analysed (the item stays in the analyzer, 0027) and each hung aboard is a lamp found. The light feature
  lights it in the makers' cold white-blue, three tenths stronger and ten cells out (`Light.ArcLamp`), its halo cyan; short of
  power it flickers as any lamp. Its pictures, the ceiling lamp's mount and globe in a cell, from the export: its body, dark,
  drawn whatever its power, and while it shines the homes' own globe alight over it with its glow, and at full power the ceiling
  lamp's arc crackling across it, as a lit home's lamp. `J` on a map.
- **The spire's chamber furnished** (`Ruins.SpireFurnishing`, the page's proposal): a table and a chair, a ceiling lamp over them,
  a shelf with an empty scroll case, the makers' banner by the shrine, a cobweb; set with the spire's own growths.

## Rejected

- **A list of ruin pieces of their own in Building:** everything a piece needs is a growth's, with three new properties.
- **Solid furniture:** physics knows walls only; only the beam and the rubble, which lie in walls, block.
- **Mood by home** (a whole home lit or dark): the page's mix within each home reads more as a ruin.

## Deliberately not handled

- **More arc lamps made aboard** from glass, wire and alloy scrap (the page): research opens what is made of what was analysed,
  and a craft of those three would open without any lamp found; a rule opening a craft by analysing another item would be new.
  Till then every lamp aboard is one found.
- **A piece's other cells built over** leave it standing, as a tree's crown does (0080).
- **Pieces past the first planet's homes:** no surface or cave grows them; only `Terrain` sets them.
- **Text of the past:** the story is told by things, no words, no ghosts, no voices (the page).
- **The scroll case brighter toward the nearest vault** (the page's idea for later): no vaults yet.
- **Re-dealing the ruins changes old planets**, as 0085's did: a save grown before this grows homes where it had none, its
  edits still applied by cell.
