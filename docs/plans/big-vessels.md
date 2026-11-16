# Plan: the liner and the tanker

The user approved [the mockup](big-vessels-mockup.html) (v2, https://claude.ai/artifact/ERaaUh83N6A5SgAoWtsx8D) and asked to
implement both classes (2026-11-17). The design, [big-vessels-design.md](big-vessels-design.md), is background: where this plan
speaks, it wins; where it is silent, the design stands. Superseded points are listed under Changes from the design.

**Words.** Decks are numbered from 0 at the top, as in code (the user's "decks 2–3" are decks 1–2). A *tall room* spans
several decks with no floor between. A *band* is a run of decks with its own blocks; a *block* is a width and a stack of rooms
covering the band. The *service block* is the one-deck block holding the well, right after the aft block (the tanker's comes
after its fuel bay). A *raised room* stands above the hull (deck −1; the dome deck −2). *Reading order* is by (top, left).

## Rules (confirmed with the user, 2026-11-17)

- **Tanker**: 4–5 decks, hull 260–340 columns. The bridge raised over the stern. Right after the engine rooms one huge
  vertical FUEL tank spanning decks 1 to the last (3 decks on a 4-deck tanker, 4 on a 5-deck one). Then tank bays two decks
  tall (one horizontal capsule or two vertical tanks, contents other than fuel), pump rooms between. A dorsal manifold with
  a riser and valve over each bay, dome hatches; sometimes spherical tanks under the keel, both struts on the hull; a sensor
  boom; a radiator. No hangar and no shuttle: shuttles are the liner's only (the user, 2026-11-17).
- **Liner**: 5–6 decks, hull 260–320, a light livery. The bottom two decks are one band of two-deck rooms only: at the stern
  one big fuel tank by the engines, in the middle holds with cargo containers stacked 2–3 high and crate stacks, at the bow
  always a hangar with a shuttle, the bow over it drawn as a hinged cargo visor (a look; the bow is solid hull). Deck 0 is cabins. Decks 1–2 always hold a pool and a restaurant, each two decks tall
  (pool: half-drained murky water with an old waterline, tiles, ladder, diving board, lounger, lifebuoy; restaurant:
  wainscot, bar with bottle shelves, tables with cloths and chairs, a grand piano, two chandeliers); a lounge above the pool,
  a galley above the restaurant; an infirmary among the public rooms. A promenade with a glazed roof, a dome on it, the
  bridge forward with a housing; a fin with the owner's emblem; escape pods along the top, about half gone (empty cradles).
- Every room is reachable from the airlock: a door between two rooms where both floors meet, otherwise a hatch with a ladder.
  Two wells on every big vessel, ladders down through every floor: past the aft block and by the last block's right, the
  liner's hold band reached by both.
- A hangar holds one of five shuttles (GULL, MULE, BEETLE, WHALE, HERON) and a fuel cart; shuttles only come apart.
- Every tank is labelled FUEL, H2O or O2 (no other liquid) and, taken apart, spills its liquid on top of what it gives: biofuel,
  water, canisters; a tall tank or a sphere 5–15, a capsule 10–25, a fuel column or the liner's fuel tank 15–35, drawn per
  tank. The pool's water is part of its picture.
- Every cargo container shows a stencilled number 01–99, drawn by the client from the seed.
- **The radar draws classes by weight**: shuttle 4, freighter 3, tanker 1, liner 1 (the user's pick), tunable content.
- **The mockup is the art pick** for every new picture, as for the derelict looks; fixed in game where the user asks.
- As the derelict-looks ADR rules: intact abandoned, damage inside only, everything a hero takes apart, looks from class and
  seed, names and codes in Latin.
- Unchanged: the shuttle and freighter, how a vessel world opens and closes, `Find(Class)`.

## Changes from the design

- No planner split: `Describe` branches on `VesselClass.Layout`; the shuttle and freighter keep today's code path, checked by
  a fingerprint of `Make` over seeds 1–64 recorded before any change. Liner and tanker share new helpers.
- No bulkhead list and no `SectionKind.Tier` unless `Outfit` needs it.
- Interiors of the new roles by small per-role routines (numbers in `Vessels.Fits`), not ASCII; one-deck rooms of existing
  furniture keep ASCII drawings.
- One capsule row with five looks, one tall-tank row with four; the manifold's valve is drawn on the riser's top (no
  `vesselValve` row; the riser's Gives cover it). `vesselValves` is the pump room's wall panel, inside.
- Decor (pool tiles, wainscot, bottle shelves, lifebuoy, hangar pad, bay doors, the fin's emblem) is drawn by `VesselDress` per
  room role while its cells stand; no new client piece.
- Performance: measured, fixed only where it misses. Three stages instead of five.

## Shape (sim)

- `Vessel.Compartment.Decks` (1 by default): height `Pitch × Decks − 1`, no deck floor across its columns between its decks.
- `VesselRole` gains: pool, restaurant, lounge, galley, infirmary, hangar, tank bay, fuel bay, pump room, hold.
- **Doors:** between two neighbouring blocks, on deck d, a door when d is the lowest deck of the room on each side (their
  floors on one row). Any other deck of the gap is hull: a tall room's upper decks face hull.
- **Hatches before furniture:** the well (a hatch at the service block's left in every floor where the rooms above and below
  differ), the raised rooms' hatches, then the repair pass: while a room is unreachable from the airlock (a breadth-first walk
  over the compartment list in reading order, neighbours by doors and hatches, a set only for "seen"), the first unreachable
  room in reading order gets a hatch to a reachable room directly above, else below, at the right end of their shared
  columns minus two. None fitting throws (our bug; a sweep of seeds catches it). Furniture is placed after and keeps every
  hatch's and ladder's two columns clear, and two walkway columns at each room's left.
- Ladders are two-row pieces down the lower room; a leftover row is a one-cell step (the hero steps one cell, 0106).
- **Draw streams:** stream 0 in this order: the layout (block widths and roles, then door states), then each room's furniture
  in reading order (holds' stacks, tank looks from the non-fuel looks only, the hangar's shuttle model). Stream 2: modules,
  the bow, then outside parts in a fixed order written in `Shipyard`'s summary. Odds are integers (one in N, or whole
  percent rounded down) in `Vessels.Fits`. New classes at the end of `Vessels.Classes`.
- **Weights:** `VesselClass.Weight` (shuttle 4, freighter 3, tanker 1, liner 1). The radar's own timer draws one
  `Below(sum of weights)` from the world Rng where it now draws `Below(Classes.Count)`, then walks `Vessels.Classes` in
  order; weight 0 is never drawn; a content test keeps the sum above 0. `Find(Class)` is unchanged. Contacts are saved with
  their class, so old contacts keep theirs.

## Rows (sim/Content; Gives from existing items, tunable; no menu card, no build cost)

Inside: `vesselBar`, `vesselPiano`, `vesselRoundTable`, `vesselChair`, `vesselLounger`, `vesselDiving`, `vesselPool`,
`vesselChandelier` (a lamp of the vessel's), `vesselStove`, `vesselCounter`, `vesselPump`, `vesselValves`, `vesselBed`,
`vesselMedCab`, `vesselFuelCart`, `vesselCapsule` (5 looks), `vesselTankTall` (4 looks), `vesselFuelColumn` (one size per span,
3 and 4 decks), `vesselShuttleGull`, `…Mule`, `…Beetle`, `…Whale`, `…Heron`. Outside: `vesselFin`, `vesselPod`, `vesselCradle`,
`vesselManifold`, `vesselRiser`, `vesselDome`, `vesselSphere`. Reused: `vesselLamp`, `vesselCargo`, crates, `salvageVending`
(the galley's fridge), rack, console, seat, bunks, locker, table, `vesselPylon`, `vesselRadiator`, `vesselBoom`, mast, dish.
Footprints and yields start from the design's two tables ("New furniture rows", "New outside rows"). Pictures in
`tools/anime-art/subjects/vessel.js` after the mockup's `drawItem`, `drawPart` and `SHUTTLE_DRAW`.

## Stages (each a try the user plays)

1. **The liner, whole inside:** bands, tall rooms, doors, hatches, raised rooms with glass, light livery; pool, restaurant,
   lounge, galley, infirmary, cabins, holds, fuel tank, hangar with the five shuttles, decor; the fingerprint, the seed sweep,
   the jump-tick pin.
2. **The tanker, whole inside:** stern bridge, fuel column, tank bays, pump rooms.
3. **Both outsides and the radar:** fin and emblem, pods and cradles, manifold, risers, domes, spheres, boom, radiators, bay
   doors, signs in one-deck rooms; class weights; yields measured; frame budget measured on `--vessel=liner`.

Ship once the user has played all three: the ADR (the multi-deck mechanic, the two classes, the weights), the Places README
(its "a class drawn evenly" line), map, glossary, the full suite, a PR.

## Tests

- Scenarios: a door only where floors meet, hull in the gap's other decks; the well and a repair hatch; a ladder down a tall
  room; each role's furniture present; tank looks never fuel in a bay; a shuttle in every liner hangar; the airlock shut;
  the jump tick (`PlacesTests` pins 200 ms) for both classes; the old classes' fingerprint (seeds 1–64); seeds 1–512 of the
  liner and the tanker describe and stamp without throwing; the weights' draw; every new row broken by a tool gives its Gives.
- Invariants (FsCheck, all four classes): every room reachable; every hatch with a ladder under it; no hatch or ladder cell
  under a furniture part; parts never overlap and stay within `Vessel.Width`/`Height`; glass touches space; the same seed
  gives the same vessel. Prove one bites (a liner with its repair hatch removed is unreachable).
- Client: looks pinned for one liner and one tanker seed (also pinning the draw order); decor hidden once any of its cells
  changes, including only one deck of a tall room.

## Deliberately not handled

- Liquid as a fluid in tanks, spheres or the pool (a tank spills items, opened by E as a crate is); flying a hangar shuttle; no "use" text on them: the pointer names them, a tool takes
  them apart.
- Signs only in one-deck rooms: tall halls are already the richest.
- A ladder taken apart can cut a room off (the well is the only way into the hold band); ladders are buildable on a vessel
  world, so the hero builds one back or cuts through.
- Saves only go forward: one made on this branch with a liner or tanker contact breaks on an older build; an older save
  loads here unchanged (new rows and class strings are additive).
