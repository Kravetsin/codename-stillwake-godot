# Plan: Generated vessels, and their wrecks

## Why

The concept's first stage ends with a radar and an engine: the radar finds places, the jump takes the ship there. Today
there is nothing big to jump to. The only salvage is debris and wreckage a few cells across. This plan adds ships
generated from a class and a seed, any size from a shuttle of a few compartments to a cruiser, that look like ships
rather than houses in space. The first use is a wreck the ship jumps beside and the crew takes apart, the hull too. The
next is enemy ships, so an intact vessel is a first-class result and the wreck is made from it.

**Order and PRs.** Each task below is its own branch and PR, merged before the next starts. V-1 is the whole mechanic
for two classes and one cause; the rest widen it. V-1 adds one rule each to Grid, Building, Debris, Hazards, Crew, Pipes
and Power: tell the other developer before it starts, and agree who bumps `World.SaveFormat` first.

**Words.** A *vessel* is a generated ship, intact or wrecked; its *class* (shuttle, freighter, …) is content. A *wreck*
here is a vessel after the damage pass. Not to be confused with the starting wreck (`Content.Wreck`), *wreckage*
(breakable debris, 0034) or a *derelict* (a far-field wreckage kind, 0056). A *contact* is a vessel the radar has found
and not yet jumped to. A *compartment* is a part of a deck the generator fills with an *interior* (a hand-drawn ASCII
drawing); neither is a `Rooms` room, which is any sealed space. The *frame* of a class is its largest width × height; the
*vessel's box* is the grid box the current vessel was written into, its own width × height (`Grid.Reserved`). *The
ship's `Built`* is `Grid.Built` with no vessel out. Lengths are in cells (8 px; the hero is about 5 cells tall), times
in ticks (20 a second).

## Rules (confirmed with the user)

- A vessel lives in the ship's own world (surface 0), never on a surface of its own.
- It is reached by a jump chosen on the radar. Its cells are generated when the jump applies; the seed is drawn from
  the world's Rng when the radar finds the contact, so its class and size are known before the jump.
- It is placed beside the ship, `Vessels.Gap` cells right of the ship's `Built`, and may reach past the build zone. On
  the world the game makes it always fits: the crew builds only inside the zone, so the 512 columns right of the zone
  are free and the widest frame is 448. A map whose cells stand outside the zone may leave no room (`places.noRoom`).
- While it is there, its box refuses building. The next jump clears it.
- Everything on it, the hull too, is taken apart for resources with the tools that exist (screwdriver marks, axe, drill).
- Generation is layered, general to particular, and its result is a record, not cells:
  class → hull sections → decks, shafts and compartments filled with interiors → fittings.
  Then three separate pure steps: describe, stamp into cells, damage. An intact vessel is stamped without damage.
- Not now: enemy AI, movement, combat, turrets, crew, hull strength, the radar's and the jump's real costs (the concept
  leaves them open). Until V-4 the radar and the jump are dev commands.

## Design

### The describer: `sim/Core/Vessels`, `Shipyard`

Pure code beside `Terrain`: `Shipyard.Describe(class, seed)` gives a `Vessel`. Its draws come from a local `Rng` seeded by
`seed` mixed with the class's index in `Content.Vessels.Classes` (never a string's hash, which differs per process);
`Damage` draws from a second stream. The world's Rng is never touched. Core, because its users are `Places` now and enemy
ships later.

```
VesselClass(Id, Bells, Mids(min,max), MidWidth, Decks(min,max), Bow, ArmourDepth, Bridge, Crater,
            Yield(min,max))                                                 // sim/Content/Vessels.cs
Vessel(Class, Seed, Width, Height, Sections, Decks, Shafts, Doors, Compartments, Fittings)
Section(Kind: Stern|Mid|Bow|Bridge, Left, Width, Top, Bottom, TaperTop, TaperBottom)
Deck(Floor)                           // the floor row; the 6 rows above it are open: a pitch of 7
Shaft(Column, Top, Bottom)            // a ladder run, 2 wide, an even count of rows
Door(X, Y)                            // an ordinary door through the stern armour of the first deck
Compartment(Deck, Left, Width, Top, Role, Interior)   // Deck -1 for the bridge's
Fitting(Kind, X, Y, Loot)             // a machine of Vessels.Machines; a crate and what it holds
VesselInterior(Id, Role, Rows)        // ASCII, one Legend character a cell, 6 rows high
VesselPart(X, Y, Layer, Kind, Material, Holds)   // what Stamp gives: a thing a cell, as a grid op places it
```

1. **Hull first.** Sections are laid along the width: a tapering bow, `Mids` midsections, a stern with engine bells,
   a bridge on top. Each is a top and bottom profile per column with an integer taper; the silhouette is their union.
   Armour is every cell within `ArmourDepth` of the edge. Size is the number and scale of midsections, so one mechanism
   serves every class.
2. **Inside.** Decks at a pitch of 7 rows; a corridor along each; at least one shaft joining every adjacent pair; doors
   in the armour. Compartments split each deck between shafts; each takes an interior of its width and role. The role
   is by position: bridge forward and on top, reactor by the stern, holds midships, life support central. A required
   role always has a position (the class's minimum size guarantees it; a content test draws every class's smallest).
3. **Fittings.** Crates (`crate`) with loot by role, lamps (`lamp`, unpowered). Antennas, radiators and docking ports
   in V-2.

**What a vessel may hold.** `Vessels.Machines` is a whitelist, not an exclusion list: machines whose cost is fair loot
and that keep no state in another module by their id (no queue, no fitted module, no store, no drone job): `crate` and
`lamp` in V-1. A content test checks every interior against it.

**The hull's material.** Every wall and back wall of a vessel is a new material, `hull` (code 14): its `Cost` is what a
cell gives when hewn or taken apart, a scrap chunk; its `Strength`, `Armour` and `BuildTicks` as iron's, so a mark on it
takes iron's time and the axe iron's hits. It is never a blueprint: `Material.Built` now means a card in the build menu. A content test holds each class's whole yield
(walls, back walls, machines, crates) inside its `Yield` band, so a freighter is not a flood (economy plan, principle 2).
Floors, ladders, doors and machines are the ordinary ones and give their cost. Its look in V-1: the iron wall's tiles,
darker; art later.

### Stamp and damage

`Stamp(vessel)` turns it into a throwaway buffer: structures with materials, back walls, machines by their top-left
cell, crate contents. `Damage(buffer, vessel, cause)` edits it; the `Vessel` says where the reactor and holds are.
The cause is chosen at `Jump` (V-1: breach always). Causes, content:

- **Breach** (V-1): a crater, its side (above, below or the bow) and its centre drawn, its radius the class's; every
  cell within it emptied, machines and crates in it lost whole, so the loot falls by what the crater took.
  Compartments it reaches are open to space.
- **Blast** (V-2): the stern cut back and a radius round the reactor emptied.
- **Cut in two** (V-2): the aft half shifted by a gap and a few rows. Cells cannot drift; the gap is the drift.
- **Scavenged** (V-2): the hull whole, crates empty, doors left open.

Radii compare squared distances (integers); halves round down.

### The module: `Places`

`sim/Places`, id `places`, registered last. Why a new module: Debris owns moving pieces and only reads the grid;
Building owns plans; Terrain makes a surface's layout before its grid exists. A vessel is cells written into a running
grid and replaced when the place changes, and the other places the concept names (meteor rings, a cluster event) will
come through the same jump.

- **State:** `Contacts [{Id, Class, Seed}]` (at most `Vessels.MaxContacts`, 8), `NextContactId`. Nothing else: the
  cells are the grid's, the box is `Grid.Reserved`.
- **Depends on:** Grid (queries and ops), Items (`PutIn` to fill crates), Building (`Wipe`, below).
- **Commands**, from any peer (the co-op trusts its friends; dev commands are sent only by the dev flag and panel):
  - `Find(Class)`: draws a seed from the world's Rng, adds a contact. Refused `places.notAboard` (the sender is not on
    the ship's surface), `places.noClass`, `places.full`.
  - `Jump(Contact, Intact)`: clears the old box, places and writes the vessel, drops the contact. Refused
    `places.notAboard`, `places.noContact`, `places.noRoom`. `Intact` is sent only by the dev flag.
- **A jump, in order:** the new box first: its left the ship's `Built.Right + Gap + 1` (the ship's: the vessel's box left
  out), its top the ship's middle row (`Top + Height / 2`, rounded down) less the vessel's `Height / 2`, rounded down,
  moved inside the grid by `Vessels.EdgeClear` (16); none fits: `places.noRoom`, and nothing changes. Then
  `Building.Wipe(old box)`; `Reserve(box)`; the parts written with `Lay` in reading order, so machine ids follow it;
  crates filled with `PutIn`.
- **`Gap`** is 40, more than `Logistics.Reach` (32, a content test), so no drone bay of the ship reaches a vessel.

### Grid: the reserved box

- A saved `Reserved` box, null for none (added with that initializer). Query `Reserved`. Setting it bumps `Version`.
- Ops `Reserve(Box?)` and `Lay(cell, placement)`: `Place` that may write only inside `Reserved`, the zone not asked; any
  other problem throws. Only `Places` calls them (README). Why not `Place`: it refuses every cell outside the zone, and
  must refuse players inside the box. Grid's invariant becomes "nothing is placed outside the zone but by `Lay`".
- `PlacementProblem` refuses `grid.reserved` for a cell inside the box, before `grid.outsideZone`.

### Building: `Wipe(Box)`

A new op on `IBuildingOps`: takes apart everything in the box the way a finished mark does, through Building's own
steps (unplugging, a machine's state elsewhere), refunding and dropping nothing, and drops every blueprint, mark and
crack in the box. Why Building: it owns taking things apart, and a mark kept on a cell the new vessel stamps again would
survive with its progress. Machines in the box are only the whitelist's, so nothing else holds their ids.

### One rule each in six modules: the vessel is not the ship

- **Debris:** the hull's box (cells with a structure within `Built`) skips the reserved box, so the stream does not
  widen to the vessel. Pieces pass through it as through the hull (0006).
- **Hazards:** a meteor's targets skip the reserved box, so a vessel ahead never shields the ship. The streak may pass
  over the vessel: nothing collides.
- **Crew:** `EdgeCost` is 0 inside the reserved box as inside the field, so the cold stays off a vessel that reaches
  past the field's 128 cells. The README's "never less farther out" becomes "outside the field and the box".
- **Building:** `Hew` on the ship's surface strikes only cells inside the reserved box: each ray is judged per struck
  cell, and stops at the box's edge, so a fan from the gap never breaks the ship. Elsewhere aboard it is refused
  `building.notHere` as today; `HewProblem` and `Struck` follow the same rule. Shots (`Hit`) are unchanged. The client
  must offer the axe and drill there: check it does not hide them on the ship.
- **Pipes:** `LayProblem` refuses `grid.reserved` in the box.
- **Power:** laying or moving a cable into the box is refused `grid.reserved`.

### Saving and determinism

- Inputs are commands only; every peer runs the same pure generation in the same tick. The world's Rng is drawn only by
  `Find`.
- Adding `Places` bumps `World.SaveFormat` (38 → 39 today; whoever merges second takes the next number) with an empty
  step in `Migrations`. A joiner gets the cells, `Reserved` and `Contacts` in the save, nothing beyond 0002.
- **The jump's tick is long, and that is accepted.** A jump clears one vessel and writes another, several layers a cell,
  so a freighter's second jump may pass the grid's change log (65,536) and make Rooms, Atmosphere, Debris and Hazards
  look at the whole `Built` box once. It happens once per jump, every few minutes, like a load. A test measures the
  freighter's jump tick (V-1) and the cruiser's (V-3) with a `Stopwatch` and pins the measured ceiling; the user judges
  the hitch in play. Spreading the write over ticks waits for a hitch that shows.

### Client

No new scene: the grid, lights and tools already draw and act on any cells. V-1 adds a dev flag
`--vessel=<class>[,intact]` (a `Find` and a `Jump` on the first tick), the `hull` material's tiles, and the En text of
`grid.reserved` and every `places.*` refusal. V-4 brings the radar's screen.

## Tasks

### V-1 — A shuttle and a freighter, wrecked by a breach ✅

Built as below; the ADR is `2026-10-30-generated-vessels`. Yield measured over 200 seeds: a shuttle 412–1,097 items, a
freighter 2,881–6,765, nearly all hull scrap; the bands hold them with margin, to be tuned in play.

- Core `Vessels` (describe, stamp, damage with breach), `Content.Vessels` (two classes, about 12 interiors, loot by role,
  `Gap`, `EdgeClear`, `MaxContacts`, yield bands, the whitelist), the `hull` material, `Places`, Grid's reserved box,
  Building's `Wipe`, the six rules, the save step, the dev flag, `hull` tiles, En text.
- **Scenarios:** a jump writes only in its box, right of the ship's `Built`, and leaves every other cell and the zone's
  hash as they were; a second jump onto an overlapping box leaves no old crate, mark, blueprint or crack, and the ship
  untouched; a breach opens its compartments to space; the stream's box and the meteor's targets are the same with a
  vessel out; no cold in the box, and back after the next jump; `Hew` breaks a vessel wall, a fan from the gap leaves
  the ship's walls, and `Hew` elsewhere aboard is refused; a blueprint, a pipe and a cable are refused `grid.reserved`
  in the box (inside the zone and past it); a ship built to the zone's right edge still gets its vessel; a small map
  refuses `places.noRoom` and changes nothing; `Find` and `Jump` from a planet refused `places.notAboard`; every other
  refusal; the freighter's jump measured.
- **Invariants** (FsCheck, both classes × seeds): the same (class, seed) gives the same `Vessel`, buffer and hash,
  intact and wrecked; the vessel fits its class's frame, and the frame the space right of a full zone; every deck has
  its 6 open rows; every compartment is reachable from a door for the hero (a walk, a one-cell step, ladders, doors),
  asserted by construction (a shaft per deck pair, every corridor reaching a door) and walked with the hero's own reach
  helper if one exists. Proved to bite: one shaft dropped on a seed fails connectivity.
- **Content tests:** every interior holds only whitelisted machines; each class's yield inside its band; `Gap` over
  `Logistics.Reach`; every class's smallest vessel has a position for each required role.
- `ModulesTests`' `Fuzz` draws `Find` and `Jump` (both classes, intact or not); small fuzz maps meet `places.noRoom`.
- Docs: the ADR (`docs/decisions/YYYY-MM-DD-generated-vessels.md`), `sim/Places/README.md`; Grid, Building, Debris,
  Hazards, Crew, Pipes and Power READMEs; `docs/map.md`, `docs/glossary.md` (vessel, contact, interior).
- **Done when** a frame of each class, intact and wrecked, beside the starting wreck and beside a ship built to the
  zone's edge, has been looked at by the user, and a hero has flown over, gone in by a door and taken a wall and a
  crate apart.

### V-2 — Three more causes, two more classes, the outside

- Blast, cut in two and scavenged, the cause drawn at `Jump` by the class's weights; the corvette and the frigate
  (nacelle and keel sections join here); antennas, radiators and docking ports as fittings, with art the user picks.
- **Done when** each cause reads at a glance on a frame, and the scenarios of each cause pass.

### V-3 — The cruiser

- The cruiser's class (up to 448×128, 5–8 decks); its jump measured, and the write spread only if the hitch shows.

### V-4 — The radar and the jump for real

- `Find` from the radar antenna in range and on its beat, the jump paid in biofuel by the engine, the radar's screen
  (the ship computer already has a radar program to build on: `game/features/computer/RadarScreen.cs`).
  Needs the user's rules first: the concept leaves the jump's cost and the radar's screen open.

## Not in this plan

Enemy vessels (AI, movement, combat, turrets, crew), docking or claiming a vessel, flying it, power or air aboard one,
sentries aboard (the creatures plan may add them to vessels later).

## Deliberately not handled

- **A hero on the vessel when someone jumps:** the cells vanish round them, at most about 490 cells right of the ship
  and warm only where the next box or the field reaches. They fly back. Rare, and a body at worst.
- **A hero, a floor stack or a body where a new wall is stamped:** the vessel is written round whatever floats right of
  the ship. Items pushes stacks and bodies out as it does when a cell fills; the hero controller's handling of a body
  inside a wall decides for heroes. Accepted; V-1 looks at it on a frame and writes down what happens.
- **A blueprint laid right of the ship before a jump** in the new box is dropped by `Wipe` only if it was in the old
  box; one in the new box alone waits, refused `grid.reserved`, until the next jump or a cancel.
- **The cold past the box's rows:** a hero flying above or below the box, outside the field, is cold as anywhere there.
- **Debris over the vessel:** the stream's lane may run through it; a piece over a vessel wall is judged by its middle
  cell (Debris README), so one may be hard to grab there.
- **Content changed between `Find` and `Jump`** makes a pending contact come out differently: contacts are short-lived,
  and stamped cells are saved, never regenerated.
- **Air and power in a vessel:** its rooms start empty, as rooms with no vents do; its lamps are dead.

## Open

- The hull's yield and each class's band: a first pass in V-1, tuned in play.
- Class sizes and interiors per class: a first pass in V-1, from frames the user looks at.
