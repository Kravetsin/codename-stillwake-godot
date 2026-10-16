# 0041 — Wreckage of many kinds, and circuits

- Status: accepted
- Supersedes: of [0027](0027-what-is-analysed-opens-what-is-made-of-it.md), the `Locked` list (the
  solar panel held back until electronics exist); of [0034](0034-wreckage-and-the-axe.md), the hull
  section as the only wreck and "about one spawn in thirteen is a wreck"
- Date: 2026-10-16

## Context

The hull section was the only wreck: every one chopped gave plates and chunks, so the axe had one
answer and the drift one surprise. The solar panel could not be built at all, held back by a list
until electronics existed, so the wreck's one panel was all the power a ship would ever make.

## Decision

- **Six kinds of wreckage,** each a silhouette that tells what it breaks into. As content
  (`Wreckage`, `Loot`, `Debris.Kinds`):

  | Kind             | Cells | HP | Hits | Loot                                                      | Weight |
  | ---------------- | ----- | -- | ---- | --------------------------------------------------------- | ------ |
  | `hullSection`    | 3 × 2 | 15 | 8    | 2–3 hull plates, 2–4 scrap chunks                          | 3      |
  | `cargoContainer` | 4 × 2 | 11 | 6    | 8–12 planks, 1–2 scrap chunks; 1 in 4: 1–2 gears; 1 in 4: 2–4 wire | 2 |
  | `machineBlock`   | 3 × 2 | 19 | 10   | 2–4 gears, 1–2 hull plates, 1–2 scrap chunks; 1 in 4: a rotor | 1  |
  | `cableRack`      | 2 × 2 | 9  | 5    | 6–10 wire, 1–2 scrap chunks, exactly 1 circuit            | 1      |
  | `tankCluster`    | 2 × 2 | 7  | 4    | 2–3 canisters, 1 scrap chunk                              | 1      |
  | `shieldSlab`     | 4 × 2 | 31 | 16   | 5–7 hull plates, 1–2 scrap chunks                         | 1      |

- **Small debris tripled** (scrap chunk 18, hull plate 9, cargo crate 6, supply pod 3), so the wrecks
  fit between them: of 45, 9 are wrecks. A piece spawns every 10 s on average; a wreck every 50 s, a
  hull section every 2.5 min, each kind of weight 1 every 7.5 min.
- **Circuits,** a new item, come only from cable racks, one each and always: luck decides when a rack
  drifts by, never what it gives. No craft makes one.
- **The solar panel is built of circuits:** 6 scrap, 6 iron and 3 circuits. It opens by the one rule
  of 0027, once scrap, iron and a circuit are analysed. With nothing left in it, `Research.Locked` and
  `Recipe.Locked` go (principle 2), and the analyzer's "cannot open it yet" line with them.
- **Nothing new besides:** every kind uses the crack filter, the chop and the burst, and the loot
  floats as a hull section's does (0034). The client knows each kind's picture and its size, which is
  the box it is aimed at and struck by (0039, 0040).

## Rejected

- **A gas burst from the tank cluster:** an effect and a push for one kind; later, if at all.
- **Circuits by chance (1–2 in 1 of 2):** a panel would wait on two kinds of luck.
- **The shield slab only in harder places:** there are no places yet; it drifts with weight 1 until
  there are.

## Consequences

- A first panel takes three racks: about 22 minutes of drifting on average, or less for a crew that
  chases every rack it sees.
- A wreck's item icon is its picture shrunk to 16 pixels; seen only in creative mode and the testbed.
- No save format change: pieces and stacks store items by id, and nothing saved names a lock.
