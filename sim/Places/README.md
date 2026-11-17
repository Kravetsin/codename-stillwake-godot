# Places

## Files

| File | Purpose |
| --- | --- |
| `Places.cs` | The radar's contacts, found by a timer or by hand; the course the shuttle flies and its set off; the jump that opens a contact's vessel world; the vessel written there. |

The places the radar finds and the way to them ([generated-vessels](../../docs/decisions/2026-10-30-generated-vessels.md),
[vessel-worlds](../../docs/decisions/2026-11-09-vessel-worlds.md)): the contacts, aboard, and the jump that opens a surface of
its own for a contact's vessel and moves its sender there. The vessel is built by `Shipyard.Build` (sim/Core `Vessels/`:
described, stamped, then wrecked, `Hulk`), from `Content.Vessels` and `Content.Wrecks`; the surface's kind is the class's `VesselKind`.

- **Depends on:** `Grid` (`IGridQueries`: the size, `MachineAt`, `BackAt`; `IGridOps.Lay`), `Items` (`IItemsOps.PutIn`,
  `Caught`: a crate's loot), `Crew` (`ICrewQueries.HeroOf`), `Landing` (`ILandingQueries.Rides`, `SetOffProblem`;
  `ILandingOps.SetOff`, `SetOffHome`); the world's `ISurfaces` (`Visit`, `Seed`).
- **State** (`PlacesState`): `Contacts` (`Contact(Id, Class, Seed, Offset, Left, Wreck)`, in the order found, at most
  `Vessels.MaxContacts`), `NextContactId`, `NextIn` (ticks till the radar's own next; 0 before the first). Saved. A vessel
  world's cells are its grid's, saved with its surface while open. `Course` (a contact's id; unsaid while none).

## Rules

- Aboard only: off the ship's surface `Find`, `Jump`, `SetCourse`, `ClearCourse` and `Forget` are refused `places.notAboard`.
- **The radar's own contacts** ([radar-contacts](../../docs/decisions/2026-11-08-radar-contacts.md)), aboard, each tick: `Left`
  down by one, gone at 0; a countdown from `Contacts.First` (15 min); at 0 a contact of a class drawn by `Weight` (while under
  `MaxContacts`), the next in `EveryMin..EveryMax`. `Lasts` ≤ `EveryMin`: never two at once.
- **A contact added** (by the radar or `Find(Class, Wreck)`, the dev flag `--vessel` and the dev panel's): its seed drawn from the
  world's Rng, then its side and its distance in `Contacts.Nearest..Reach`, each evenly; `Offset` the distance signed (minus
  left of the ship); `Left` = `Contacts.Lasts`. The client draws it on the radar, where Set course sends `SetCourse`; `Jump` is the dev panel's.
- **The course** ([shuttle-flight](../../docs/decisions/2026-11-16-shuttle-flight.md)): one per world, `SetCourse(Contact)`
  by anyone aboard, the last of a tick winning; cleared by `ClearCourse`, when its contact runs out, is jumped to or forgotten
  (`Forget(Contact)`: off the radar for everyone), and on set off.
  `CostOf(contact)`: litres for the round trip, each leg `|Offset| × KmPerUnit` km over `Shuttle.KmPerLitre`, rounded up;
  aboard, `SetOff` burns it from the shuttle's store (`landing.noFuel` short of it); the return burns nothing.
- **`SetOff`**, from a shuttle's rider: aboard `places.noCourse` (none, or its contact gone) first, then Landing's
  `SetOffProblem`. Aboard the contact is used up, the course cleared and Landing flies the shuttle to a world of the class's
  kind from the contact's seed; off the ship it flies home.
- **`Jump(Contact)`**, as its command applies: refused `places.noContact` for a contact not held, `places.noHero` for a sender
  with no hero (its seat would hold the world open with no farewell to close it), `places.seated` for one in a drop pod or
  flying (its pod would follow). Else the contact is used up and `ISurfaces.Visit` opens a surface of the class's kind
  (`vessel.<class>`) from the contact's seed, and moves the sender there at once, to its spawn just inside the door. Every
  jump opens a new world; a friend joins by `Transfer` and arrives at the same door.
- **The vessel world** (Core's `World.Open`): space, the frame (`Vessel.Width` × `Height`) and `Vessels.Margin` round it, the
  frame's top-left at (`Margin`, `Margin`), the spawn at `Shipyard.Door`. Once every module stands, Places' `Opened` writes
  the vessel there: its parts laid (`Grid.Lay`) in reading order of their top-left cells, so machine ids follow it, the
  spawn's own back wall kept, an open door's piece turned open (`SetDoorway`); its crates filled (`PutIn`) and their loot
  counted obtained (`Caught`, the-board), a tank's liquid alike ([big-vessels](../../docs/decisions/2026-11-17-big-vessels.md)).
  Furniture, salvage piles (`Content.Vessels.Salvage`) and outside parts ([derelict-looks](../../docs/decisions/2026-11-17-derelict-looks.md)).
- **Wrecks** ([vessel-wrecks](../../docs/decisions/2026-11-18-vessel-wrecks.md)): `WreckPercent` of contacts, drawn from the
  seed, are wrecked by a chain of 1–3 events (by class: shuttle 1, freighter 1–2, liner and tanker 1–3), each next drawn evenly
  from those allowed: explosion, fire, icing, asteroid, break. A forced chain (`Find`'s `Wreck`, carried by the contact, the
  voyage and `Surface.Wreck`) replaces the draw, a step not allowed at its turn skipped; an empty one is intact. The arrival
  room and two cells round it are never touched; engines stay unless carved; rooms not reached keep everything. The radar
  shows only the class. `Opened` lays `Build`'s parts, bodies' scrap put in as crates' loot.
- **Its kind's rules** (`VesselKind`): the ship's space (no gravity in the open, vacuum, starlight), no debris, no meteors;
  built as a planet allows, no machines; tools break what stands, nothing is marked (`ToolsBreak`), as on a planet.
- **It closes** at the end of the tick no peer stands on it (`Kind.Transient`, Core): gone home, died and woke aboard, or left
  the game. Everything there is erased; its id is never reused. A load in the game seats everyone aboard, so it closes then.

## API

- **Queries:** `Contacts`, `NextIn`, `Course`, `CostOf` (static), `SetOffProblem(peer)`.
- **Commands:** `Find(Class, Wreck)` — `places.notAboard`, `places.noClass`, `places.full`. `Jump(Contact)` — `places.notAboard`,
  `places.noContact`, `places.noHero`, `places.seated`. `SetCourse(Contact)` — `places.notAboard`, `places.noContact`.
  `ClearCourse` — `places.notAboard`. `Forget(Contact)` — `places.notAboard`, `places.noContact`. `SetOff` — `places.noCourse`, then Landing's.
- **Fuzz:** now and then, a jump opening a whole surface: `Find` of a class or none, half with a forced chain of up to four
  steps (allowed or not), `Jump`, `SetCourse` or `Forget` of a contact held or not, `ClearCourse`, `SetOff`.

## Invariants

- A contact keeps `1 ≤ Left ≤ Lasts`, `Nearest ≤ |Offset| ≤ Reach`; the radar's own come one at a time (`RadarContactsTests`).
- A jump changes nothing of the ship's grid; the vessel there is what `Shipyard.Build` makes of the contact's class, seed and
  forced chain (`PlacesTests`, `VesselWreckTests`): the table's rules hold, the arrival room stands, no two parts overlap.
- No vessel world is open with nobody on it after a tick, and a closed one never opens again (`PlacesTests`, a property).
- The same class and seed make the same vessel on every peer, intact and wrecked (`VesselTests`).

## Deliberately not handled

- Where a contact lies changes nothing yet: the offset is the radar's.
- No radar needed: contacts come whether a radar is built or not (the computer's rules live in the client).
- What is left on a vessel world is erased with it; only what a hero carries home comes home (the user's ruling).
- No revisit: the contact is used up by the jump. Quitting on a vessel loses it.
- A crate's loot counts as obtained when the vessel opens, taken home or not: counting what a hand takes out would need Items
  to know a vessel's crates.
- The jump's tick is long, like a load (`PlacesTests` pins 200 ms).
- Content changed between `Find` and `Jump` makes a pending contact come out differently; a written vessel is saved, never
  regenerated.
- Air and power in a vessel: its rooms start empty; its lamps burn as a look of the client, on no power of the sim.
- A wreck's hazards (live cables, fire still burning, cold) and the debris near it as salvage; a room cut off by a break or a
  crater is reached by cutting in.
