# The debris net

A net hung on the ship's bow catches small debris drifting into it and holds it until a hero takes it with E: Raft's collection net,
early, so salvage piles up while the crew works indoors. Drones that empty it or catch on their own are for later and not designed here.

Written by `design-author`, cut down by `design-reviewer`, decided by the user (2026-11-03), then put through `/review-plan`. Its claims
about the code were checked by reading it; nothing has been run. The look is picked: «Гамак на стрелах», `sling`, two pipe booms with
struts and a steel-cable net slung between them, on the [pick page](https://claude.ai/artifact/CeSLuYfw15pauPkYMKswLE).

Words: a **piece** is a debris piece, drifting or held; an **entry** is a piece in a net's `Held`; the **inventory** is a hero's
hotbar and bag together. Sizes are wide × tall, in 8 px cells.

## What the player meets

1. With the workbench's first wires and tubes the net's card inks in on the Storage tab, beside the crate (step 7 of
   [board.md](board.md), with the harpoon).
2. The player hangs the 2×6 net on the bow: every row of it against a hull wall on its left, its own cells open space. Anywhere else the
   ghost is red: «Hang it outside, on the hull's right side».
3. Scrap chunks, hull plates, cargo crates and supply pods drifting into it stay there, up to 10 pieces, the net bulging out as it fills.
   Everything else (wrecks, soil clods, asteroids) passes through. A full net lets everything pass; its full bulge says so, and the
   pointer over it shows «7/10».
4. A hero presses E on it, outside or inside through the wall, and everything goes into the inventory; what does not fit stays in the net.
5. **Going wrong:** an inventory with no room says so. A meteor breaking the wall behind the net breaks the net, and what it held floats
   out where it hung; so does a net taken apart by hand.

An estimate, for the playtest to check: a net catches about one piece a minute (6 of the stream's ~88 rows in the scoop's hull, a piece
of any kind every ~4 s, most of them catchable), so a full one is about ten minutes away.

## Decisions

**Content.** A machine `net`, 2×6, of 6 wire and 4 tube, no slots, no power, Storage tab after the crate, with a `Legend` character for
test maps. One trait, `Catches`, read by Grid, Hazards, Debris and the client. In `Content.Debris`: `NetHolds = 10` (pieces, whatever
each is) and `NetCatches`, the kinds a net takes: `scrapChunk`, `hullPlate`, `cargoCrate`, `supplyPod`.

**Placement** (Grid). `PlacementProblem` refuses a `Catches` machine `grid.notOnHull` unless, for every row, the cell directly left of
the footprint holds a wall (the structure layer, `IsWall`), and every cell of the footprint is space (`IsSpace`: no structure, no back
wall); both helpers are the engine's, from its exhaust check. Without the space rule a net hung inside a room would catch there, since
debris passes through the hull. The one reason text covers a missing wall and a back wall alike; a machine in the way stays
`grid.occupied`.

**Held** (Debris' state). `Held` is a list of entries `{ Net, Id, Kind, Cell, Items }`: `Net` the machine's id (ids are never reused, so
a net built again on the spot starts empty), `Id` and `Kind` the piece's, `Cell` the cell of its middle when caught, `Items` a
`List<Stack>` of what it gives (its salvage item, or a container's loot), never tuples, which the world's serializer writes empty. Debris
already depends on Items for `Grab`.

**The catch** (Debris' drift system, simulation phase). After a piece moves it is caught when its kind is in `NetCatches`, it is not
reeled, and the cell of its middle holds a `Catches` machine with fewer than `NetHolds` entries. Pieces are tested in id order; a caught
one leaves `Pieces` and becomes an entry. A container's loot is rolled by `LootOf` then, after the free place is confirmed, so a full net
draws nothing from the Rng. The catch records `Found` through Grab's own helper (shared, not copied), but not `CaughtFrom`: no popup
fires at a net the player may be far from; the handbook may show a kind before its pieces are collected.

**Pieces, not slots.** The user chose pieces: the net holds 10 things, each drawn as the piece it was. Slots would merge four chunks into
one stack, so a net would almost never fill, and would make nets a drone source (see Deliberately not handled).

**Collect(Machine)**, the command E sends. It walks `Held` in catch order and, for every entry, gives what fits through a new
`Items.GiveWhatFits(peer, items)`, which returns the rest and counts what it gave as obtained, as `GiveOrDrop` does; it goes on past an
entry that does not fit, since a later one may. An emptied entry goes. Refused `debris.noNet` (no such machine, or it does not catch: a
meteor or a screwdriver may clear it before the host applies the E), `debris.netEmpty`, `debris.bagFull` (nothing fits). Not `Grab`: one
piece, outside only, the rest floating off. Not `Move`: it needs slots.

**A net gone** (Debris, at the start of its drift system, before pieces move and catch). Entries whose `Net` is no longer a `Catches`
machine drop their items at their `Cell` (`DropAt(Nobody)`), in catch order, as floor stacks. It is a pure function of the grid and
`Held`, run when the grid's `Version` has moved since the last look; that last look is derived (`_heldAt`, reset in `Rebuild()` as
`_hullAt` is), never saved, so running once more after a load changes nothing. One rule for a meteor and the screwdriver; kept against
the reviewer's cut, because taking apart a full net by mistake would otherwise lose ten pieces.

**Meteors** (Hazards). The user's rule: meteors never hit the net, and a meteor breaking the wall it hangs on breaks it too. Meteors come
from the right, and today a machine met first shields its row; the aim passes a `Catches` machine, so the wall behind is still hit (a
bow lined with cheap nets would otherwise end the shower). In the break step, each wall the blast breaks, in cell order, clears the
`Catches` machine in the next cell of its row (x + 1 inside the grid), refunding nothing; a machine is cleared once, and Grid's `Clear`
does nothing where nothing is. Net cells still stop the blast's spread, as every machine does; intended. Hazards runs before Debris
(Registry), so the pieces drop on the same tick.

**Client** (`features/debris`). The picked `sling`: its back (booms, struts, plates) and its net in front of the held pieces, the net's
sag by load (`SAG` in the pick page's `nets.js`, extended to 10), exported through a `tools/anime-art` subject. Held pieces drawn with
their own looks in a heap that overlaps, as a net full of junk does. A catch pops in; no settling animation. The pointer over a net shows
its count. E on a net sends `Collect`; refusals show in the existing popup.

**Saved.** `Held`, a new field initialised empty: no format bump. `_heldAt` is derived.

## Determinism and co-op

Pieces caught in id order, loot drawn in that order; `Held` kept in catch order and walked so; drops before the catch; integers only.
Two heroes' E on one tick: host order, the second getting the rest or `debris.netEmpty`. A joining peer gets `Held` in the snapshot.
Reach, through the wall included, is the client's, as every machine's E.

## Deliberately not handled

- Drones: a net has no slots, so no drone touches it (0062 serves slots); catching or emptying by drone is the drones' design.
- Nets on the hull's left, top or bottom; a net on a planet's wall catches nothing, planets having no debris.
- The wall behind taken apart by hand: the net stays hanging, as the engine does.
- A full net raises no alert beyond its bulge and the pointer's count.
- A partly collected container keeps its look in the net.
- A piece through two nets: the first reached catches it.

## Tests

Scenarios:
- Placement against a wall column; refused with a row missing its wall, a back wall in a cell, or the wall on the right.
- Each kind of `NetCatches` caught; a wreck, a clod, an asteroid and a reeled piece pass; a full net passes and draws nothing from the
  Rng; a container's loot rolled once and recorded in `Found`; a piece caught on the tick its net is built.
- Collect all; Collect with a part-full inventory (a later entry still given); each refusal leaving the world unchanged; Collect from
  inside; two Collects on one tick; a Collect on the tick a `Work` finishes taking the net apart gives `debris.noNet`.
- A meteor's aim passing the net to the wall; the net cleared, its pieces on the floor; a blast breaking the wall beside a net clears it;
  a power below the wall's armour clears nothing.
- A net taken apart drops its pieces; save and load mid-fill keeps the items; a load, then the net cleared, still drops them.

Invariants: every entry's net stands after the drop; none holds more than `NetHolds`; Collect neither makes nor loses items; the
conservation sums (Building's) count `Held`. `ModulesTests`: Debris' `Fuzz` draws `Collect`, on a map with a net (most draws refused
otherwise).

## Order

1. **Sim:** content row and `Legend` character, the trait, placement, `Held`, the catch, `Collect`, the drop, meteors, tests; the
   Debris, Grid, Hazards and Items READMEs, each checked against 8 KB first, history to the ADR.
2. **Client:** the `sling` exported, the held heap, the count on the pointer, E, i18n En and Ru.
3. **ADR** with the pick and the user's rules; the concept's catching line; this plan deleted.

## Risks

- **Ten pieces in 2×6:** debris pictures are 12–18 px against a 16 px wide net, so the heap overlaps heavily. If it reads badly in the
  game, the footprint grows to 3×6; the catch rate stays, since rows decide it.
- **The stream's rows:** a tall hull thins the stream (`Debris.Margin`), so a net's catch rate is a playtest number.
