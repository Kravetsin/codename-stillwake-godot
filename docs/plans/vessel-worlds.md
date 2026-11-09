# Plan: A vessel is a world of its own

## Why

Today a vessel is written into the ship's own grid, `Vessels.Gap` cells right of the ship (generated-vessels, 0076's
surface 0): a reserved box that refuses building, one rule each in seven modules so the vessel is "not the ship", a
`noRoom` refusal, a wipe on the next jump. And the concept has the whole ship jump to the place.

The user drops that idea (2026-11-09). The ship stays where it is; the crew goes to a vessel, as the drop pod takes them
to a planet. A vessel lives on a surface of its own, opened when someone goes there and closed once everyone has come
home. Nothing about the build zone, the box or the gap is left to think about.

**Asked by the user** (2026-11-09):
- Remove the dev panel's `shuttle`, `freighter` and `to vessel` buttons; add two: **to shuttle** and **to freighter**.
- A press makes a new world with that vessel and moves the player's hero onto it.
- Meanwhile the home world (the ship) stays in memory.
- Once the player is back home, the vessel's world is erased.

**Where it is heading** (principle 2): the radar's contacts (built) are the places; a craft of the player's (another
agent's plan) will take the crew there and back, calling what this plan leaves; later kinds of place (a cluster of debris,
an event, enemy ships) are other kinds of transient surface. So the mechanic built here is *a transient surface*, not "a
vessel button": opened for a visit, closed once empty, its first users `Jump` and the dev panel, its next that craft.

## Words

- **Vessel world** — a surface (0076) of a vessel kind: a grid of space with one vessel in its middle. "World" in the
  user's words; in code it is a surface, since a `World` is the whole of them.
- **Transient surface** — one whose kind says it closes once no peer stands on it. Planets are not transient: they stay.
- **Closed** — a surface whose modules are dropped and whose id is never reused; what was on it is *erased*.
- **Visit** — opening a surface and moving a hero onto it in one step, at command time.
- **Frame** — a described vessel's `Width` × `Height` (`Shipyard.Describe(class, seed)`, `Vessel.Width`/`Height`): it
  depends on the seed, at most the class's `Length` wide.
- *Shuttle* here is always the vessel class (a derelict to salvage). The player's craft that will take the crew there is
  another agent's work, not this plan's (the user's word); this plan only leaves `Jump` and `Visit` for it to build on.

## Rules (confirmed with the user, 2026-11-09)

- A vessel lives on a surface of its own, never in the ship's grid. The ship's grid has no reserved box any more.
- `Jump(Contact)` stays the way in (the radar's Set off and the dev panel both send it), with a new meaning: it opens a
  vessel world of the contact's class and seed and moves the sender there, to its spawn just inside the door.
- Aboard only, as now: from a planet or another vessel world, `Jump` is refused `places.notAboard`; go home first. A
  sender with no hero yet, or seated in a drop pod, is refused too (below).
- Every `Jump` opens a new vessel world and uses up its contact: two players pressing it get two worlds, and a vessel
  once left cannot be gone back to. A friend joins one through the dev panel's `down` (a `Transfer` to the highest-id open
  surface other than the ship), arriving at the same spawn.
- A vessel world closes when no peer stands on it, at the one point named under Closing below: everyone went home, died
  (and woke aboard, 0076), or left the game (a farewell seats them aboard). Whatever stays there is erased with it:
  machines, crates, floor stacks, plans, and the body of a hero who died there with everything it carried. Only what a
  hero carries home comes home (0076: only the hero and inventory cross).
- **Loading a save brings everyone home, so a vessel never outlives a load** (the user's pick): the game seats the host
  aboard and sends every other hero its farewell on load (`Main.Enter`, as for a planet), the vessel is left empty and
  closes on the first tick.
- The ship keeps running while its crew is away, as it does while they are on a planet (0076: every surface runs).
- A vessel world has the ship's kind of space: no gravity in the open, gravity aboard (`Gravity = null`, the ship's way,
  `Kind.Gravity`), vacuum, starlight, no debris, no meteors. It is built as a planet allows (`PlanetTypes.First.Builds`:
  walls, floors, ladders, doors, hatches) to patch a way through; no machines, since everything on it is erased when it
  closes. Its grid is the frame and `Vessels.Margin` cells of space on every side; no build zone, so no cold at the edge.
- Vessels come as built (wrecks to come, the user's word). The dev `wrecked` toggle goes with the buttons.

## Design

### Content: a vessel kind per class

`Content.Surfaces.VesselKind : Kind`, one per `Vessels.Classes` row, id `vessel.<class>`, with its `Class`, `Transient = true`,
`Debris = false`, `Builds = PlanetTypes.First.Builds`, no `Rows`: its layout is made from the vessel (below). `Kind` gains
`Transient` (default false); `Vessels` gains `Margin`. Why not one `vessel` kind with the class on the `Surface` record: the
kind already names the rules of a surface; a class's kind keeps `Surface` as it is.

### Core: opening for a visit

- `ISurfaces.Visit(int peer, string kind, uint seed)` → the new id. It opens a surface of `kind` with the given seed (not one
  drawn from the Rng: the contact's seed was drawn when the radar found it), and moves `peer` there at once, to the
  surface's spawn. Allowed only while commands apply (it throws during the phases), the moment `OpenSurface` and
  `Transfer` already use, so no module sees a surface appear under it. Why not `Send` + `OpenPlanet`: those defer to the
  end of the tick, and the hero would stand a tick in empty space before the vessel is written.
- **The layout.** `World.Open(kind, opener, seed)` makes a vessel kind's layout in Core, as it grows a planet's with
  `Terrain.Generate`: `Shipyard.Describe(class, seed)`, a grid of space `Margin` cells round the frame, the frame centred,
  and an `S` at `Shipyard.Door(vessel)` (pure, the cell just inside the first door, moved from the client's `VesselDoor`;
  over the middle if none). Crew's `Init` takes its spawn from that `S`, so every arrival with no cell (`Visit`, `down`'s
  `Transfer`, a wake from passing out, `Crew` line 216) stands at the door.
- **Writing the vessel.** `Open` runs, in order: `Create`, `Inherit`, `Rebuild`, then a new hook `Module.Opened()` on every
  module. After `Rebuild`, because Grid's `Occupancy` and `Log` exist only from then and `Lay` needs them. Its one user is
  Places, which writes the vessel (below). `ISurfaces` gains `Seed`, the surface's own.

### Core: closing

- **When.** At the end of `Step`, after every surface ran its phases and after the deferred `Send`s were made (Sim.cs, after
  the `_sends` loop): the world walks its surfaces in id order, and one of a transient kind with no peer in `_where` on it
  is closed. A `Transfer` and a farewell made this tick have already moved their heroes.
- **The slot stays,** so `_surfaces[id]` and `World.Surfaces[id]` keep meaning the id. `Surface` gains `Closed` (false by
  default). A closed surface has no modules, runs no phase, takes no `Transfer` (`world.noSurface`); `ISurfaces.Send` to
  it throws (a bug: today's callers send only to the ship or a planet).
- **Every walker of surfaces skips closed ones,** each named and fixed in S-1: `World.Step`'s phase loop, `Save`, `Load`,
  `Rebuild` after load, `Fuzz`, `ModulesOn`; `Main.Enter` (it asks every surface's Crew for heroes); the client's view
  switch and the dev panel's `ToLast`; `GroundDemo`. A `grep -n "Surfaces\b\|_surfaces"` over `sim`, `net` and `game` is
  the checklist; a test opens and closes a vessel, then saves, loads, joins a guest and draws the fuzz on the result.
- **Saved** as `{ id, kind, seed, closed: true }` with no `slices`; `Load` has its own branch for it (no `Another()`, no
  `slices` read). An added field meaning "none yet": no format bump.
- Commands in flight from a closed surface: `world.elsewhere` already refuses them (the sender is home).

### Places: the vessel written on its own surface

- `Jump(Contact)` on the ship: refused `places.notAboard` off the ship, `places.noContact` for a contact that is gone,
  `places.noHero` for a sender with no hero (a peer not yet in, whose seat would hold the vessel open with no farewell to
  close it, 0076) and `places.seated` for a hero in a drop pod or flying (its pod would follow and land on the vessel).
  The two new checks read Crew's `HeroOf` and Landing's `SeatOf`/`FlightOf`: Places gains both query interfaces in its
  constructor (both modules run before it), as Crew's and Landing's own commands ask the same (`crew.noHero`,
  `landing.seated`). Else it
  drops the contact and calls `Visit(peer, "vessel." + class, contact.Seed)`. No box, no `noRoom`, no wipe. `Wrecked` goes.
- `Opened()` on a vessel world's Places: describes the vessel from its kind's class and `Surfaces.Seed`, then the steps
  `ApplyJump` takes today, aimed at the frame's place in the grid: parts laid in reading order, crates filled (`PutIn`),
  their loot counted obtained (`Caught`; the board is global, so it counts once, taken home or not, as now).
- **Saved whole** while open, as a written vessel is today ("written cells are saved, never regenerated"). Though a load
  in the game closes it, the world's save must round-trip exactly: it is the snapshot a joining guest and a resync receive
  (`Host`), and what the desync checks compare. A frame is at most 448 × its decks: a few hundred KB, only while someone
  is there.

### Removing the box beside the ship (S-2)

`Grid.Reserve`/`Reserved` and `grid.reserved`; the vessel rules in Crew (box warm), Debris (stream passes it), Hazards
(meteors pass it), Building (axe and drill on its hull in `Digging.InVessel`, and `Wipe`, whose only callers are `Jump` and
`Dismiss`: S-2's author greps and deletes it), Pipes and Power (refuse in it); `Dismiss` and `places.noVessel`; the
client's `VesselDoor`. `World.SaveFormat` bumps: the step drops the grid's `reserved`; a vessel an old save had beside the
ship stays there as plain cells, built on and taken apart like any.

### Client

- **Dev panel:** `shuttle`, `freighter`, `wrecked`, `to vessel`, `back`, `remove` go; **to shuttle** and **to freighter**
  come, each `Summon(class)` as today (a held contact of the class jumped to, else `Find` then `Jump` once it comes), now
  moving the hero. Off the ship they say "Go aboard first." `down` (`ToLast`) takes the highest-id open surface but the
  ship. `--vessel=class` starts the new game with the host on that vessel.
- **The radar's Set off** keeps sending `Jump`: it now takes the presser onto the vessel at once.
- Views switch on a transfer as they do for a planet. A vessel world is a ship-like kind: stars behind, no sky, no fog
  edges, no backdrop land. Check by frames (`tools/snap.ps1 -Movie`) going there and back.
- Russian and English for the refusals (`places.notAboard`, `places.noContact`, and the new `places.noHero`,
  `places.seated`); dev buttons are English.

## Steps

Each its own branch and PR, merged before the next. S-1 touches Core's `World` and `ISurfaces`: tell the other developer
before it starts.

### S-1 — Vessel worlds: visit and close

Content kinds; `Transient`, `Margin`, `Closed`, `Visit`, `Opened`, `Seed`; the vessel layout and `Shipyard.Door`; Places'
`Jump` and `Opened`; every surface walker; the dev panel; `--vessel`; ADR and concept (Docs below). After S-1 nothing calls
`Grid.Reserve`: the box's rules in the seven modules stay in the code, never firing, until S-2 deletes them.

Tests (scenario unless said):
- `Jump` moves its sender into a new surface of `vessel.<class>`, standing at the door; the ship's grid unchanged.
- The vessel there has the cells, machines and crate loot the current `PlacesTests` (before S-1) expect for the same seed,
  re-aimed at the frame's place in the new grid.
- Refusals: off the ship `places.notAboard`; a gone contact `places.noContact`; no hero `places.noHero`; seated `places.seated`.
- `Transfer(v)` by a second hero and a wake from passing out on the vessel both land at the door.
- `Transfer(0)` from the vessel: the surface closes that tick; `Transfer` to it then `world.noSurface`; a new `Jump` opens
  a new id, never the closed one.
- Two heroes on one vessel: the first going home leaves it open; the second closes it.
- A hero dying there (wakes aboard) and a peer leaving the game there both close it.
- World save and load while on a vessel round-trips (the hero there, the vessel intact, `Rebuild` run); a save after it
  closed holds the slot closed and loads closed. The game's load (`Main.Enter`'s seating, as a test of its logic) leaves
  the vessel empty, closed on the next tick, every hero aboard.
- Two worlds kept alike across a `Visit` and a close hash the same.
- Invariant: after any tick no transient surface is open with no peer on it (FsCheck over the fuzz; proven to bite by
  skipping the close once).
- Fuzz: `ModulesTests` keep passing with `Jump` drawn by Places' fuzz; the world's fuzz caps open surfaces (not slots) at
  `FuzzedSurfaces` and now and then draws a `Transfer` to a closed id, for its `world.noSurface`.
- Budget: a freighter's `Jump` tick under `PlacesTests`' 200 ms pin.

### S-2 — No box beside the ship

The removals above, the format bump and its migration test, each module README's vessel line dropped, `PlacesTests`'
box tests deleted.

## Docs

- A new ADR `2026-11-09-vessel-worlds` superseding generated-vessels' "a vessel lives in the ship's own world" and 0076's
  "closing a surface waits for the pod and the jump".
- `docs/concept.md`, the arc: the radar finds places, the crew goes there; the ship does not jump. The engine's paragraph
  stays as it is (the user's word).
- `docs/plans/vessels.md`: its Rules and the box sections rewritten to point here; V-4 ("the radar and the jump for real")
  drops the ship's jump.
- `sim/Places/README.md`, `sim/Core` surface notes, `docs/map.md`, `game/features/dev-panel/README.md`.

## Not in this plan

- The player's craft that flies the crew to a vessel: another agent's plan.
- The engine: untouched.
- Wrecks, enemy ships, other kinds of place.
- Where a contact lies (its offset) mattering.

## Deliberately not handled

All of these follow from the closing rule (Rules, Core: closing).

- **What is left on a vessel world is erased** when it closes: a body and its inventory, crates not emptied, plans.
  Coming back for it needs the world kept, which the user ruled out.
- **A crate's loot counts as obtained when the vessel opens,** taken home or not, as it does today at the jump.
- **No revisit:** the contact is used up by `Jump`.
- **Quitting on a vessel** loses it: the load brings everyone home (the user's pick).
- **A friend joining a vessel** goes only through the dev panel's `down` for now.
- **The home ship running unattended:** meteors and fires go on while everyone is away, as on a planet. No pause.
- **Opening a freighter is a long tick** (a registry made, a 200-column vessel written): once per visit, like a load.
- **Content changed while someone is on a vessel:** the written cells are saved, never regenerated, as now.

## Settled with the user (2026-11-09)

- The two buttons are the two classes there are: `shuttle` and `freighter`.
- The engine stays as it is, untouched: it will have a use later.
- Building on a vessel world: as on a planet.
- The player's craft is out of this plan: another agent designs it.
- Loading a save brings everyone home; the vessel closes.
