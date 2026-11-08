# Places

## Files

| File | Purpose |
| --- | --- |
| `Places.cs` | The radar's contacts, found by a timer or by hand, and the jump that writes a generated vessel beside the ship. |

Where the ship is and what lies beside it ([generated-vessels](../../docs/decisions/2026-10-30-generated-vessels.md)): the
contacts the radar found, and the jump that writes a vessel into the grid beside the ship, the last one cleared. The vessel
itself is described, stamped and wrecked by `Shipyard` (sim/Core `Vessels/`), from `Content.Vessels`.

- **Depends on:** `Grid` (`IGridQueries`: `Built`, `Reserved`, the layers; `IGridOps.Reserve`, `Lay`), `Items`
  (`IItemsOps.PutIn`: a crate's loot), `Building` (`IBuildingOps.Wipe`: the old vessel taken apart to nothing).
- **State** (`PlacesState`): `Contacts` (`Contact(Id, Class, Seed, Offset, Left)`, in the order found, at most
  `Vessels.MaxContacts`), `NextContactId`, `NextIn` (ticks till the radar's own next; 0 before the first). Saved.
  The vessel's cells are the grid's, its box the grid's `Reserved`. New to save format 57; `Offset`, `Left` and `NextIn`
  to 59 (a held contact got the nearest place, right, and 15 min).

## Rules

- Aboard only: off the ship's surface `Find` and `Jump` are refused `places.notAboard`.
- **The radar's own contacts** ([radar-contacts](../../docs/decisions/2026-11-08-radar-contacts.md)), aboard, each tick in
  simulation: every contact's `Left` goes down by one and one at 0 is gone; then the countdown, started at `Contacts.First`
  (15 min, fixed, so a short run never draws); at 0 a contact of a class drawn evenly is added (while fewer than
  `MaxContacts`) and the next drawn evenly in `EveryMin..EveryMax`. `Lasts` ≤ `EveryMin`: never two of its own at once.
- **A contact added** (by the radar or `Find(Class)`, the dev flag `--vessel` and the dev panel's): its seed drawn from the
  world's Rng, then its side and its distance in `Contacts.Nearest..Reach`, each evenly; `Offset` the distance signed (minus
  left of the ship); `Left` = `Contacts.Lasts`. The client draws it on the radar, where Set off sends `Jump`.
- **`Jump(Contact, Wrecked)`**, in order: the box is found first (below); none fits, `places.noRoom`, and nothing changes.
  Else the old vessel's box is wiped (`Building.Wipe`: machines emptied and unplugged, every thing cleared, its plans and
  cracks dropped, nothing given), the new box reserved (`Grid.Reserve`), the vessel's parts laid (`Grid.Lay`) in reading
  order of their top-left cells, so machine ids follow it, its crates filled (`PutIn`) and their loot counted obtained
  (`Caught`, the-board), and the contact dropped. Compartments hold salvage, piles copied from drifting debris (`Content.Vessels.Salvage`, five
  times a piece's mean loot, broken by tools as any pile). A vessel is
  as built, wrecks to come (the user's word); `Wrecked`, sent only by dev tools, breaks it by a breach meanwhile.
- **The box:** `Vessels.Gap` columns right of the ship, the tight box of everything built but the vessel (`Grid.Built` with
  none out); its rows centred on the ship's, rounded down, moved inside the grid by `Vessels.EdgeClear`. On the game's world
  it always fits: nothing is built outside the zone, and a frame is at most 448 wide.
- Elsewhere the vessel is not the ship: Debris' stream and Hazards' meteors pass it by, Crew keeps its box warm, Building's
  axe and drill break it, and Grid, Pipes and Power refuse building in it (`grid.reserved`). Each module's README says so.

## API

- **Queries:** `Contacts`, `NextIn`.
- **Commands:** `Find(Class)` — `places.notAboard`, `places.noClass`, `places.full`. `Jump(Contact, Wrecked)` —
  `places.notAboard`, `places.noContact`, `places.noRoom`. `Dismiss` (the dev panel's): the vessel out wiped and its box
  freed — `places.notAboard`, `places.noVessel`.
- **Fuzz:** now and then, a jump writing thousands of cells: `Dismiss`, `Find` of a class or none, `Jump` to a contact held or not.

## Invariants

- A contact keeps `1 ≤ Left ≤ Lasts`, `Nearest ≤ |Offset| ≤ Reach`; the radar's own come one at a time (`RadarContactsTests`).
- At most one vessel is out: `Grid.Reserved` is its box, and nothing outside the box changes with a jump (`PlacesTests`).
- The same class and seed make the same vessel on every peer, intact and wrecked (`VesselTests`).

## Deliberately not handled

- Where a contact lies changes nothing yet: every vessel comes `Vessels.Gap` right of the ship; the offset is the radar's.
- No radar needed: contacts come whether a radar is built or not (the computer's rules live in the client).
- A hero on the vessel when someone jumps: the cells vanish round them; they fly back. A hero, a floor stack or a body where a
  new wall is written: Items pushes stacks and bodies out; the hero's controller decides for heroes.
- A blueprint laid right of the ship before a jump may fall in the new box: it waits, refused `grid.reserved`, until the next
  jump or a cancel. Placing the box round plans would need Building's queries for a rare case.
- Content changed between `Find` and `Jump` makes a pending contact come out differently; written cells are saved, never
  regenerated.
- The jump's tick is long: clearing and writing a freighter passes the grid's change log, so readers look at everything once.
  It happens once per jump, like a load: 33–70 ms for a freighter on a desk (`PlacesTests` pins 200).
- Air and power in a vessel: its rooms start empty, its lamps dead.
- The soil clod aboard wears its drifting picture, a little above the deck: the user did not choose it to be drawn whole.
- A crate's loot counts as obtained at the jump, taken or not: counting what a hand takes out would need Items to know
  a vessel's crates.
- Liquids pay a vessel no mind: water leaving the hull meets its cells as any (flowing-liquids reads the grid's changes).
