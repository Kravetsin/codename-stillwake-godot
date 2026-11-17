# The liner and the tanker: vessels of rooms several decks tall

- Status: accepted; built; extends [generated-vessels](2026-10-30-generated-vessels.md) and
  [derelict-looks](2026-11-17-derelict-looks.md); changes the radar's even draw of classes ([radar-contacts](2026-11-08-radar-contacts.md))
- Date: 2026-11-17

## Context

Every derelict was a shuttle or a freighter: two or three decks of rooms one deck tall. The user asked for bigger classes,
saw two mockups (the pick, v2: https://claude.ai/artifact/ERaaUh83N6A5SgAoWtsx8D) and asked to build the liner and the
tanker as drawn, then fixed them after playing (2026-11-17).

## Decision

- **Two classes**, last in `Vessels.Classes` so the old classes' seeds are untouched:
  - **Tanker** (4–5 decks, hull 260–340): the bridge over the stern; one fuel column right after the engines, decks 1 to
    the last; tank bays two decks tall (a capsule or two tall tanks) with pump rooms between; outside a manifold on one
    level with a riser and valve over each bay, dome hatches, sometimes spheres under the keel. No hangar.
  - **Liner** (5–6 decks, hull 260–320, light liveries): cabins on deck 0; a pool and a restaurant two decks tall on decks
    1–2, a lounge and a galley over them, an infirmary; a bottom band of two-deck rooms (the stern fuel tank, holds of
    stacked cargo, the bow hangar with one of five shuttles under a bow drawn as a cargo visor); a glazed promenade, a dome
    on it, the bridge forward, a fin with the owner's emblem, escape pods about half gone.
- **Rooms several decks tall:** `Vessel.Compartment.Decks`, no floor across a room between its decks. A big class's
  `Describe` (`VesselClass.Layout`) lays bands of blocks (`Shipyard.Blocks`); the shuttle and freighter keep their own code
  path, pinned by a fingerprint of `Make` over seeds 1–64.
- **Every room reachable:** a door between two blocks on a deck that is the lowest of the room on each side, hull on the
  gap's other decks; two wells of ladders through every floor (past the aft block and by the last block); hatches for raised
  rooms; then a repair pass gives the first unreachable room a hatch to a reachable one above, else below. Hatches are laid
  before furniture, which keeps their columns clear. A big class's bow is solid hull.
- **Interiors by role** (`Shipyard.Fit`, numbers in `Vessels.Fits`); one-deck rooms of old furniture keep ASCII drawings.
  Every new piece is a row with Hp and Gives, no menu card; shuttles and tanks only come apart.
- **Tanks hold liquid:** labelled FUEL, H2O or O2 (`biofuel`, `water`, `canister`); a tank carries its amount in its slots
  (small 5–15, capsule 10–25, a fuel column or the liner's stern tank 15–35, drawn per tank), opened on E or spilled on the
  floor when broken (`Building.HitPile` spills a broken pile's slots).
- **Looks** stay the client's from class and seed (derelict-looks): decor per room role (pool tiles, wainscot, bottles,
  lifebuoy, hangar pad, the visor, bay doors, the fin's emblem), a number 01–99 on every cargo container from `LookRng`.
- **The radar draws a class by `VesselClass.Weight`**: shuttle 1, freighter 3, tanker 4, liner 5 (the user's pick). It can
  **forget** a contact (`Forget`) and **clear the course** (`ClearCourse`), and draws each class's model to scale.

## Consequences

- A liner yields 16,500–26,500 items, a tanker 13,000–23,500; opening one costs a jump tick of about 40–50 ms (the 200 ms pin).
- The liner is now the radar's likeliest find.
- A save with a liner or tanker contact does not load on an older build; older saves load here unchanged.

## Deliberately not handled

- Flying a hangar shuttle; the liquid inside the pool (part of its picture).
- Signs only in one-deck rooms.
- A ladder taken apart can cut a room off; the hero builds one back or cuts through.
- A sphere is left out of a bay whose hull below is not level over both struts.
