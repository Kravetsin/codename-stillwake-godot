# The engine stands at the stern, its nozzle out through the hull

- Status: accepted
- Date: 2026-10-25

## Context

The first stage ends with an engine and a radar (concept): the radar finds what is out there, the engine takes the ship
there. The radar came first, with the ship's computer. The user asked for the engine now as a thing to build, the last
of all, its nozzle looking out through the wall, a slot of biofuel opened with E, cabled to power, and not yet run: the
jump between places is to come. What it is made of was open (concept, Open).

## Decision

- **A machine, `engine`**, 6×4 cells, on the ship tab, its card the last there. It draws 2 (`Draws`) as the analyzer does,
  only while its slot holds something (Power's rule for a machine with slots): pumps and igniter kept ready while there is
  fuel. It holds one slot, opened with E as any machine's.
- **It takes biofuel alone.** `MachineSpec.Takes` names the one item a machine's slots hold: a move putting or swapping
  anything else in is refused `items.notTaken` (`Stacks.Unfit`), and drones deliver it nothing else (`Receiving`). Not a
  `FuelSlot`: that would light a fire in it and burn the biofuel as the furnace does.
- **Its nozzle reaches through the hull.** `MachineSpec.Exhaust` is how many cells (3) its nozzle reaches out of its left
  side, the stern's: the ship moves right, debris drifting left past it. Machines still stand only on empty cells, so the
  footprint is the engine's block and the nozzle lies outside it. The grid refuses a placement `grid.noExhaust` unless, in
  every row of its footprint, the cells left of it are one wall (`Trait.Terrain`) or more, then space (no structure, no back
  wall, no machine), up to the nozzle's tip or, where the wall fills its whole reach, the cell past it. A one- or two-cell
  hull works; a thicker one, a room past the wall, a machine in the way or no wall does not.
- **Its picture is that much wider.** `machine_engine` is (6 + 3) × 8 by 4 × 8 px; the client lays a machine's tile, its
  plan, its ghost and its aim outline `Exhaust` cells further left (`ShipTiles.ReachOf`), as it lays the arc bench's vise
  higher (0099). The nozzle is drawn over the wall. `Plug` is in pixels from the footprint's top-left, as it always was.
- **The last thing opened**, of what a planet gives: the `nozzle`, pressed at the Former of the ruins' alloy scrap and
  copper; the `fuelPump`, at the Former of a motor (the deepest band's magnetite), tubes and a plate; the `igniter`, at
  the circuit assembler of a crystal, a circuit and wire. The engine costs those, two frames and four plates. Without any
  one of the alloy, the magnetite or the crystal it never opens (`ContentTests`).
- Saves: no format change; machines are saved by kind, and `Takes` and `Exhaust` are content.

## Deliberately not handled

- **Running it.** No command fires it and nothing burns its fuel: the jump, its fuel per jump and what it does to the
  ship are their own design.
- **What happens to the nozzle's cells later.** The rule is checked when it is planned and built. A wall built in the
  space past the nozzle, a machine there, or the wall taken apart leave the engine standing, its nozzle drawn as before:
  nothing it does depends on them yet. The jump will check its exhaust when it fires.
- **A bow engine, or one facing right.** The stern is on the left in every ship; a mirrored engine waits for a ship that
  needs one.
- **Its burning look.** The painter (`tools/ship-art/engine/engine.gd`) draws a glow and a flame for its preview only;
  the client draws neither till it runs.
