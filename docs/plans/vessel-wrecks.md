# Plan: Wrecked vessels, chains of events

## Why

Every vessel on the radar arrives whole today ("wrecks to come", Places README). A wreck turns the same four classes into
places that each read and play differently: a crater is a new way in, an opened room floats, a burnt hold holds less, a
broken ship is two halves. The story is told without words: what happened, in what order, how long ago.

The look was picked on a mockup drawn from real stamped vessels (seed 7 of each class), version 5:
https://claude.ai/artifact/X9dqanpx3SFFVFXDsUt3Z8 — its `fates.js` is the reference for every event's shape below.
This plan replaces V-2 of `vessels.md` (blast, cut in two, scavenged).

## Rules (confirmed with the user)

- A wreck is one of the four classes, built as today, then a **chain of 1–3 events** in time order.
- **Count by class, each count equally likely:** shuttle exactly 1; freighter 1 or 2; tanker and liner 1, 2 or 3.
- **Five events:** explosion, fire, icing, asteroid strike, break. Each next event is drawn evenly from those allowed on the
  ship as it stands after the ones before:
  - **Explosion** at most once: an engine (a tank on the tanker) tears a void; walls bow out; doors near it blown open.
  - **Fire** burns only rooms still sealed and not yet burnt, through open doors, stopped by shut ones. First: it starts in
    the galley (liner) or life support (others). Right after an explosion: in the sealed rooms next to the blast. It may
    repeat while sealed unburnt rooms remain; with none, it cannot be drawn.
  - **Icing** at most once: after a breach not already frozen (explosion, break), round the latest one; as the first
    event, power loss: the hull whole, every room frozen inside, windows frosted, dark.
  - **Asteroid** a crater several rooms deep, decks under it sunk one or two rows, a shower of 1–3 small holes downrange,
    stones of the rock left in the crater. It always freezes its own breach (an asteroid alone reads dull): the icing of
    that crater is part of the strike, not a separate event. Twice at most on the liner and tanker, three crater widths
    apart; once on the others.
  - **Break** at most once, never on the shuttle: the ship split at a rib, the halves moved apart by a gap of whole
    columns and a few whole rows. At an earlier explosion or asteroid if there was one (that is its cause), else overload.
- **Age per event:** the last independent event is fresh or old, evenly; earlier ones are old. An event that directly
  follows its cause shares its time: explosion→fire, a breach→icing, explosion or asteroid→break. Old: dull, sparse,
  no embers. If the first event is old, the hull is pitted, its paint faded, and scavengers took some skin panels (ribs
  left).
- **Rooms no event reached keep everything that stood in them.** Only a room a hole opens into loses loose things (pulled
  to the hole, some out of it); scavengers take nothing from rooms.
- **Engines stay** unless an event destroyed them (an explosion in the engine room, a crater through it). A whole engine
  room with no engine reads wrong.
- **Crew remains: bodies in suits.** Their look is a pick of its own (W-2), on a page of candidates.
- **Radar:** a wreck is learnt on arrival; the radar shows the class as today.

## Design

### One builder for sim and client: `Shipyard.Build`

Today `Places.Opened`, `Sim` (the world's map) and four client views (`VesselDress`, `VesselSigns`, `VesselLights`,
`BeaconLights`) each call `Describe` and `Stamp` themselves. A wreck changes the parts, so all six must see the same ones.
`Shipyard.Build(cls, seed)` returns `Built(Vessel Vessel, Wreck? Wreck, IReadOnlyList<VesselPart> Parts)`: described,
stamped, then damaged by the wreck's events. Every caller moves to it; `Damage(parts, vessel, VesselCause)` and the
unused `VesselCause.Breach` go (the asteroid replaces the breach).

`Wreck` is public, read-only, and is all the client needs to draw looks: `Events` in order, each with `Kind`, `Age`,
its centre, radius, direction (a small integer vector), the cells it carved, the rooms it burnt or froze, the break's
column and offsets, the asteroid's stones. The client never re-runs the damage; it reads the wreck.

### Drawing the chain (`Shipyard.Wreck.cs`)

- Whether a contact is a wreck: `Vessels.WreckPercent` of contacts, drawn from the seed's own stream (stream 3).
- The count, then each event from the allowed ones, then ages: stream 3. Each event's own geometry: stream `4 + index`,
  so editing one event never moves another (the mockup's rule).
- Tunables in `sim/Content`: `VesselClass.Events` (min, max), `WreckPercent`, `FreshPercent` (50), per class the blast
  radius, the rock radius (today's `Crater`), the break gap range; the fire's reach; shower count 1–3.

### Each event on the parts, in integers

All distances are squared integers; a direction is one of a fixed table of small integer vectors; lobes and ragged edges
are drawn per cell from the Rng (the mockup's float hashes become `Rng.Below`). Rounding: radii down.

- **Explosion:** a lobed void round the engine or tank, opened through the nearer skin; the armour over it moved out one
  row; decks within reach sunk one row; loose things slid away from it; doors within reach opened. The freighter's stern
  cut back at its first rib, its engines lost.
- **Fire:** a flood from the start rooms through sealed cells and open doors (shut doors and hatches stop it), each room
  burnt by distance. In the sim: crates in the seat's room become a bits pile, other crates in burnt rooms lose a share
  of what they hold (`Vessels.BurntLoss`), the origin's shut door to a neighbour opened if it had none open.
- **Icing:** nothing in the sim. It is looks only (rime, icicles, frost, crystals), also laid by every asteroid round its
  own crater.
- **Bodies in suits:** 0–2 per event in rooms it reached (by a shut door a fire stopped at, near a hole, afloat outside a
  breach), none in the arrival room; a part of their own (a `vesselBody` piece, breakable for a suit's scrap). Count and
  loot are tunables in `sim/Content`.
- **Asteroid:** the crater carved (ribs at the skin kept across it), armour round it pushed in one row, decks under it
  sunk up to two rows where nothing stands, the shower's small holes, stones as `salvage` piles (a `pileRock` kind: new
  art, the mockup's stone) lodged in the crater floor; what stood in the crater lost.
- **Break:** every part moved to its half (columns ± half the gap, rows by the half's offset); parts straddling the cut
  lost; faces torn one or two cells deeper per row; deck plates at the faces bent down one row. Everything earlier
  events left moves with its half (the mockup's `remapAll`).
- **Pulled things:** loose machines in a room a hole opens into slide toward it, the nearest small ones lost through it.
- **Scavenged (old first event):** skin panels gone round 2–4 ribs, ribs kept; up to two crude patches over holes.
  Engines and everything in rooms stay.

The margin round the frame (`Vessels.Margin`, 128) holds the break's shift; the frame stays as described.

### The way in

The spawn is the first door in reading order (`Door`). No event carves the arrival room or its door, and the break moves
the spawn with its half: `Door(built)` is computed after the wreck. A crew always docks at a whole airlock.

### Saving and determinism

A contact keeps its class and seed; the wreck is a function of both, so nothing new is saved and `SaveFormat` stays. A
vessel world is never saved (it closes on load). Same class and seed give the same parts on every peer (`VesselTests`
extended to wrecks). The intact path is untouched: the existing fingerprint (seeds 1–64) still holds for seeds that draw
no wreck, and a new one pins wrecked seeds per class.

### Client

- **Looks** (`game/features/wreck/`, logic in `logic/`, plain C# with tests): per event, from `Wreck`, drawn over the hull
  and rooms in view only: soot and smoke tongues, the V at the seat, charred floors, warped and scorched doors, sagging
  plastic (machines in burnt rooms tinted, never new parts); heat tint, beads, streaks; rime, icicles, frosted windows,
  the ice fan; folds and stress cracks (overload); petals and rib stubs at every carved edge; hanging cables and pipes;
  pitting and faded livery when old. Old events dulled. Tiles or a shader per look, never a node per cell.
- **Motion:** particles (dust, paint, ash, ice, fuel crystals, embers, rivets) as one `MultiMeshInstance2D` per kind,
  deterministic from the seed; drifting debris and furniture outside as decorations (not grabbable); sparks, vents,
  glints. Embers and blinking red emergency lamps through `VesselLights` (lamp states by event: the stern half blinks,
  the bow is dark, one sealed room still lit; all dark when the last event is old or after power loss).
- **Budget:** a liner with three events in view at 60 fps; particles culled to the view; measured with
  `-- --vessel=liner --wreck=... --frames`.

### Dev

`--wreck=blast,fire,rock:old` with `--vessel=<class>` forces a chain (ages `fresh` by default, `:old` per event);
`--wreck` alone draws one from the seed. The dev panel's contact list shows a wreck's chain.

## Tasks

Each stage is a try the user plays (`godot --path game -- --vessel=liner --wreck=...`), then the next.

### W-1 — The chain and its geometry

- `Shipyard.Build`, `Wreck`, the chain draw and its rules, the five events on the parts, the pull, scavenging, the
  arrival room kept; every caller on `Build`; `Damage`/`VesselCause` removed; `pileRock` piece with art from the mockup;
  bodies placed as `vesselBody` with a placeholder picture until W-2's pick.
- **Tests:** a scenario per event and per rule (count bounds per class, once-only events, no break on a shuttle, icing
  first only as power loss, icing refused right after an asteroid, engines kept by scavengers, fire refused with no
  sealed room, break at its cause, asteroid twice apart, bodies only in rooms an event reached); a property over
  seeds 1–500 per class that the rules hold, the arrival room is whole and its door stands; same seed → same parts; the
  intact fingerprint unchanged; a test proven to bite.
- Check on a frame: a hero crosses a 20-cell break gap in zero-g; if not, the gap's upper bound drops to what can be crossed.
- **Done when** the user walks each event's geometry ("только геометрия") on every class and says yes.

### W-2 — Looks

- The wreck feature's looks per event and age, the old hull (pits, fade), lamps by event.
- Bodies in suits: candidates drawn on a pick page (lying, slumped by a door, afloat), the pick exported through `tools/`.
- **Done when** the user plays wrecks of each class and the looks match the mockup.

### W-3 — Motion and budget

- Particles, drifters, sparks, vents, glints, embers; frame budget on the liner with three events.
- **Done when** the budget test passes on a desk and the user plays it.

### W-4 — Wrecks among the contacts

- `WreckPercent` on: the radar's contacts include wrecks, shown as their class; the wreck is learnt on arrival.
- Ship: ADR `YYYY-MM-DD-vessel-wrecks.md`, Places README (the wreck rules, under 8 KB), map and glossary lines,
  `vessels.md` V-2 removed.

## Not in this plan

A wreck's look on the radar; hazards aboard (live cables, fire still burning, cold that hurts); ice as water and stones as ore (Open);
debris near the wreck as salvage the net catches; air simulated aboard (rooms stay empty of air, as today); an in-game
replay of the event (the mockup's replay was a design aid).

## Deliberately not handled

- **A room cut off by the break or a crater** (its ladder or door gone): tools break what stands on a vessel, so a hero
  cuts in. No reachability guarantee past the arrival room.
- **The arrival room is always whole,** even when the event's centre would take it: the crew needs a door to dock.
- **Content changed between `Find` and `Jump`** gives the contact another wreck: contacts are short-lived (as in
  `vessels.md`).
- **Decorative drifters are not grabbable:** catching them is the debris net's mechanic, a later plan.

## Open

- **Ice and stones for play:** ice as water, stones as ore — the user's call before W-4.
- `WreckPercent` and `BurntLoss`: a first pass in W-1, tuned in play.
