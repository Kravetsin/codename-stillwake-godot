# 0046 — Recipes show what a thing is made of

- Status: accepted
- Supersedes: of [0028](0028-the-lifepod-and-crafting-by-machine-only.md), the pod's half speed; of
  [0034](0034-wreckage-and-the-axe.md), the axe's 6 scrap; of
  [0044](0044-the-harpoon-reels-a-piece-to-a-point.md), the harpoon's recipe; of
  [0045](0045-a-burner-generator-shares-the-furnaces-fire.md), the burner generator's cost and a
  plank's 300 ticks
- Date: 2026-10-17

## Context

Recipes were whatever was at hand when each thing was added: the fire axe was 6 scrap, the harpoon
plates, wire and scrap, a pipe a unit of scrap. A recipe said nothing of what its picture showed, and
nothing left room for what comes later (an engine, a radar, silicon and glass from the rings). A
chunk smelted into 12 scrap and a hull plate into 6 iron, so materials were never short; an analysis
took 10 s, so a new blueprint was no step at all; and the Fabricator, at half speed, was the factory
rather than the bench a player starts with.

## Decision

- **A recipe matches its picture, and the picture its recipe.** The axe is an iron blade on a wooden
  handle; the harpoon a plank stock, a frame, a tube, a coil and its line of wire. A new recipe, or a
  new picture, keeps the two in step.
- **Five layers,** each made of the ones before it: salvage (scrap chunks, hull plates, planks,
  canisters, circuits) → materials, from the furnace (scrap, iron) → parts (wire, plate, gear, tube) →
  components (rotor, frame, coil) → products (tools, machines, builds). Later layers (steel, silicon,
  glass, a motor, a lens) slot in without moving these.
- **Three new parts**, opened by analysis as every craft is (0027): `tube` of 1 iron in 40 ticks,
  `frame` of 2 plates in 80, `coil` of 3 wire in 60. Their icons are placeholders (a pipe tile shrunk,
  the plate outlined, the wire turned and tinted) until the art is picked.
- **Recipes:**

  | Thing              | Cost                                   | Was                              |
  | ------------------ | -------------------------------------- | -------------------------------- |
  | fire axe           | 2 iron, 1 planks; 60 ticks             | 6 scrap                          |
  | harpoon            | 1 planks, 1 frame, 1 tube, 1 coil, 4 wire; 120 ticks | 2 plates, 4 wire, 4 scrap; 80 |
  | analyzer           | 2 plates, 4 wire                       | 8 scrap, 4 iron                  |
  | tank               | 4 plates, 1 tube                       | 4 plates, 1 gear                 |
  | vent               | 1 plate, 1 tube                        | 1 plate, 1 gear                  |
  | oxygen generator   | 1 frame, 1 rotor, 1 tube, 2 wire       | 4 plates, 1 rotor, 2 wire        |
  | burner generator   | 1 frame, 1 rotor, 1 coil, 6 scrap      | 4 plates, 2 gears, 1 rotor, 4 wire |
  | valve              | 1 tube, 1 gear                         | 1 scrap, 1 gear                  |
  | pipe               | 1 tube a cell                          | 1 scrap                          |

  The door, lamp, crate, floor, ladder, cable and walls are unchanged; so is the solar panel, until
  its own chain is designed.
- **Smelting yields a third:** a scrap chunk gives 4 scrap in 60 ticks (was 12 in 100), a hull plate
  2 iron and 1 scrap in 100 (was 6 and 4 in 200): one plate, one iron wall. Smelting is quicker per
  item, so a unit of material costs about the fuel it did: a plank smelts some 27 scrap or 8 iron
  (was 36 or 9).
- **Fuel burns longer:** a plank burns 400 ticks (was 300), in the furnace and the burner generator
  alike; with less from each item, planks would otherwise be what runs out.
- **An analysis takes a minute,** 1200 ticks at full power (was 200): every blueprint opened is a
  step the player notices.
- **The lifepod crafts at a fifth of full speed** (`Crafts.PodSpeed` 200, was 500): a wire 5 s, a
  plate 10, a rotor 15, a frame 20, the harpoon 30. It is the most basic bench, worked while the hero
  is out salvaging. Machines for parts (a former and the like), faster and with longer queues, are
  planned; its queue stays at four.

## Rejected

- **The solar panel's new chain now** (frame, solar cells, wire): a cell wants silicon and glass,
  which want asteroids and a drill. It keeps 6 scrap, 6 iron and 3 circuits until that design.
- **A circuit in the analyzer:** research would wait on rare electronics racks.
- **Keeping the old yields and slowing only the pod:** materials would stay plentiful, and the furnace
  and fuel would still matter little.

## Consequences

- The wreck's opening plays on without a dead end (`ContentTests` checks that every recipe opens from
  the wreck's grants), but more slowly. The first furnace needs 4 chunks and 2 hull plates as before;
  the axe now a third hull plate and a plank. Before the axe, hull plates come only from small debris,
  about one in 50 s, since the wrecks that give most of them need the axe.
- Closing room B (16 wall cells, 22 back wall cells of scrap) takes 76 scrap, 19 chunks: about eight
  minutes of drifting chunks, where it was under three.
- The wreck's panel makes 5 and its generator and lamp draw 5; a cabled analyzer draws 2 more, so it
  runs at about 70% and an analysis takes some 85 s unless the lamp is unplugged. The harpoon waits on
  seven analyses: iron, plate, wire, planks, tube, frame, coil.
- Taking apart the wreck's machines refunds the new parts (the oxygen generator a frame, a rotor and a
  tube), which opens their blueprints early, at the cost of the room's air.
- No save format change: recipes and yields are content, and nothing saved names a cost.
