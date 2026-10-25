# Power

## Files

| File | Purpose |
| --- | --- |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Power.cs` | The module: its queries, operations and commands, charging and settling stores. |
| `PowerNodes.cs` | Nodes, spans and networks, and the `Cable`, `Ends` and `PowerNetwork` records. |

Electricity: cables laid point to point between nodes, the networks they join, and what share of its users' draw each
network can give (0017); cables move by hand (0021); stores keep what is spare (0101).

- **Depends on:** `Grid` (machines and what is built, to find nodes); `Rooms` (whether a panel stands in space);
  `Items` (`IItemsQueries`, `IItemsOps`: a cable's cost, what a charging station holds, modules' draw).
- **State** (`PowerState`, saved): `Cables`, each as its two node cells, the lower first, and whether it was paid,
  ordered by its ends; `Resting`, machine ids told to rest, sorted; `Stores`, `StoreCharge(Machine, Energy)` in
  power-ticks by machine id, only those holding something (0101). A battery's charge is the item's, saved by `Items`.
- **Derived**, found from the cables every tick in derivation: `Networks` in id order (nodes, supply, demand,
  satisfaction; stores' `Stored`, `Capacity`, `Flow`, `Intake`; `Steady`, what its panels make); `Satisfaction` per
  cabled user; `Flows` into each cabled store, out below 0. The machines are those at cable ends (`MachineAt`), so a
  tick costs what is cabled, not the ship's machines.

## Rules

- **A node** (0017) is a machine that makes, draws or stores power (`MachineSpec.Powered`), named by its top-left
  whichever cell a cable is laid at; a **run with a plug** (a launch platform, `StructureSpec.Plug`), named by its left
  end, whichever of its cells a cable is laid at; or an **anchor**: any other cell with a wall, floor, door or back wall and no
  machine, existing only while a cable ends there.
- **A cable** (0017) joins two different nodes whose nearest cells are within `Content.Power.MaxSpan`, one cable a
  pair. It takes no cell: it runs through walls and leaks no air.
- **Moving an end** (0021): `Move` takes every cable ending at a node, or one named, to another node; none may then
  span farther than a cable does. Moving costs nothing.
- **A network** (0017) is the nodes cables join, named by its lowest node cell. A powered machine with no cable is on
  none: it makes and gets nothing.
- **Supply:** its makers not resting; a fuel burner (`FuelSlot`, rested by `furnace` while its fire is out, 0045)
  anywhere, any other only under the stars: every cell in space with no back wall behind (0043).
- **Demand:** each drawing user's `Draws` × (1000 + its modules' `Draw`) / 1000, rounded up (0086). A station
  (`Content.Stations`) draws whenever crafting does not rest it, whatever its slots hold (0057); an analyzer only while
  its slot holds something (0024); a charging station only while something it holds is short of full (0053); none
  told to rest. A machine taken apart leaves `Resting`.
- **Satisfaction** (0017): `floor(1000 × served / demand)`, at most 1000, 1000 with no demand; served is supply plus
  what stores give out. Every user on it works at that share, a resting one included.
- **Stores** (0101): a machine with a `Store` holds up to its `Capacity` in power-ticks. `PowerNodes.Settle`, in
  derivation, from what each holds as the tick begins, stores in reading order of their cells: spare goes in, each up
  to its `In` and room left, the rest wasted; short, each gives up to its `Out` and what it holds until the shortfall is
  met. A store never takes from another. The simulation phase adds each flow to what it holds.
- **A fire is wanted** (`Needed`, 0101) while the network draws, or its stores could take in, more than its panels make
  (`Steady`); `furnace` lights a burner's next fuel only then. Other fires are not counted, so two burn together.
- **Charging** (0053): each tick in simulation, after the networks, in machine id order, a charging station puts its
  `Draws` × satisfaction / 1000 in thousandths (`Content.Charge.Scale`), rounded down, evenly into the slots short of
  full, the remainder one each to the first in slot order (`IItemsOps.Charge`), never past full. A drone bay shares it
  between docked drones (0062) and takes no module. Power owns this: it knows satisfaction and depends on items.
- **A map's cables** (`Layout.Cables`; the wreck's from `Content.Wreck.Cables`) count as paid; one ending on no node
  fails the load (`FormatException`) (no ADR).

## API

- **Queries** (`IPowerQueries`): `Cables`; `Networks`; `NodeAt(cell)`; `NetworkOf(node)`; `Feeds(cell)`, a structure a cable
  ends on with power enough: its network makes or stores some and covers its demand (a launch platform's lamp); `Satisfaction(machineId)`,
  0 off every network; `Draw(machineId)`, modules folded in (0086); `LayProblem(peer, a, b)` and
  `MoveProblem(from, to, cable?)` for the ghost and a dragged end; `Charges(machineId)`, filling this tick; `Store`, a
  `StoreLevel(Energy, Capacity, Flow)` or null; `Needed`; `ChargeRate`, scaled, 0 while charging nothing (0100).
- **Operations** (`IPowerOps`): `Rest(machineId, resting)`, from the next derivation: `research` rests an analyzer
  with nothing to learn, `crafting` an idle station (0057), both from simulation; `furnace` a cold burner from
  construction, so it shows this tick (0045). `Unplug(machineId)`, by `building` taking a machine apart: its cables move
  to a peg at its plug's cell (`Plug` pixels, eight to a cell); `Plug(machineId)`, by `building` building a powered
  machine: cables on a peg at its plug's cell move to it (0110). A cable that would end where it starts or join a pair
  already joined stays.

## Commands

`Lay(A, B)`, paid from the sender (`Content.Power.CableCost`; creative mode pays nothing and gets nothing back):
`power.notAboard` (0110), `power.noNode`, `power.sameNode`, `power.tooLong`, `power.twice`, `items.notEnough`.
`Cut(A, B)` gives back what was paid: `power.noCable`, `items.full`. `Move(Node, To, Cable?)`: `power.notAboard`,
`power.noCable`, `power.noNode`, `power.sameNode`, `power.twice`, `power.taut` (0021). `From` is the sender.

## Invariants

- Cables join two different nodes, the lower first, in order, each pair once, none past `MaxSpan` however moved.
- Satisfaction is within 0..1000; `floor(draw × satisfaction / 1000)` summed never exceeds supply plus stores' output.
- Energy is kept: supply + given out = drawn + taken in + wasted, waste never below 0; a store stays within 0 and its
  capacity, moves at most its `In` or `Out`, and stops short only at one of those limits (`StoreTests`).

## Deliberately not handled

- An anchor's wall taken apart or broken leaves the cable in the air, working: refusing needs `building` to read cables.
- A machine built over an anchor without its top-left there, or plug on a peg, leaves the anchor inside it (0110). One
  taken apart other than by `building` (a meteor, a dev clear) leaves its cables at its top-left, in the air.
- A peg may stand farther than a span from a cable's other end, by the machine's size: only laying and moving check.
- No priorities, switches or overload; a store holds nothing back and never leaks. A store taken apart loses its charge.
- Networks are found from scratch every tick: tens of cables. A cache would also have to follow slots and rests,
  which do not move the grid's `Version`.
