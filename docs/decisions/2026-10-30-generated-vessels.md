# Generated vessels, written beside the ship by a jump

- Status: accepted; slice V-1 built (the shuttle and the freighter, a breach, the dev jump); the rest planned in
  `docs/plans/vessels.md`
- Date: 2026-10-30

## Context

The concept's first stage ends with a radar and an engine: the radar finds places, the jump takes the ship there. There was
nothing big to jump to: the only salvage was debris and wreckage a few cells across. The user wants random places, a wreck
first, that the ship reaches by the radar's jump and the crew takes apart whole, the hull too; ships of every size, from a
few rooms to cruisers, that look like ships rather than houses in space; and enemy ships later, so an intact ship must be
possible and the wreck made from it.

## Decision

- **A vessel is described, then stamped, then damaged** (sim/Core `Vessels/`, `Shipyard`): three pure steps, so every peer
  makes the same one from a class and a seed. Draws come from a local `Rng` of the seed mixed with the class's place in
  `Content.Vessels.Classes`, never a string's hash; the damage pass draws a second stream.
  - **Described** (`Vessel`), general to particular: the class (`VesselClass`: midsections, decks, armour, bow, bridge, as
    ranges the seed picks from); the hull, its silhouette the union of sections along the width — engine bells left of a
    stern, midsections, a bow tapering more below than above, a bridge on top over the last midsection; decks inside it at a
    pitch of seven rows, six open over a floor; a compartment a midsection a deck, its role by position (the engine room
    next to the stern, life support in the middle, quarters on the first deck, holds below, the bridge on top), furnished
    with an interior of its role drawn as ASCII (`VesselInterior`); a ladder shaft between each two decks and down from the
    bridge; a door through the stern armour of the first deck. Size is the number of midsections, so one mechanism serves
    every class. The description is a record, not cells: enemy vessels will read it.
  - **Stamped** into parts (`VesselPart`): a back wall in every cell of the hull, walls of armour and floors, ladders, the
    door, the interiors' floors and machines, the bells; crates hold loot drawn by their compartment's role.
  - **Damaged** by what wrecked it (`VesselCause`): a breach is a crater of the class's radius from above, below or the bow;
    every thing with a cell in it goes whole, a crate with its loot. Intact is no damage.
- **It lives in the ship's own world, beside the ship** (`Places`, a new module, run and saved last). `Find` draws the seed
  when the radar finds a contact; `Jump` writes the vessel `Vessels.Gap` (40) columns right of the ship's built box, its
  rows centred on it, inside the grid by `EdgeClear` (16). It always fits on the game's world: nothing is built outside the
  zone, so 512 columns are free right of it, and the widest frame is 448. The next jump wipes it first.
  - *Rejected: a separate surface, as a planet.* Only a hero and its inventory cross surfaces (0076): a hull taken apart is
    thousands of items, and the ship would not be seen beside it.
  - *Rejected: only inside the build zone.* A ship built over the zone leaves no room.
  - *Rejected: always past the zone.* An early ship lies 500 cells away: a jetpack's whole tank there and back, in the cold.
- **Its box is reserved** (`Grid.Reserved`, saved, none by default): `Grid.Lay` writes only inside it, the zone not asked;
  `Place`, pipes and cables are refused `grid.reserved` in it, before `grid.outsideZone`. So nothing the crew builds is ever
  there, and a wipe never has to unplug anything of theirs.
- **The vessel is not the ship:** Debris' stream finds the hull without the box (the stream does not widen to it); Hazards'
  meteors aim at the ship past it (a vessel ahead is no shield); Crew's cold costs nothing in the box (one reaching past the
  field is still worked in); Building's axe and drill strike aboard inside the box only, each ray stopping at its edge.
- **Its hull** is a material of its own, `hull`: as strong and slow to take apart as iron, a scrap chunk a cell, drawn as
  iron darker; never built, so never a blueprint. `Material.Built` now means a card in the build menu.
- **What a vessel holds is a whitelist** (`Vessels.Machines`: the crate, the lamp): machines that are fair loot and keep no
  state in another module by their id, so a wipe leaves nothing behind. `Gap` is more than a drone bay's reach.
- **Yield is bounded per class** (`VesselClass.Yield`, a content test over seeds): a first pass of 300–1,300 items for a
  shuttle and 2,400–8,000 for a freighter, nearly all of it scrap chunks of the hull, to be tuned in play.
- Save format 39: the module joins every surface with no contact and no vessel out.

## Slices

| Slice | Delivers | Status |
| --- | --- | --- |
| V-1 | The shuttle and the freighter, the breach, `Find` and `Jump` by the dev flag `--vessel=class[,intact]` | built |
| V-2 | Blast, cut in two and scavenged; the corvette and the frigate; antennas, radiators and docking ports | planned |
| V-3 | The cruiser, and its write spread over ticks only if the hitch shows | planned |
| V-4 | The radar finds contacts in range, the engine's jump paid in biofuel, the radar's screen | needs the user's rules |

## Consequences

- A jump's tick is long: clearing one vessel and writing another passes the grid's change log, so Rooms and the rest look at
  everything once. It happens once per jump, every few minutes, like a load; `PlacesTests` pins the measured ceiling.
- A hero on a vessel when someone jumps is left in space; a blueprint laid where the next box falls waits till the jump
  after. Both are accepted (the module's README).
