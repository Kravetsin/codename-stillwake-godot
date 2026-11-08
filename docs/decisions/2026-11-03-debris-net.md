# The debris net

- Status: accepted.
- Decided by the user (2026-11-03), after `design-author`, `design-reviewer` and `/review-plan`; the look picked on its
  [pick page](https://claude.ai/artifact/CeSLuYfw15pauPkYMKswLE).

A net hung on the ship's bow catches small debris drifting into it and holds it until a hero takes it with E: Raft's collection
net, early, so salvage piles up while the crew works indoors. Drones that empty it or catch on their own are for later.

## What the player meets

1. With the workbench's first wires and tubes, beside the harpoon, the net's card inks in on the Storage tab after the crate. It
   costs 6 wire and 4 tube.
2. The player hangs the 2×6 net on the hull's right side, in space: every row of it against a hull wall on its left, its own
   cells open space (no structure, no back wall). Anywhere else the ghost is red: «Hang it outside, on the hull's right side».
   On the new game's scoop the one such place is the right face of the column on its roof; any wall built out gives more.
3. Scrap chunks, hull plates, cargo crates and supply pods drifting into it stay there, up to 10 pieces, the net sagging as it
   fills. Everything else (wrecks, soil clods, asteroids) passes through, and a full net lets everything pass. The pointer over
   it shows its count, «7/10»; nothing else on screen says it in words.
4. A hero presses E on it, outside or inside through the wall it hangs on, within the reach every machine's E has, and everything
   goes into the inventory; what does not fit stays in the net. It looks as catching a piece by hand does: each piece taken
   flies from where it lay in the net to the hero, and the text over the hero's head names what went into the inventory.
   Refusals show in the prompt as every E's do: «The net is empty», «No room in the bag», «The net is gone».
5. Meteors aim past the net to the wall behind it; a meteor breaking that wall breaks the net with it, and what it held floats
   out where it was caught, and the net's own 6 wire and 4 tube float at its cell for anyone to take. A net taken apart by
   hand drops what it held too and refunds its cost to its worker, as every machine does.

## Decisions

- **Pieces, not slots.** The user chose pieces: the net holds 10 things, each drawn as the piece it was. Slots would merge four
  chunks into one stack, so a net would almost never fill, and would make nets a drone source.
- **Loot is rolled when caught,** once the net's room is sure, so a full net draws nothing from the world's Rng. A caught kind
  counts as found for the Handbook, with no popup: the player may be far from the net.
- **Held entries** (`DebrisState.Held`): the net's machine id, the piece's id and kind (its look), the cell it was caught in
  (where it floats out) and what it still gives; a new saved field, so no format bump. Ids are never reused: a net built again
  on the spot starts empty.
- **`Collect`** walks the net's entries in catch order and gives each what fits (`Items.GiveWhatFits`), going on past one that
  does not, since a later one may. Not `Grab` (one piece, outside only) nor `Move` (it needs slots).
- **Collect shows as a Grab:** each entry's give is reported as a grab's (`CaughtFrom`, by piece and catcher), and the client's
  `OwnCatches` turns an own `Collect` gone from flight into the same popups and flights home as a `Grab`, from where each piece
  lies in the net. One path for both, so the two never drift apart. The catch into the net still shows nothing.
- **A net gone** drops its entries' items at their cells as floor stacks, one rule for a meteor and the screwdriver: taking a
  full net apart by mistake would otherwise lose ten pieces.
- **A meteor drops the net's cost:** `Hazards` drops its whole `Machines` cost at the net's cell (`DropAt(Nobody)`). Hazards
  owns it because its `Break` is the one place that knows a meteor, not a screwdriver, took the net; the screwdriver's
  refund to the worker and the broken walls' nothing stay as they were.
- **The look is the pick, `sling`, «Гамак на стрелах»:** two pipe booms with struts, a steel-cable net with crimped joins slung
  between them, its edges lashed with copper, drawn by `tools/anime-art/subjects/net.js`. The booms are the machine's picture;
  the plates bolting them to the hull lie over the wall's face; the held pieces, each in its own debris look, heap in catch
  order over the booms, overlapping as a net full of junk does; the net is drawn in front of them, sagging by load over 10
  steps to the pick's full sag. A catch pops in, with no settling.
- **Reach is the client's**, through the wall too, as every machine's E: among friends (0002).

## Deliberately not handled

- Drones: a net has no slots, so no drone touches it; catching or emptying by drone is the drones' design.
- Nets on the hull's left, top or bottom; a net on a planet's wall catches nothing, planets having no debris.
- The wall behind taken apart by hand: the net stays hanging, as the engine does.
- A full net raises no alert beyond its sag and the pointer's count.
- A partly collected container keeps its look in the net.
- A piece caught into the net while the hero's own E on it is still on its way to the host is given without its popup:
  the client shows the pieces the net held when E was pressed.
- A piece through two nets: the first reached catches it.
- Ten pieces in 2×6 cells overlap heavily, and the biggest stick out past the net's sides. Accepted as a net full of junk; if it
  reads badly in a playtest, the footprint grows to 3×6, the catch rate staying, since rows decide it.
- The catch rate (about a piece a minute on the scoop, a full net some ten minutes off) is a playtest number: a tall hull thins
  the stream.
