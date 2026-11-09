# A vessel is a world of its own, closed once everyone is home

- Status: accepted; built; supersedes in part [generated-vessels](2026-10-30-generated-vessels.md) (a vessel written into
  the ship's grid beside it, its reserved box, the jump that wipes the last) and [0076](0076-a-world-is-made-of-surfaces.md)
  (closing a surface, left for later)
- Date: 2026-11-09

## Context

A vessel was written into the ship's own grid, `Vessels.Gap` cells right of the ship: a reserved box that refused building,
one rule each in seven modules so the vessel was "not the ship" (the stream and meteors passed it, it was warm, tools broke
it, no one built in it), a `noRoom` refusal, and a wipe of the last vessel on the next jump. The concept had the whole ship
jump to the place. The user dropped that idea (2026-11-09): the ship stays where it is, and the crew goes to a vessel, as
the drop pod takes them to a planet. A player's craft to fly there is another change's; this one gives it a place to go.

## Decision

- **A vessel lies on a surface of its own** (0076), of its class's kind (`Content.VesselKind`, `vessel.<class>`): the ship's
  space (no gravity in the open, vacuum, starlight), no debris, no meteors; built as a planet allows, no machines, since
  everything on it is erased; tools break what stands and nothing is marked, as on a planet. Its grid is the vessel's frame
  and `Vessels.Margin` (128) cells of space round it, no build zone, so no cold.
- **`Jump(Contact)` opens it** and moves the sender there at once, just inside its first door (`Shipyard.Door`, the map's
  spawn): the radar's Set off and the dev panel both send it. `ISurfaces.Visit(peer, kind, seed)` opens a surface from a given
  seed (the contact's) and moves the peer in, only while commands apply, as `Transfer` does, so no module sees a surface
  appear under it. The vessel is written by a new hook, `Module.Opened`, once every module of the new surface stands
  rebuilt: Places lays the parts as the jump did, fills the crates and counts their loot obtained. Every jump opens a new
  world and uses up its contact. Refused, besides `notAboard` and `noContact`: `places.noHero` (a peer with no hero would
  park its seat there with no farewell to close it) and `places.seated` (a drop pod's rider would bring the pod along).
- **A transient kind's surface closes** (`Kind.Transient`) at the end of the tick, after the transfers and sends, on which no
  peer stands on it: everyone went home, died and woke aboard, or left the game. Its slot stays, `Surface.Closed`, with no
  modules, so ids keep meaning the slot and are never reused; a transfer there is `world.noSurface`; it is saved as its id,
  kind, seed and `closed`, no slices. Planets are not transient: they stay.
- **Saved whole while open,** so a world's save round-trips exactly (a joining guest's snapshot, a resync, the desync checks).
  In the game a load seats everyone aboard, so a vessel world never outlives a load (the user's pick).
- **The box beside the ship is gone:** `Grid.Reserve`/`Reserved`, `grid.reserved`, `grid.outsideReserved`, `Building.Wipe`,
  `Dismiss`, `places.noRoom`, `places.noVessel`, `Vessels.Gap`, `EdgeClear` and the seven modules' rules. Format 62 drops the
  grid's `reserved`; a vessel an old save had beside the ship stays as plain cells.
- **The engine stays as it is** (the user's word): it will have a use later.

## Consequences

- What is left on a vessel world is lost when it closes: a body and its inventory, crates not emptied, plans. Only what a
  hero carries home comes home.
- No revisit: the contact is used up. Quitting on a vessel loses it.
- The home ship runs while its crew is away, as on a planet: meteors and fires go on.
- Opening a freighter is a long tick, once per visit, like a load (`PlacesTests` pins 200 ms).
- A friend joins a vessel by `Transfer` (the dev panel's `down`) until the player's craft carries several.
