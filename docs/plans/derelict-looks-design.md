# Design: derelict looks

Reference: [the approved mockup](derelict-looks-mockup.html), published at https://claude.ai/artifact/BDRspiJRSZ3MBMfp4A7LsE.

## Problem
On a vessel world the player gets a grey box of cells: a stepped bow, wall "tentacles" for engines, the same iron wall
everywhere, a dimly lit inside and nothing outside to take. One derelict looks like the next, and none of them reads as a
ship that someone owned and then left.

## Player experience
1. The crew arrives at the door of an intact freighter in empty space. From outside it reads as a ship: modules of
   different heights with ribs between them, a raised bridge with a housing behind it, a chamfered bow, nozzles at the
   stern, radiators, a mast and a dish on top, and containers hanging from a keel truss. The owner's stripe and
   "PELICAN · KT-0412" are painted on the side. Light falls from the upper left.
2. A red beacon blinks on the mast and a white strobe flashes at the bow.
3. Inside it is dark: red emergency lamps, one lamp dying, consoles glowing, and a red dot over each shut door. The
   helmet lamp lights the rest.
4. The rooms show what happened: frost on a cold wall, a sparking cable, soot over a burnt console, a fallen ceiling panel.
5. Every outside part comes apart with a tool and gives parts. The hull still comes apart cell by cell.

Goes wrong: a hero cuts the hull under the painted name. The letters over the cut cells vanish and the rest of the name
stays. The mast above stays where it was, in space; machines need no floor.

## Out of scope
- Wrecked vessels (`VesselCause.Breach`): no outside damage, torn plates or breach looks.
- Any backdrop: planet, nebula, parallax.
- Inner doors stamped open or jammed (the mockup shows some): all stay shut as now.
- Signs as things: the toppled crate holds nothing, the gas is not air, the frost is not cold.
- Directional starlight in the light engine: the art carries the direction.
- New classes, the arrival shot, and how a vessel world opens or closes.

## Scale
- World: one vessel per surface, up to about 235×52 cells for a freighter. About 10–30 outside parts, at most about 60
  client lights, 3–5 signs.
- Players: 1–4 on one vessel.
- Frequency: no new commands. Parts break by the tool hits that already exist. Looks change only where the grid changed.

## Approach

**1. Sim or client.** If a hero can take it apart, or it fills a cell, it is sim. If it only shows, it is client, derived
from class, seed and grid.
- **Sim:** bulges, ribs, the housing, the bow line, nozzles and every outside part (as machines), and a second main
  engine on 3-deck freighters.
- **Client:** the hull's skin (panels, seams, rivets, tone steps, outline, chamfers, livery, name, code, rust, pits), lamp
  states, screens, door dots, beacons and signs.
- **Why:** no rule reads paint or light, and light is already drawn only. Lamp states in the sim would need machine rows
  that differ only in their picture, and those would change lamp yields, which the rules forbid.
- **Rejected:** looks saved in Places' slice. That saves and hashes bytes no rule reads, and a joining guest already has
  the seed.
- **Where the numbers live:** shapes and parts in `sim/Content/Vessels`; looks (liveries, names, sign and lamp odds) in
  `game/Content`, where the lamp looks already are.
- **A hero dismantles what a look sits on:** each look piece names its cells and what they held when stamped (hull wall,
  back wall, floor or machine). It is drawn only while they still hold it. `WreckDress` already uses this rule for its
  torn cells.

**2. Outside parts.** They are new rows in `Vessels.Outside`: piles as salvage is, with `Hp`, a fixed footprint, `Gives` as
their cost and no menu card. They join `Machines.All` and `Vessels.Machines`.
- Not `VesselSalvage`, because those copy a drifting piece's size and loot, and none of these parts drifts.
- The keel's containers reuse `salvageContainer`.

| Part | Class | Footprint | Where |
| --- | --- | --- | --- |
| nozzle | both | 8×7 | in space, flush with the stern armour, centred on each engine deck |
| rcs | both | 3×2 | four: top and bottom, at the stern and at the body's fore end |
| mast | both | 2×8 / 2×10 | on top; a freighter's on its bridge |
| dish | both | 5×5 | on top near the bow; a freighter's on its bridge |
| solar | shuttle | 11×6 | on top near the stern; a pile, not the working `solarPanel` |
| tank | shuttle | 16×3 | under the belly |
| radiator | freighter | 16×6, 1–3 | on top, between the aft rooms and the housing |
| collar | freighter | 6×3 | on top, after the radiators or past the bridge |
| truss | freighter | 11×3 a segment | under the keel, from the aft rooms to the bow |
| container | freighter | `salvageContainer` | under a truss segment, about 78% of them |
| boom | freighter | 8×2 | at the bow tip |

- **Placement:** `Shipyard` places them after the rooms from a draw stream of their own (2), so a seed's rooms stay as
  they were. They stand in cells with no structure and no back wall, touching a hull wall. `Grid.Lay` already writes
  there, and machines need no floor.
- **The frame grows to hold them.** The door and spawn keep their place on the vessel.
- **The nozzle is its own machine**, not `Exhaust` on the main engine. As `Exhaust` it would come apart only with the
  engine, and a breach could not later take it alone. Its picture's throat meets the stern wall, so it needs no reach.
- **Bells go:** `VesselClass.Bells` and the bell walls are removed. Every deck below the first holds a main engine: one
  nozzle on a 2-deck vessel, two on 3 decks.
- **Pictures:** a new anime-art subject `vessel` draws each part after the mockup's forms, lit from the upper left, plus
  `_lit` for beacons.

**3. Hull shape and skin.**
- **Shape (sim):**
  - Past the aft rooms the body splits into modules every 22–38 columns. Each bulges 0–2 rows up and down, with no
    bulge near the bridge.
  - A rib 2 wide stands between each two modules, one row past the higher neighbour.
  - The freighter gets a housing 6 wide behind its bridge.
  - All of it is solid hull. Deck armour is still measured from the body, so rooms keep their size.
  - The bow is a flat run, then a line of slope 1:1 or 1:2 (class content). Its edge becomes a few chamfer tiles instead
    of a staircase of uneven steps.
- **Skin (client):** GridView's existing dress layer gets two sources of one `IHullDress` (cell and open sides → picture
  over, picture instead, edge picture): the wreck's and the vessel's. Every tile comes from a finite set in the ShipTiles
  atlas, drawn per livery (4):
  - **Panels:** chosen by column within an 8-cell panel and by row role: lit top edge, shaded bottom, middle.
  - **Ribs:** their own tone.
  - **Chamfers:** half-cell pictures, used only where the cell's sides are open as described; otherwise a square wall.
  - **Stripe:** in the bottom armour row.
  - **Wear:** rust at seams and pits, placed by rule.
  - **Outline:** heavy ink where an open side faces space (no back wall), light where it faces a room. That gives one
    line round the whole vessel, and a cut hole inks itself.
- **Name and code:** drawn in Godot's font in the livery's paint, worn by a mask. A letter is no longer drawn once any
  cell under it is gone. Names are i18n keys.
- **Cost:** the frame is walked once as the surface loads, then only changed cells from the grid's log. TileMapLayers
  only, no node per cell.

**4. Light.** Vessel kinds get no ambient light, as underground has none: inside is dark, outside is starlit. A client
`VesselView` (`ILightSource`) gives these lights, each only while its machine or cell stands:
- **Room lamps:** each lamp's state is drawn from the seed: red emergency (the `emergencyLamp` look's colour), dead, or
  the vessel's one dying lamp (`Flicker` at a fixed share). The first deck's aft room is always red.
- **Screens:** consoles' screens, three in four lit, some glitching, plus the main engine's amber indicator.
- **Door dots:** a red dot over each door the grid says is shut.
- **Beacons:** red on masts, a white strobe on the boom (on a shuttle, its bow-tip cell), amber on the stern's top RCS.
- **Rejected:** new lamp machine kinds (yields change, rows differ only in look) and Godot lights (0029).

**5. Where it is heading.**
- **Wrecked vessels (next):** they reuse the hull dress with `WreckDress`'s torn pictures at breach edges, and the rule
  that looks vanish with their cells. `Damage` already removes outside parts in its crater, as it removes any part.
- **Corvette, frigate, cruiser (V-2, V-3):** they add `Outside` rows and class ranges.
- No other hooks.

## Determinism and co-op
- **Inputs:** no new commands; everything still enters through the existing tool hits.
- **Shape:** drawn with `Shipyard`'s local `Rng` on stream 2, all in integer cells. Shares of a run are rounded down.
- **Looks:** derived from `SurfaceContext.Seed` by a fixed integer hash, so every peer sees the same name, lamps and
  frost. Blink and flicker phases use each client's clock and run out of step between screens; accepted.
- **Mid-session join:** the snapshot carries the grid, the looks are derived again, and dismantled cells are already gone.
- **Save format:** no bump. New machine rows are content, the description is not saved, and a vessel world never
  outlives a load.

## Trade-offs accepted
- Chamfer cells are full cells, so a hero jetpacking along the bow bumps an invisible corner. It happens only outside and
  rarely; there are no slopes in the grid (0106).
- A part on a cut hull floats in space, because machines need no floor.
- The toppled crate and the fallen panel look like things but are pictures: the pointer names nothing and they hold no
  loot.
- The painted name is not in the ink finish; things drawn straight on a canvas already are not.
- Class yield totals rise: bulges, ribs, the housing, the outside parts and the second engine add to them. `Yield` ranges
  are retuned; what already exists keeps its yield.

## Tests
- **Scenarios:** the bells are gone; one nozzle per deck below the first, touching the stern wall and centred on its
  engine; every 3-deck freighter has two main engines; each class has its outside parts; a container hangs only under a
  truss segment; bulges stay within 0–2 rows; a rib between each two modules; the housing stands behind the bridge; the
  bow's slope is an allowed one; the door and spawn are unchanged relative to the first door; a part broken by a tool
  gives its `Gives`.
- **Invariants (FsCheck, class × seed):** parts never overlap; every outside part is on cells with no structure and no
  back wall, touching the hull; everything is inside the frame; the decks' open rows are untouched; the yield is within
  the class range; `Opened` lays every part.
- **Client (`game.tests`):** the same seed gives the same dress; 3–5 signs and exactly one dying lamp; a look piece is
  hidden once a cell under it changes; a chamfer is drawn only where the sides are open as described.

## Cost
- **Modules:** `Shipyard` and `Vessel` (changed); `Content.Vessels` (`Outside` rows, class ranges, `Bells` gone); GridView's
  dress, now two sources; ShipTiles; a new client feature `vessels` (dress, `VesselView`); light (vessel kinds' ambient);
  `game/Content` `VesselLooks`; i18n names; the anime-art subject `vessel`. One ADR superseding generated-vessels in part.
- **Content:** about 11 part rows, class ranges, 4 liveries, 14 names, sign and lamp odds.
- **Estimated diff:** about 600 lines of sim with tests, about 1,000 of client, about 900 of art scripts, plus pictures.
- **Stages,** each playable in `--vessel`:
  1. Shape: bulges, ribs, housing, bow line; bells become nozzles; the second engine.
  2. Skin: panels, tone steps, outline, chamfers, livery, name, rust, pits.
  3. The other outside parts and their beacons.
  4. Light inside: dark, lamp states, screens, door dots.
  5. Signs of abandonment.

## Open questions
- **Nozzle yield:** should it give the `nozzle` item? That is a gated engine part (from the Former, out of alloy scrap and
  copper), so a derelict handing it out opens the engine and the salvage drone early. Default: plates and copper only.
- **Doors:** the mockup has open and jammed doors, which the rules do not mention. They stay shut; add them?
- **Art picks:** is the mockup the pick for every outside part's look, or does each part get a candidates page?
- **Names:** do English players get translated names through i18n, or the mockup's Cyrillic names for everyone?
