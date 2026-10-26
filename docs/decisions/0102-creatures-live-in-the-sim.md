# 0102 — Creatures live in the sim

- Status: accepted; slices 1 (the sim), 2 (the client) and 3 (four kinds in the looks the user picked) built; its creature's mode and the species as one flat record superseded by [2026-10-26-creatures-are-composed](2026-10-26-creatures-are-composed.md)
- Date: 2026-10-22
- Design: `designs/creatures.md` (the session's scratchpad), its review and the user's decisions, which override its top

## Context

The first planet's ground, caves and ruins are there to explore, and nothing on it moves. Creatures are the next
mechanic: neutral ones on the first planet (the user's word), hostile ones on planets to come, a pen aboard later to keep
them. The user's focus for the first slice is how a creature takes hits and reacts to the player; what it gives is a
placeholder. A creature walks, falls, is struck by the axe and the drill of any hero and drops what it gives: every peer
must agree on where it is and when it dies, so its body, its hit points and its yields are the world's.

## Decision

- **A module of every surface, `Fauna`** (`sim/Fauna`, `dependsOn` Grid, Crew, Items), as debris is (0006): creatures are
  spawned, moved and killed by the same integer rules on every peer, saved and hashed with the world. Inputs are only
  commands — `Strike`, the dev `Release` — and the heroes' reported cells. It runs last, after crew, in the simulation phase.
- **State:** each creature's `Id, Species, X, Y, VX, VY, Damage, Mode, ModeLeft, SeenAt`, the module's `NextId` and
  `Clock`. Derived, never stored (the review's cut): its facing, the sign of `VX`; whom it flees, found as its flinch
  ends; the beat, `Clock % Beat`. `SeenAt` stays, as the far field's (0052): despawning by linger needs it.
- **Habitats are data on the planet type** (`PlanetType.Fauna`, 0095): each a species, a place — the open surface (no back
  wall) or a cave floor in one depth band (0080, `Terrain.BandOf`) — and a weight. The first planet has lopers on its
  surface; its bands are ready for the glimmite and the driftbell (the design's slices 3 and 5). The ship is no planet
  type: it spawns nothing, draws nothing and its fauna slice never changes, so a ship's run is as before but for the
  slice's bytes.
- **Its own ring.** Round each hero in id order, on a beat of 2 s, a point 62 to 76 columns out (past the farthest view's
  60, inside the keep box's 80) on either side, then down the column inside the keep box to the first place the habitat's
  species stands; never in any hero's view (`Fauna.View`, the debris' own). At most 10 in a hero's keep box from spawning
  and 40 on a surface. Gone 30 s out of every keep box, at once out of every hard box, and with the last hero. The ring is
  drawn alongside the far field's (`DebrisModule.SpawnAround`), some fifteen lines alike: a shared ring waits for a third
  caller and a hash-equality test that proves debris draws as before.
- **Walking** (`Live.cs`): integer, per axis, under the planet's gravity (`Kind.Fall`, as stacks fall); solid is what
  stops a stack. A walker steps up one cell and down one, turns back at a wall it cannot step and at a drop of three rows or
  more — at the brink, judged by where its edge gets this tick, so a fleeing one stops there rather than overhanging — and
  walks off a shallower one. A tick moves under a cell across (the fastest flight 750) and down (`MaxFall` 600), so it never
  passes through a wall a cell thick. Modes: idle, wander, flee, hurt; only an idle or a wander begun draws from the Rng.
- **It jumps** (the user's word after a playtest: pinned to a wall a loper could do nothing). A species' `Jump` (speed up) and
  `JumpAcross` (speed across in the air) are data; its highest and longest follow under the planet's gravity (`Leaps`). Where it
  would turn it first flies a leap through the grid, tick by tick as it would move, and takes it if it lands at least a cell on
  and no more than three rows down; else it turns as before. Fleeing, the lowest of three jumps that lands (sized for a ledge
  of two, of three, and its whole jump), over walls and across gaps; wandering, only up a ledge of two at its feet, so it does
  not bound about. In the air it presses flush on a wall and is carried over once clear. The loper: 440 up and 400 across,
  4.4 cells high and 16.8 long landing level on the first planet; it clears a wall of four and a gap of a dozen, and
  turns at a wall of five and at a pit too wide or deep. Still cornerable: under a low ceiling, or by a cliff of four rows
  or more, or a pit past its leap. No draw: it is a pure function of the grid.
- **The loper,** 2 × 2, 12 hit points: six axe hits, twelve of the drill, six of the fitted drill. Wanders at 2 cells a
  second, flees at 15 for 3 s, then calms to idling and wandering: faster than a hero's sprint of 13.2, so a blow is the end of
  the chase for now — the hunter follows and finds it calm again, corners it at a cliff or a wall, or will stun it (the arc's
  stun module to come). Tuned in play twice: at 9, then 6.4 (under a walk, so a hunter kept up and the drill held it), the
  user wanted it to outrun a sprint so a stun has a point.
- **A hit** (`Strike(Creature, Tool)`) is any weapon's, its damage the tool's `ToolStats` for its sender (0098), refused
  `fauna.gone`, `items.flat`, `items.notRunning` as wrecks' hits are (0054, 0055). It knocks the creature away from the
  striker's reported cell (`Knock` 200 a tick, a hop of under a cell). **Hit-stun is the species' own** (`Species.Stun`, ticks
  steering nothing before it flees, then flight from the nearest hero): neutral creatures have none and bolt the knock's way at
  the first blow; a hostile may hold still under one. The loper's is 0 (the user's word after the first recording: with an
  8-tick flinch on every hit the drill held it in place and ground it down; a neutral creature must get away). A faster tool
  knocks its pace's share (`Fauna.KnockPace`, the axe's 10 ticks): each drill hit a fifth, so ten hits a second do not bat it
  across the screen; and a blow never slows one already running the knock's way, so the drill's run of small knocks cannot hold
  it under the bit either. No hop renewed while it rises. The last hit drops its yields by nobody from its top
  row, so they fall where it stood: placeholders, two or three biomass and a seed one time in four. The roll is debris' loot
  roll, moved to `Core` (`Loots.Roll`) for both.
- **Dev:** `Release(Species, Cell)`, a command as `world.open` is, refused `fauna.notHere`, `fauna.full`, `fauna.blocked`;
  the client's `--fauna` sends it.
- **Save format 27:** an empty fauna slice after every other, aboard and on every surface.

## Slice 2: the client (`features/fauna`)

- **A blow is felt the frame it is struck.** This player's own `Strike` makes its creature react the frame it is sent, a round
  trip before the sim's knock arrives; a friend's as the sim's damage rises. The reaction (`CreatureMotion`): a white flash, a
  squash that springs back past round, a flinch thrown the way it is knocked, a wide eye and pinned ears, a 70 ms shake, the
  blade stopping on it with the figure's own bite; then the sim's knock, hop and flight, drawn between ticks. All of it is drawn
  only: the drawing is never held in place (the first version froze it 70 ms, which with the sim's stun read as pinning). A
  drill's hit is its share of the axe's, as for the ground (0094).
- **Seen and heard by what it is** (0093, 0094): an `Impact` per species (`Flesh`: white slash streaks, fur tufts, a warm puff,
  a glint and shock line, no blood) and a hit, thump and death per species, placeholders from the files here till the user
  picks. Death: a burst, puffs, a ring, the body swelling white and gone; its yields fall as the sim drops them.
- **The body was a placeholder** (the review's slice-1 art), replaced by the user's picks in slice 3: a round body with ears,
  legs and one eye that watches a hero within 9 cells, leaning as it flees, tilted onto slopes under its feet.
- **Contact** is the wreck's (0039, 0054): the axe's swept edge and the drill's bit against each creature's body and a margin,
  but wherever the hero stands, not only out in space.
- `-- --planet --fauna` lets six lopers out by the hero and, without `--manual`, hunts them by script for recordings.

## Slice 3: four kinds, their looks, and a gallop

The user picked four looks on the loper page ([Лопер: облик](https://claude.ai/artifact/Rt6NAdUCtPCGR64REJWkkL)), 2026-10-22,
"they can go in now": «Классика», «Тапирчик», «Шестиног», «Пушок».

- **Four species, one life.** Each a `Fauna.Species` naming its `Look`, the design it is drawn in; the loper's behaviour for all
  (wander, notice, flee 3 s, jump, no hit-stun, knockback; neutral). Numbers differ only where a body calls for it:

  | Species | Look | HP (axe hits) | Wander, flee (cells/s) | Knock, hop | Jump up, across (height) | Yields (placeholders) |
  | ------- | ---- | ------------- | ---------------------- | ---------- | ------------------------ | --------------------- |
  | `loper` | classic | 12 (6) | 2, 15 | 200, 180 | 440, 400 (4.4 cells) | 2–3 biomass, seed 1 in 4 |
  | `tapir` | tapir | 16 (8) | 1.6, 14 | 150, 140 | 400, 360 (3.6) | 3–4 biomass, seed 1 in 3 |
  | `sixleg` | sixleg | 14 (7) | 2, 15 | 200, 180 | 440, 400 (4.4) | 2–3 biomass, seed 1 in 4 |
  | `fluff` | fluff | 8 (4) | 1.6, 14 | 240, 220 | 380, 360 (3.2) | 1–2 biomass, seed 1 in 4 |

  The tapir is heavier (more hit points, knocked less, a lower jump: a wall of three, not four); the fluff small and light
  (knocked farther, a smaller hop, still over a ledge of three). Both flee at 14, still past a sprint's 13.2.
- **Spawned by habitat** (the user's rule: the surface, the upper caves, the garden band, the lower layers), data on the
  planet type: the loper (weight 3), the tapir (2) and the sixleg (2) on the open ground; the fluff, whose glow lights the
  dark, on cave floors of the cold band (2) and the garden's (3). Nothing yet in the amber and ember bands. The ring's search
  down a column already finds a cave floor with a back wall in the habitat's band (slice 1), so no new search was needed;
  every habitat is drawn by weight together, and a draw with no place in its column wastes the beat as before.
- **Art as index maps.** The page's painter (`tools/planet-art/fauna`, `loper-parts.js`) paints every part as an index map, R
  its ramp (23–31, beside the ground's), G its tone (outline to highlight), and a `_glow` map of its glowing pixels;
  `export.js` writes them into `game/art/fauna/<design>/`, byte for byte the page's own exports, and their sizes, pivots, rig,
  pose numbers, eye and the three palettes' ramps into the generated `game/Content/FaunaArt.cs`. They are imported lossless,
  no mipmaps, no alpha-border fixing, and recoloured at load (`CreatureParts`) by the planet's palette
  (`FaunaLooks.PaletteOf`: the first planet's the page's «Саванна»), so a planet to come recolours them with a palette, not
  with new pictures.
- **Drawn as the page draws them** (`CreatureBody`, `CreaturePose`): a sprite a part in the rig's order, each turned about its
  pivot and hung from its parent's, at the screen's resolution as a hero's parts are; the eye in its four states (open, half
  shut grazing, shut blinking, wide startled), its pupil on the hero it watches by whole pixels. Idle with no hero near a while
  it grazes, with one near it notices. Its glowing parts are added over it unlit and each spot of them gives light (glowing
  things give light): the fluff's spots and antennae light a cave, the tapir's pod and the sixleg's berries a little round them.
- **A gallop, not a twitch** (the user: "all variants twitch very fast when fleeing"). The legs went by the ground covered a
  frame, a stride lengthening only to 34 px: 7.2 strides a second at 15 cells a second, and a 3.5 px bound each. Now its
  strides go by time at `MostStrides · tanh(v / (MostStrides · Stride))` strides a second (`CreatureGait`): one each `Stride`
  pixels at a crawl, rising smoothly and never past its cap however fast; its speed is the ground the sim moved it on its last
  tick, eased, never a frame's step; everything eased by time, so 60 and 144 frames a second draw the same. At full flight the
  pose blends into a gallop: forelegs together and hind legs half a stride on (a six-legger keeps its tripods), wider reach, a
  bound a stride, the body rocking nose up and down and stretching out and gathering; the fluff bounds along in hops.
  Measured (the view's pipeline in a test, 60 and 144 fps, even and ragged frames alike): before, 2.6 strides a second walking
  and 7.2 fleeing for every kind; after, walking 2.4 (loper), 1.9 (tapir), 2.4 (sixleg), 2.5 (fluff), fleeing 3.2, 2.6, 2.8
  and 3.4.
- **One of a kind is not the next** (the user, 2026-10-22: "variety within one species, by size and colour, will give a strong
  effect"). Drawn only (`CreatureVariety`, plain C#, tested): the sim's 2×2 box, hit points and speed stay the species', so
  pathing, jumps and the wall tests are untouched, and the blade still meets the sim's box, as for every creature. Each creature's
  looks come from its id and its surface's seed by an integer hash, never the world's random stream, so every peer draws the
  same individual and a load brings it back. Its **size** 82–116 % of its design (`FaunaLooks.Varieties`, per species), a
  golden-ratio step an id so any three in spawn order span most of the range; its **coat** (the fur ramp only: never its
  outline, eye, skin, belly or glow) shaded up to three quarter tones between its ramp's own tones and blended 21–45 % toward a
  neighbouring ramp of the palette, cool and warm by turns an id (the loper toward the lavender fur or the rosy skin, the
  cool-coated three toward the tan fur, the skin or the belly); so a coat lies between its own ramp and its neighbour's a tone
  either side, in every palette. One in seven is a **morph**, its coat a whole tone off: the loper and the sixleg darker, the
  tapir and the fluff paler. Its **markings** (the marks part's runs of pixels: the loper's stripes, the tapir's saddle, the
  sixleg's patches, the fluff's glowing spots) each dropped a quarter to a third of the time, those kept faint, half or bold;
  the fluff's spots kept bold and lighting only where worn. Recoloured once a look, kept for the run.- **Down a slope without a snap** (the user: "at some point it straightens onto flat ground and drops sharply"). The sim stands
  a creature on the top of a slope's whole cell and steps its body a whole cell in a tick; the client drew each foot relative to
  that row, at most 0.6 of a cell down, so at a slope's foot the body hovered over the flat until the sim stepped it down and
  then dropped 56 px in two frames, and at a slope's start dipped and popped up. Now the sim's `FeetOf` reads the ground's own
  surface under each foot, in the feet's row and the two under, so the body's steps move neither foot, and the client draws
  the feet there, closing anything else (a whole step, an edge's teeter, a landing) at a cell each 90 ms, the tilt eased in and
  out over 90 ms. The sim's physics are unchanged; `FaunaFootingTests` walks and flees a loper over a hill of slopes and back,
  its feet never moving up or down faster than it walks across. Measured in the same recorded walk: 59 frames where a
  grounded body moved more up or down than across before, none after.- **Seen and heard by what it is:** the cool-coated three throw lilac tufts (`Impact.CoolFlesh`); the tapir sounds lower and
  the fluff higher than the loper (placeholders).
- **Dev:** `--fauna=<species or look>` lets out that kind only (else the six round the hero are every kind in turn), from the
  hero's row up out of a hill or down to a floor, so in a cave too (`--ground=band1 --fauna=fluff`); `--fauna-trace` logs each
  creature's strides a second and each one's looks as it comes into view; `--fauna-herd` lets out ten of a kind in a row.

## Measured

On this machine, Release build, the first planet with a hero and its cap of 40 lopers walking, struck every half second:
**0.055 ms** a tick mean against 0.024 idle (`BudgetTests.KeepsAPlanetsTickWithFortyCreaturesUnderBudget`).

## Next

- **Stun from a module:** the arc's stun module will add hit-stun through a hit effect (`HitEffect`, 0054) on the tool, added to
  the species' own as the strike applies; nothing in the module reads it yet.

## Rejected

- **Creatures in the client or on the host only,** sent as snapshots: a second state path, interpolation of others' worlds,
  and hits judged against positions that differ by peer. In the sim they cost a few integer adds each.
- **Sharing the far field's ring now** (the design's first version): changing debris' spawning in the same change, and every
  pinned draw with it; the review cut it to a later, separate step.
- **Herds and fleeing from a running hero** (the design's first version): the review kept single lopers that flee when hit.

## Deliberately not handled

- **Creatures collide with nothing alive:** not with each other nor with heroes.
- **A slope is stood on at its top;** the client draws a creature's feet on the diagonal (`FaunaModule.FeetOf`).
- **They sense through rock;** flight is from the nearest hero by box distance.
- **Despawning forgets damage,** and hunting gives biomass without end but the cap and the beat.
- **Nothing aboard:** no creature lives on the ship, nor may be let out there; a pen aboard (the user's later idea) is a
  decision of its own and nothing here blocks it.
- **Creatures in the ruins:** they spawn on the open surface only; one that wanders into a ruin is welcome there.
- **Size is drawn only:** a big loper and a small one have the same box, hit points and jump; a body drawn 16 % over its box
  overhangs a wall by a pixel or two, as an overhang already does. No young, no sexes: an individual is a look, not a life.
- **A dying body's parts show through each other** for its 170 ms of fading, under its white swell: each part fades alone,
  where the page laid the whole on a layer first; and a far ear's glow is added over the body, as the page adds every glow last.
- **The fluff lives only where its habitat finds a cave floor** in its column inside a hero's keep box; a hero deep in the amber
  or ember bands meets none, as nothing lives there yet.
