# Design: the liner and the tanker, abandoned vessels of many decks

Reference: [the approved mockup](big-vessels-mockup.html), published at https://claude.ai/artifact/ERaaUh83N6A5SgAoWtsx8D.

## Problem
Every derelict today is a shuttle or a freighter: two or three decks of rooms one deck tall. There is no big ship to
explore, with halls two decks tall (a pool, a restaurant, a hangar with a shuttle in it, a tank hall) and ladders between decks.

## Player experience
1. The radar shows a liner or a tanker; the player sets a course and flies the shuttle there.
2. Outside, a liner has a long light hull, a glazed promenade with a dome on it, a fin with an emblem, a row of escape pods
   with about half gone. A tanker has a bridge over its stern, pipes and valves along its back, sometimes spherical tanks
   under its keel.
3. Through the airlock: red lamps, doors along the deck, hatches with ladders down.
4. On a liner the hero climbs down into a pool hall two decks tall (half-drained, murky, a diving board); next to it a
   restaurant with a bar, a grand piano and two chandeliers.
5. Below: holds with containers stacked high, a fuel tank at the stern, at the bow a hangar with a small shuttle. All of it
   comes apart for parts.

Goes wrong: the hero takes apart the only ladder into the hold band; they build a ladder or cut through, as on a planet.

## Out of scope
- Liquid in any tank, sphere or the pool: the water is part of the basin's picture.
- Flying, boarding or fuelling a hangar shuttle: the five models only come apart.
- Wrecked big vessels, gravity changes, enemy vessels.
- Any change to the shuttle, the freighter, how a vessel world opens and closes, or the radar's rules.

## Scale
- One vessel world at a time; the liner's frame about 380×75 cells, its world about 636×331 (210k cells), about 25k parts
  laid once at the jump. Nothing new per tick. 1–4 players aboard.

## Approach

### 1. Rooms several decks tall, and bands
- `Vessel.Compartment` gets `Decks` (1 by default), height `Pitch × Decks − 1`; `Stamp` lays no deck floor in a room's
  columns between its decks. A raised room is `Deck` −1, or −2 for the second tier; `SectionKind.Tier` for promenade and dome.
- `Vessel.Bulkheads (X, Y)`: two columns of hull across one deck's six rows where two blocks meet with no door.
- `Describe` split in two: a class planner returns bands of blocks (a width and a stack of rooms: role, span of decks, a
  drawn interior or a fit); one shared layout places blocks, doors and bulkheads, connectivity, raised rooms, furniture,
  sections, modules and outside parts. `VesselClass.Layout` names the planner: `Rooms` (today's code, moved unchanged),
  `Liner`, `Tanker`; their numbers in `Vessels.Liner`, `Vessels.Tanker`.
- Shuttle and freighter: the aft block, then one band per deck. Liner: band A over the upper decks, band B the bottom two.
  Tanker: one band over every deck.
- Today's classes stay exactly as they are: a test fingerprints `Make` for shuttle and freighter seeds 1–64, recorded before
  the split. Why exact: the client dresses an open world from `Describe(class, seed)` when it loads.
- Rejected: a separate `Describe` for big ships (two copies of doors, hatches, parts); rooms as a cell map (every client
  reader of `Compartments` rewritten).

### 2. Connectivity (stamped by `Describe`)
1. Doors: in each gap between blocks, on each deck, a door where that deck is the bottom of both rooms; else a bulkhead.
2. The well: down the block after the aft block, a hatch at its Left in every floor where the rooms above and below differ.
3. Raised rooms: a hatch at their first inside columns, a shaft to the floor below; the dome's ladder runs on through the
   promenade.
4. Repair pass: while a room cannot be reached from the airlock, the first such in reading order gets a hatch to a reachable
   room directly above or below. If none fits, `Describe` throws (our bug).
- Ladders are two-row pieces, rounded down; the leftover row is a one-cell step. Fitted rooms keep ladder columns clear.
- Invariant `EveryRoomIsReachedFromTheWayIn…` over all four classes; a door only where floors meet; every ladder ends within
  one row of its floor. Proven to bite by a liner with its repair hatch removed.

### 3. Interiors
- Tall rooms, and one-deck rooms with new furniture, are furnished by code: a `Fitter` (sim/Core/Vessels) after the
  mockup's `furnish`, sizes and odds in `Vessels.Fits`, stream 0 after the layout. One-deck rooms of existing furniture keep
  ASCII drawings (cabins, crew, storage, machine, the room under the bridge); new narrow drawings for liner blocks.
- Why code: the legend has about 15 free characters against about 25 rows; tables fill a room's width; holds stack per seed.
- New furniture rows (footprints tunable, Gives from existing items):

| Row | Gives |
| --- | --- |
| `vesselBar` 7×3 | planks 3, glass 2, plate 1 |
| `vesselPiano` 5×3 | planks 4, wire 3, copper 1 |
| `vesselRoundTable` 3×2 | planks 1, cloth 1 |
| `vesselChair` 1×3 | planks 1 |
| `vesselLounger` 4×2 | frame 1, cloth 1 |
| `vesselDiving` 5×8 | frame 2, planks 1 |
| `vesselPool` 20×5 (basin, water, waterline and ladder drawn in) | plate 6, tube 2 |
| `vesselChandelier` 3×3 (hangs, glows) | glass 3, wire 2, copper 1 |
| `vesselStove` 4×3 | plate 2, coil 1, wire 1 |
| `vesselCounter` 5×2 | plate 2 |
| `vesselPump` 4×4 | gear 2, tube 3, rotor 1 |
| `vesselValves` 3×4 | tube 3, gear 1 |
| `vesselCapsule` 14×11, 5 looks (fuel, H2O, O2, N2, coolant) | plate 10, tube 4 |
| `vesselTankTall` 8×11, 4 looks | plate 6, tube 2 |
| `vesselFuelColumn` 10×19 and 10×26 | plate 14 / 18, tube 4 |
| five shuttles: GULL 21×7, MULE 24×9, BEETLE 16×9, WHALE 24×7, HERON 16×11 | the mockup's yields; motor → rotor, small nozzle → copper |

- Reused: `vesselLamp` (two in a tall or wide room; the hangar's floods), `vesselCargo` stacked 2–3 high, crate stacks,
  `salvageVending` as the galley fridge, rack, console, seat, bunks, locker, table. Tank contents are looks
  (`Vessels.Coloured`). The hangar's shuttle model is drawn on stream 0. Pictures in `tools/anime-art/subjects/vessel.js`.

### 4. Raised rooms, hull and outside parts
- Raised rooms are hull, six open rows each; no module bulges up under any. Liner: promenade 44–60 wide over the first
  block at least 10 columns past the service block, a dome ≤ 18 wide on its stern end (second tier), the bridge forward
  with its housing. Tanker: the bridge over the stern (`BridgeAt`), glass fore and aft.
- Glass in `Stamp`: the dome's whole roof, the promenade's roof in panels (3 of every 5 columns, tunable), bridges as today.
- New outside rows, placed in `Outfit` with `OnTop` and `Beneath`:

| Row | Gives |
| --- | --- |
| `vesselFin` 13×10 on the service block | plate 8, copper 4 |
| `vesselPod` 5×4 every 6 columns; about half an empty `vesselCradle` 5×3 (stream 2) | pod: plate 4, cloth 1, wire 2; cradle: frame 2 |
| `vesselManifold` 12×2 | tube 6, gear 1 |
| `vesselRiser` 1×1, stacked | tube 1 |
| `vesselValve` 2×2 | gear 1, tube 1 |
| `vesselDome` 4×2 | plate 2, gear 1 |
| `vesselSphere` 10×11 | plate 10, tube 4 |

- Liner: a radiator aft, the bow strobe. Tanker: the keel-truss routine turned upward for the manifold (one level, struts
  of `vesselPylon`, a riser and valve over each tank bay and the fuel bay), a dome hatch on each bay, spheres under 55%,
  a radiator past the bridge, the boom. Every bridge a mast and a dish.
- Bay doors are a look on the bottom armour under a hangar (wall cells hold no machine).
- Client: `VesselLooks` per class (old lists kept in order); `VesselDress` bay doors and the fin's emblem; `VesselLights`
  the chandelier as a lamp, the airlock by its room; `VesselSigns` one-deck rooms only; new `VesselDecor` (pool tiles,
  wainscot, bottle shelves, lifebuoy, hangar pad) by room role while its back wall stands.

### 5. Performance
- The liner's world ~1.6× a freighter's cells, ~3× its hull parts. Stage 1 adds both classes to the 200 ms jump-tick pin.
  If missed: first `Outfit`'s per-cell `sections.Any` and column LINQ scans (a table of hull rows per column), then laying
  parts in Places. The pin is not raised; the write is not spread over ticks.
- Client load: `Describe` + `Stamp` once per view; if the liner's hitch is clearly worse, stamp once and share.
- Frames: about 100 lamps walked per frame; measure on `--vessel=liner`, cull to view only if over budget.

### 6. Where it is heading
Bands, tall rooms, bulkheads, connectivity, raised rooms, the fitter and one segments-on-supports routine. Next users:
wrecks (`Damage` works on any layout) and new classes (one more planner). No hooks for liquids or flyable shuttles.

### 7. Stages
1. Liner shape: bands, tall rooms (lamps only), doors, bulkheads, hatches, raised rooms with glass, light livery; the
   fingerprint test and the pin.
2. Liner interiors: pool, restaurant, lounge, galley, stacked holds, fuel tank, hangar with the five shuttles, decor.
3. Liner outside: fin and emblem, pods and cradles, radiator, bay doors, signs.
4. Tanker shape and interiors: stern bridge, fuel column, tank bays and pump rooms, the occasional hangar.
5. Tanker outside: manifold, risers, domes, spheres, boom; yields measured for both; then ship.

## Determinism and co-op
Nothing new enters the sim. `Describe` stays pure on local streams (0 layout and furniture, 2 shape and parts); new classes
at the end of `Vessels.Classes`. Whole cells, whole percent rounded down, ties in reading order, no dictionaries. Looks
from `LookRng`. Save format: no bump; new classes add surface kinds and contact class strings, both additive.

## Trade-offs accepted
- Fixed footprints: a room's width follows its furniture. The bow keeps today's slope model. A ladder ends one row above the
  floor. Signs avoid tall rooms.

## Open questions (answered in the plan)
- The mockup as the pick for the ~30 pictures; the infirmary and fuel cart; how often the radar draws the new classes.
