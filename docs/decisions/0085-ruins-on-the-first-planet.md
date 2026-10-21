# 0085 — Ruins on the first planet

- Status: accepted; slice a built (spires, alloy, caches, the relic's shrine); vaults and scrolls next (slice b). Amends
  [0080](0080-caves-glow-by-depth.md): a growth felled by nobody now lets its yields fall.
- Date: 2026-10-21
- Design: `designs/ruins-scrolls.md` (the session's scratchpad) and its review, which overrides its top

## Context

The first planet (0077, 0079, 0080) had ground, caves and their growth, but nothing anyone had made: nothing to aim a sortie
at, and nothing to find that the ship could not make. The concept wants ruins of whoever came before: landmarks seen from
afar, a shell harder than stone to get into, and what they hold, tech the ship cannot make, worth the dig. The arcs' tech
will be a module set into tools and stations (designed next); this slice places its relic.

## Decision

- **Alloy, a natural material** (`Materials`, code 6, `a` in a test's material map): strength 60, five times stone — fifteen
  seconds of the axe, six of the drill, four of the arc drill — yielding a scrap of it, `alloyScrap`, which builds it back.
  Its walls are a blueprint as soil's and stone's, opened once alloy scrap is analysed (0027), aboard as leaky decor (leak 36,
  as stone); its back walls follow the rule natural back walls get in 0084.
- **Spires** (`Content.Ruins.Spire`, 9 × 30, drawn as ASCII: `#` alloy, `.` open with an alloy back wall, `s` a soil plug,
  `c` the cache, `r` the shrine, `l` the lamp, a space the ground as it grew): a tip of alloy with a lamp in its top cell
  over a room walled at either side by a soil plug a hero's height, which two swings of the axe open; under it, a body sunk
  in the ground, a hole in its roof dropping into a shaft of ledges three rows apart, either side by turns, so a hero jumps
  back up as it fell, and at the bottom a chamber with a cache on its floor and a shrine in its right wall.
- **Where** (`Terrain`, a pure function of the seed, the ground's integer hash, no world Rng): `Ground.Spires` (4 on the first
  planet, 1 on the small one, so the fuzz digs alloy), each in a band of its own along the width so they spread and never
  overlap, every column at least `Ground.RuinClear` (384) from the middle, so finding one takes a walk; anchored at the
  highest surface across its columns, `Ground.SpireRise` (10) of its rows above that, so its tip stands over the ground
  round it (0 buries it). Moved down or up only where a grid is too small to hold it (the small planet's few seeds).
- **Stamped last:** after the caves' growth, before the growth is sorted by cell: its cells overwrite the rows and the
  materials but where it keeps the ground; what grew in its box goes, and what lay in the rock a cell round it, which the
  alloy may close off from its cave; its own growths are added. Spikes and slopes were cut before, so nothing cuts into its
  alloy; a slope beside its shell is left as it was cut.
- **The cache, the shrine and the lamp are cave kinds** (`Flora.Cave`) with no chance in any band — never grown, only set
  by a spire — so Building fells and saves them as any growth, with no list of their own: the cache stands on its alloy
  floor and opens to any hit, giving alloy scrap 4, wire 6 and silicon 2; the shrine lies in a wall of alloy and goes when
  the alloy is dug out, sixty damage, giving an `arcCoil`, a relic, gear so kept when its hero dies; the lamp lies in the
  tip's top cell and gives nothing.
- **A growth felled by nobody lets its yields fall** (slice 0, every kind of flora alike, the surface's and the caves'):
  built over (a floor or a ladder in its cell), its hold gone to a meteor — whatever derivation's sweep fells — it drops
  its `Yields` at its cell for anyone (`DropAt(FloorStack.Nobody, …)`). Its cell is in `Felled` from then, so a load never
  drops it again. Building a floor over a cache is no way round its alloy, but it no longer destroys it.
- **Seen** (placeholders till the user picks on a page): alloy walls of cool steel plates in courses, rivets and a few
  engraved glyphs, its back walls darker, metal grit on its tops and vents, glyphs and the makers' emblems as details; the
  cache an alloy capsule with a small teal lamp, the shrine a niche holding the coil wreathed in arcs, and the spire's lamp a
  cyan lens in its tip, each casting its own light (glowing things give light).
- **Dev:** `--ground=spire`, `spire-in` and `spire-far` stand the hero at the spire nearest the spawn, on its roof by a plug,
  in its chamber, or forty columns off; `--snap FILE` saves the screen once the world is `--snap-at` seconds old and quits.

## Rejected

- **A list of ruin growths of its own** (`Flora.Ruin`): everything Building needs of them a cave kind already has.
- **Stamping before the spikes and slopes are cut:** they would cut its shell, and the caves would grow in it.
- **The arc drill made of the coil at the lifepod:** the arcs become a module set into tools and stations, designed next;
  till then the coil is a trophy and the arc drill is still found (`Weapons.Found`).

## Deliberately not handled

- **Vaults and scrolls** (slice b), the crucible (c) and the deepest scroll (d).
- **The arc coil does nothing yet:** no recipe or module takes it; it waits for the module design.
- **A spire's body stands above lower ground beside it:** anchored at the highest ground across it, its alloy shows
  where the ground falls away, a wall a hero may have to dig or jump. Over a chasm it hangs.
- **Re-dealing the ruins changes old planets:** a save of a planet grown before this change grows spires where it had none;
  its edits still apply by cell.
- **Spires at the edge of a small grid** move to fit, and their anchor is then not the ground's highest.
