# Places

## Files

| File | Purpose |
| --- | --- |
| `Places.cs` | The radar's contacts, found by a timer or by hand; the course the shuttle flies and its set off; the jump that opens a contact's vessel world; the vessel written there. |

The places the radar finds and the way to them ([generated-vessels](../../docs/decisions/2026-10-30-generated-vessels.md),
[vessel-worlds](../../docs/decisions/2026-11-09-vessel-worlds.md)): the contacts, aboard, and the jump that opens a surface of
its own for a contact's vessel and moves its sender there. The vessel itself is described and stamped by `Shipyard` (sim/Core
`Vessels/`), from `Content.Vessels`; the surface's kind is the class's `VesselKind`.

- **Depends on:** `Grid` (`IGridQueries`: the size, `MachineAt`, `BackAt`; `IGridOps.Lay`), `Items` (`IItemsOps.PutIn`,
  `Caught`: a crate's loot), `Crew` (`ICrewQueries.HeroOf`), `Landing` (`ILandingQueries.Rides`, `SetOffProblem`;
  `ILandingOps.SetOff`, `SetOffHome`); the world's `ISurfaces` (`Visit`, `Seed`).
- **State** (`PlacesState`): `Contacts` (`Contact(Id, Class, Seed, Offset, Left)`, in the order found, at most
  `Vessels.MaxContacts`), `NextContactId`, `NextIn` (ticks till the radar's own next; 0 before the first). Saved. A vessel
  world's cells are its grid's, saved whole with its surface while it is open. New to save format 57; `Offset`, `Left` and
  `NextIn` to 59 (a held contact got the nearest place, right, and 15 min). `Course` (a contact's id; unsaid while none).

## Rules

- Aboard only: off the ship's surface `Find`, `Jump` and `SetCourse` are refused `places.notAboard`; go home first.
- **The radar's own contacts** ([radar-contacts](../../docs/decisions/2026-11-08-radar-contacts.md)), aboard, each tick in
  simulation: every contact's `Left` goes down by one and one at 0 is gone; then the countdown, started at `Contacts.First`
  (15 min, fixed, so a short run never draws); at 0 a contact of a class drawn evenly is added (while fewer than
  `MaxContacts`) and the next drawn evenly in `EveryMin..EveryMax`. `Lasts` ≤ `EveryMin`: never two of its own at once.
- **A contact added** (by the radar or `Find(Class)`, the dev flag `--vessel` and the dev panel's): its seed drawn from the
  world's Rng, then its side and its distance in `Contacts.Nearest..Reach`, each evenly; `Offset` the distance signed (minus
  left of the ship); `Left` = `Contacts.Lasts`. The client draws it on the radar, where Set course sends `SetCourse`; `Jump` is the dev panel's.
- **The course** ([shuttle-flight](../../docs/decisions/2026-11-16-shuttle-flight.md)): one per world, `SetCourse(Contact)`
  by anyone aboard, the last of a tick winning; cleared when its contact runs out or is jumped to, and on set off.
  `CostOf(contact)`: litres for the round trip, each leg `|Offset| × KmPerUnit` km over `Shuttle.KmPerLitre`, rounded up;
  shown only, a stub till fuel-liquid.
- **`SetOff`**, from a shuttle's rider: aboard `places.noCourse` (none, or its contact gone) first, then Landing's
  `SetOffProblem`. Aboard the contact is used up, the course cleared and Landing flies the shuttle to a world of the class's
  kind from the contact's seed; off the ship it flies home.
- **`Jump(Contact)`**, as its command applies: refused `places.noContact` for a contact not held, `places.noHero` for a sender
  with no hero (its seat would hold the world open with no farewell to close it), `places.seated` for one in a drop pod or
  flying (its pod would follow). Else the contact is used up and `ISurfaces.Visit` opens a surface of the class's kind
  (`vessel.<class>`) from the contact's seed, and moves the sender there at once, to its spawn just inside the door. Every
  jump opens a new world: two players jumping get two; a friend joins by `Transfer` and arrives at the same door.
- **The vessel world** (Core's `World.Open`): space, the frame (`Vessel.Width` × `Height`) and `Vessels.Margin` round it, the
  frame's top-left at (`Margin`, `Margin`), the spawn at `Shipyard.Door`. Once every module stands, Places' `Opened` writes
  the vessel there: its parts laid (`Grid.Lay`) in reading order of their top-left cells, so machine ids follow it, the
  spawn's own back wall kept; its crates filled (`PutIn`) and their loot counted obtained (`Caught`, the-board). Compartments
  hold salvage, piles copied from drifting debris (`Content.Vessels.Salvage`, five times a piece's mean loot, broken by tools
  as any pile). A vessel is as built, wrecks to come (the user's word).
- **Its kind's rules** (`VesselKind`): the ship's space (no gravity in the open, vacuum, starlight), no debris, no meteors;
  built as a planet allows, no machines; tools break what stands, nothing is marked (`ToolsBreak`), as on a planet.
- **It closes** at the end of the tick no peer stands on it (`Kind.Transient`, Core): gone home, died and woke aboard, or left
  the game. Everything there is erased; its id is never reused. A load in the game seats everyone aboard, so it closes then.

## API

- **Queries:** `Contacts`, `NextIn`, `Course`, `CostOf` (static), `SetOffProblem(peer)`.
- **Commands:** `Find(Class)` — `places.notAboard`, `places.noClass`, `places.full`. `Jump(Contact)` — `places.notAboard`,
  `places.noContact`, `places.noHero`, `places.seated`. `SetCourse(Contact)` — `places.notAboard`, `places.noContact`.
  `SetOff` — `places.noCourse`, then Landing's.
- **Fuzz:** now and then, a jump opening a whole surface: `Find` of a class or none, `Jump` or `SetCourse` to a contact held
  or not, `SetOff`.

## Invariants

- A contact keeps `1 ≤ Left ≤ Lasts`, `Nearest ≤ |Offset| ≤ Reach`; the radar's own come one at a time (`RadarContactsTests`).
- A jump changes nothing of the ship's grid; the vessel there is what `Shipyard` stamps for the contact's class and seed
  (`PlacesTests`).
- No vessel world is open with nobody on it after a tick, and a closed one never opens again (`PlacesTests`, a property).
- The same class and seed make the same vessel on every peer, intact and wrecked (`VesselTests`).

## Deliberately not handled

- Where a contact lies changes nothing yet: the offset is the radar's.
- No radar needed: contacts come whether a radar is built or not (the computer's rules live in the client).
- What is left on a vessel world is erased with it: a body and the inventory it held, crates not emptied, plans. Coming back
  for it needs the world kept, which the user ruled out; only what a hero carries home comes home.
- No revisit: the contact is used up by the jump. Quitting on a vessel loses it.
- A crate's loot counts as obtained when the vessel opens, taken home or not: counting what a hand takes out would need Items
  to know a vessel's crates.
- The jump's tick is long: a registry made and a freighter written, once per visit, like a load (`PlacesTests` pins 200 ms).
- Content changed between `Find` and `Jump` makes a pending contact come out differently; a written vessel is saved, never
  regenerated.
- Air and power in a vessel: its rooms start empty, its lamps dead.
- The soil clod aboard wears its drifting picture, a little above the deck: the user did not choose it to be drawn whole.
