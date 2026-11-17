# Plan: Wrecked vessels, chains of events

## Why

Every vessel on the radar arrives whole today ("wrecks to come", Places README). A wreck turns the same four classes into
places that each read and play differently: a crater is a new way in, an opened room floats, a burnt hold holds less, a
broken ship is two halves. The story is told without words: what happened, in what order, how long ago.

The look was picked on a mockup drawn from real stamped vessels (seed 7 of each class), version 5:
https://claude.ai/artifact/X9dqanpx3SFFVFXDsUt3Z8. Its event and look code is kept beside this plan,
`vessel-wrecks-mockup/fates.js` and `looks.js`: the reference for every shape below (floats there, integers here).
This plan replaces V-2 of `vessels.md` (blast, cut in two, scavenged).

## Words

- **Frame:** the vessel's box, `Vessel.Width` × `Height`, cells from its top-left; every coordinate below is in it.
- **Compartment:** a room of the `Vessel` (`Compartments`, by index), never a Rooms module id.
- **Skin:** a hull cell within two cells of space in the stamped vessel. **Armour:** the hull rows over and under the
  decks. **Rib:** a `Rib` section of the body.
- **Spawn:** `Shipyard.Door` of the built vessel, as Places uses it today. **Arrival room:** the compartment holding the
  spawn's cell.
- **Breach:** the opening an explosion, an asteroid or a break carves. A breach is **frozen** once icing laid on it
  (an asteroid's own breach is frozen when made).
- **Reached:** a compartment an event carved a cell in, burnt, froze, or that a breach opens into (a carved cell inside
  it or within one cell of its bounds).
- **Loose:** a machine of `Vessels.Loose` (crates, chairs, tables, loungers, seats, carts, cargo). **Small:** loose with a
  footprint of at most 6 cells.
- **Group:** an event and the events that directly follow it as its consequence (table below). A group has one age.

## Rules (confirmed with the user)

- A wreck is one of the four classes, built as today, then a **chain of 1–3 events** in time order.
- **Count by class, each count equally likely:** shuttle exactly 1; freighter 1 or 2; tanker and liner 1, 2 or 3.
- **Five events.** Each next event is drawn evenly from those allowed on the ship as it stands after the ones before:

| Event | Allowed when | Where | At most |
|---|---|---|---|
| Explosion | not yet drawn; its void clears the arrival room | the engine room's main engine; on the tanker the tank bay's tall tank nearer the middle (of the two nearest) | 1 |
| Fire | a compartment at least 4 wide is sealed (not reached by a breach), unburnt, and not the arrival room | first in the chain: the galley (liner) or the widest life-support room (others), else quarters; right after an explosion: the 1–2 sealed compartments nearest the blast within 3 blast radii; otherwise: the sealed unburnt compartment nearest the latest event's centre | any |
| Icing | the chain is empty (power loss), or the latest breach is not frozen | the latest breach; first: the whole ship | 1 |
| Asteroid | fewer than 2 (liner, tanker) or 1 (others) drawn, and a spot exists: a column over a module's top, three crater radii from an earlier crater, its crater clear of the arrival room | that spot | 1 or 2 |
| Break | not the shuttle, not yet drawn, a rib exists 22+ columns from the stern and the bow whose cut leaves the arrival room whole on one side | the rib nearest the latest explosion or asteroid (its cause); with none, overload, one of the two ribs nearest the middle | 1 |

  If nothing is allowed, the chain ends short of its count.
- **An asteroid freezes its own breach** (alone it reads dull): rime, icicles and the ice fan round its crater are part
  of the strike.
- **Groups and ages:** fire right after an explosion, icing right after an explosion or a break, and a break right after
  an explosion or an asteroid join the group of the event before them. The last group is fresh or old
  (`FreshPercent`, evenly); every earlier group is old. Examples: asteroid → fire → break is three groups (old, old,
  fresh-or-old); explosion → fire → asteroid is two ([explosion, fire] old, [asteroid] fresh-or-old).
- **Old** reads dull, sparse, no embers. If the first group is old, the hull is pitted, its paint faded, and scavengers
  took skin panels round 2–4 ribs (ribs left) and patched up to two holes.
- **Rooms not reached keep everything that stood in them.** Scavengers take nothing from rooms. **Engines stay** unless
  an event carved them.
- **Crew remains: bodies in suits,** in reached rooms only; their look is a pick of its own (W-2).
- **Radar:** a wreck is not told apart on the radar; it is learnt on arrival.

## Design

### One builder: `Shipyard.Build`

`Shipyard.Build(cls, seed, forced)` returns `Built(Vessel Vessel, Wreck? Wreck, IReadOnlyList<VesselPart> Parts)`:
described, stamped, then the chain applied. `Built.Vessel` is the vessel as wrecked: sections, decks, compartments and
doors moved with their half. `Stamp` becomes internal (tests see it), so the compiler finds every caller; `Door` takes
the `Built`. The callers moved to it in W-1:

- sim: `Places.Opened`, `Sim.Open` (the world's map through `Space`);
- game: `VesselDress` (its `Covers` set and `_jammed` from the parts), `VesselSigns` (parts and `Door`),
  `VesselLights` (parts and `Door`), `SurfaceViews.cs:96` (`BeaconLights`);
- tests: `VesselTests`, `PlacesTests`, `BigVesselTests`.

`Describe` stays public for the intact shape of a class. `Damage(parts, vessel, VesselCause)` and `VesselCause` go: the
asteroid replaces the breach.

### The wreck's record

```csharp
public sealed record Wreck(IReadOnlyList<WreckEvent> Events, bool Scavenged, IReadOnlyList<Patch> Patches);
public sealed record WreckEvent(
    WreckKind Kind, bool Old, int Group,
    int X, int Y, int Radius, int DirX, int DirY,           // centre and direction, frame cells; direction from the table
    IReadOnlyList<int> Carved,                              // frame cell indices, reading order
    IReadOnlyList<int> Compartments,                        // burnt (fire) or frozen (icing, asteroid), by index
    WreckBreak? Break, IReadOnlyList<(int X, int Y)> Stones, WreckKind? Cause);
public sealed record WreckBreak(int Column, int Gap, int SternRows, int BowRows);
```

Coordinates are after the whole chain: when a break moves a half, every earlier event's record moves with it (the
mockup's `remapAll`). A compartment split by the break keeps its index in both halves. The client draws looks from this
record alone and never re-runs the damage.

### Forced chains reach the sim as data

`Find(Class, Wreck? = null)` → `Contact(…, Wreck?)` → `ISurfaces.Visit(peer, kind, seed, wreck)` →
`Surface(Id, Kind, Seed, Wreck? = null)`, each new field with an initializer meaning "drawn from the seed", so no save
bump. A forced chain is a list of `(WreckKind, bool Old)`; an empty list forces an intact ship. `Build(cls, seed,
forced)` takes it: a forced step not allowed at its turn is skipped (the dev console says so; the client can run the
same check). `Sim.Open` and `Places.Opened` read it from the surface, so every peer, a late joiner too, builds the same.

### Drawing the chain, in this order

Streams of `Draws(cls, seed, stream)`: describing 0, shape 2 (stream 1, the old damage, is retired); the wreck 3; event
`i`'s geometry `4 + i`; scavenging 9.

1. Wreck or not: `WreckPercent` on stream 3 (skipped when forced).
2. The count, on stream 3, evenly in the class's `Events`.
3. For each step: the allowed set computed on the ship as built so far (the table), one picked evenly on stream 3, then
   its geometry applied on stream `4 + i`. A later event depends on the earlier geometry; an earlier one never on a
   later.
4. The groups, then the last group's age on stream 3.
5. Scavenging on stream 9 if the first group is old.

The intact path draws on stream 3 only for step 1: streams 0 and 2 are unchanged, and a test pins that seeds drawing no
wreck stamp exactly the parts of `main` (the fingerprint of seeds 1–64, `WreckPercent` forced to 0).

### Integers and order

- A disc: `(2x+1−2cx)² + (2y+1−2cy)² ≤ (2r)²` on cell centres; an ellipse scales the two terms by small integer weights.
- A lobed edge: per cell, `r × (88 + Rng.Below(25)) / 100`, rounded down; lobes per eighth of the circle from the Rng.
- Directions: a fixed table of small vectors ((0,1), (1,2), (1,1), (2,1), (1,0) and their signs).
- Cells are walked in reading order; sets are lookups only, never iterated. Ties go by (distance², Y, X).

### Each event on the parts

- **Explosion:** a lobed void of the blast radius round its centre, doubled toward the nearer skin; the armour over it
  moved out one row; decks within 1.2 radii sunk one row where nothing stands; doors within 2.2 radii opened. The
  freighter's stern cut back at its first rib, ragged by up to 2 columns per band, its engines lost.
- **Fire:** a flood from its start compartments through sealed cells and open doors and hatches (shut ones stop it),
  step cost 1, upward ½ (fire climbs), out to the fire's reach; each compartment burnt by its nearest flooded cell,
  intensity `100 × (reach − d) / reach`. If the first compartment has no open door, its first door in reading order is
  opened. In the sim: every crate within 8 columns of the seat (the stove, the life tank, else the room's middle)
  becomes a `pileBits`, whose loot is a bits pile's as anywhere; in every other burnt compartment each stack in a crate
  loses `count × BurntLoss / 100`, rounded down (the rest kept).
- **Icing:** nothing in the sim (looks only), on the latest breach or, first, the whole ship.
- **Asteroid:** the crater (a lobed ellipse, ribs at the skin kept across it), a channel narrowing on along its
  direction, armour round it pushed in one row, decks under it sunk up to two rows where nothing stands; 1–3 shower holes
  of radius 1–2 downrange; stones as `pileRock` salvage piles on the crater floor and in the first shower hole; what
  stood in the crater lost.
- **Break:** every part moved to its half: the stern half `gap / 2` (rounded down) columns sternward and `SternRows`
  rows, the bow half the rest of the gap bowward and `BowRows` rows; a part straddling the cut lost; each band's cut
  ragged by −3..+3 columns, a deck's floor by ±2..4 more; faces torn 0–2 cells deeper per row; deck plates at the faces
  bent down one row. Its cause decides its marks (looks): overload folds and stress cracks, a crater with stones at the
  faces, or heat tint and outward petals.
- **Pulled things,** after every event that carves (explosion, asteroid, break): each loose machine in a compartment a
  breach opens into, in reading order, finds its nearest breach cell; within `reach × 35 / 100` it is lost (small ones 80
  in 100, others 45, on the event's stream), else it slides along its floor toward the breach up to
  `2 + Below(reach / 2)` columns, to the farthest free cell; a slide never lands on another part (occupancy checked), and
  one that finds no free cell stays.
- **Bodies in suits:** 0–2 per event (`BodiesPerEvent`), each in a reached compartment: by the shut door a fire stopped
  at, by a breach, or (a breach) just outside it; never in the arrival room. A `vesselBody` machine, breakable, holding
  a suit's scrap in its slots (`PutIn` and `Caught` in `Opened`, as for crates).
- **Scavenged:** skin panels gone round 2–4 ribs, ribs kept; up to two patches (hull cells set back) over the fate's
  holes. Engines and everything in rooms stay.

The margin round the frame (`Vessels.Margin`, 128) holds the break's shift. No event's geometry touches the arrival room
or a 2-cell ring round it; the break's cut leaves it whole. The spawn is computed after the chain.

### Starting values (`sim/Content`)

| Tunable | Value |
|---|---|
| `WreckPercent` | 0 until W-4, then 60 |
| `FreshPercent` | 50 |
| `VesselClass.Events` | shuttle 1–1, freighter 1–2, liner 1–3, tanker 1–3 |
| Blast radius | shuttle 5, freighter 8, liner 8, tanker 10–12 |
| Rock radius (replaces `Crater`) | shuttle 4, freighter 7, liner 9, tanker 10 |
| Break gap | freighter 8–11, liner 11–16, tanker 12–18 columns |
| Break rows | bow ±2–3; stern 0, or 1 the other way (40 in 100) |
| Fire reach | 48 (shuttle, freighter), 70 (liner, tanker) |
| `BurntLoss` | 50 |
| `BodiesPerEvent` | 0–2 |

### Saving and determinism

A contact and a surface keep class, seed and an optional forced chain; the wreck is built from them on every peer, so
nothing derived is saved and `SaveFormat` stays. A vessel world is never saved. All peers run the same content (the
game's assumption everywhere). The same class, seed and forced chain give the same parts (`VesselTests`).

### Client

- **Looks** (`game/features/wreck/`, plain C# in `logic/` with tests): per event from `Wreck`, in view only: soot and
  smoke tongues, the V at the seat, charred floors, warped and scorched doors, sagging plastic (machines in burnt
  compartments tinted, never new parts); heat tint, beads, streaks; rime, icicles, frosted windows, the ice fan; folds
  and stress cracks (overload); petals and rib stubs at carved edges; hanging cables and pipes; pitting and faded livery
  when scavenged. Old groups dulled. Tiles or a shader per look, never a node per cell.
- **Motion:** particles (dust, paint, ash, ice, fuel crystals, embers, rivets) as one `MultiMeshInstance2D` per kind,
  seeded; drifting debris and furniture outside as decorations (not grabbable); sparks, vents, glints. Lamps through
  `VesselLights`: the stern half blinks red, the bow is dark, one sealed compartment still lit; all dark when the last
  group is old or after power loss.
- **Budget:** a liner with three events in view at 60 fps; particles culled to the view; measured with
  `-- --vessel=liner --wreck=... --frames`.

### Dev

`--vessel=<class> --wreck=blast,fire,rock:old` sends `Find` with that forced chain (`:old` per step, fresh otherwise);
`--wreck` alone lets the seed draw. The dev panel shows a contact's chain.

## Tasks

Each stage is a try the user plays (`godot --path game -- --vessel=liner --wreck=...`), then the next.

### W-1 — The chain and its geometry

- `Build`, `Wreck`, the chain draw and the table's rules, the five events on the parts, the pull, bodies (placeholder
  picture), scavenging, the arrival room kept; every caller listed above on `Build`, `Stamp` internal; `Damage` and
  `VesselCause` removed; the forced chain through `Find`, `Contact`, `Visit` and `Surface`; `pileRock` with art from the
  mockup; Places' `Fuzz` draws forced chains (valid and not), so the generic suites hash wrecks from W-1.
- **Tests:** a scenario per event and per rule (count bounds per class, once-only events, no break on a shuttle, icing
  first only as power loss, icing refused after an asteroid, engines kept by scavengers, fire refused with no sealed
  compartment, break at its cause, asteroid spots three radii apart, bodies only in reached compartments, a forced step
  skipped when not allowed, `BurntLoss` rounding); properties over seeds 1–500 per class and over forced chains of three
  on the liner and tanker: the rules hold, the arrival room and the spawn's door stand, no two parts overlap; the intact
  fingerprint unchanged with `WreckPercent` 0; one test proven to bite.
- Check on a frame: a hero floats across an 18-cell break gap; if not, the gap's upper bound drops to what can be crossed.
- **Done when** the user walks each event's geometry on every class and says yes.

### W-2 — Looks

- The wreck feature's looks per event and group age, the scavenged hull (pits, fade), lamps by event.
- Bodies in suits: candidates drawn on a pick page (lying, slumped by a door, afloat), the pick exported through `tools/`.
- **Done when** the user plays wrecks of each class and the looks match the mockup.

### W-3 — Motion and budget

- Particles, drifters, sparks, vents, glints, embers; the frame budget on the liner with three events.
- **Done when** the budget test passes on a desk and the user plays it.

### W-4 — Wrecks among the contacts

- `WreckPercent` at its value: the radar's contacts include wrecks, shown as their class.
- Ship: ADR `YYYY-MM-DD-vessel-wrecks.md` (the mockup files referenced from it, then removed with this plan), Places README
  ("as built, wrecks to come" replaced by the wreck rules, under 8 KB), map and glossary lines, `vessels.md` V-2 removed.

## Not in this plan

A wreck's look on the radar; hazards aboard (live cables, fire still burning, cold that hurts); ice as water and stones as
ore (Open); debris near the wreck as salvage the net catches; air simulated aboard (rooms stay airless, as today); an
in-game replay of the event (the mockup's replay was a design aid).

## Deliberately not handled

- **A compartment cut off** by a break or a crater (its ladder or door gone): tools break what stands on a vessel, so a
  hero cuts in. No reachability guarantee past the arrival room.
- **The far half of a break** may take a long float or a cut through; its rooms are reached as any cut-off room.
- **The arrival room is always whole,** even where an event would take it: the crew needs a door to dock.
- **No warning before docking** with a burnt or frozen wreck: hazards are out of scope, a wreck is learnt on arrival.
- **Contacts found before W-4** turn into wrecks once `WreckPercent` is on; content changed between `Find` and `Jump`
  gives another wreck: contacts are short-lived (as in `vessels.md`).
- **Decorative drifters are not grabbable:** catching them is the debris net's mechanic, a later plan.
- **A chain may end short of its count** when nothing is allowed (a small ship already burnt through): rare, measured in
  the W-1 property.

## Open

- **Ice and stones for play:** ice as water, stones as ore — the user's call before W-4.
- `WreckPercent`, `BurntLoss` and `BodiesPerEvent` are first values, tuned in play.
