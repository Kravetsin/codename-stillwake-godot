# Power

Electricity: cables laid point to point between nodes, the networks they join, and how much of what
its users would draw each network can give (ADR 0017); cables move by hand (0021). Ported from
`packages/sim/src/modules/power`; its rules hold here unchanged.

- **Depends on:** `grid` (machines and what is built, to find nodes), `rooms` (whether a panel stands
  in space), `items` (what a cable costs, what a charging station holds, what a machine's modules add to its draw:
  `IItemsQueries`, `IItemsOps`).
- **State** (`PowerState`, members internal): `Cables` — every cable as the cells of its two nodes,
  the lower first, and whether it was paid for, ordered by its ends; `Resting` — the machines told to
  rest, by id, sorted: they draw and make nothing. Saved; a battery's charge is the item's, saved by
  `items`; `Stores` — what each store holds in power-ticks, `StoreCharge(Machine, Energy)` by machine id, sorted, only
  those holding something (0101). Derived: `Networks` — what the cables join, in id order, each with its
  nodes, supply, demand, satisfaction and its stores' `Stored`, `Capacity`, `Flow`, `Intake`, and `Steady`, what its
  panels make; `Satisfaction` — each cabled user's, by machine id, only looked up; `Flows` — what goes into each
  cabled store this tick, out below 0. All found again every tick, in the derivation phase, from the cables: the machines on
  them are those at their ends (`MachineAt`), so a tick costs what is cabled, not the ship's machines.
- **Files:** `PowerNodes.cs` — nodes, spans and networks, and the `Cable`, `Ends` and `PowerNetwork`
  records; `Power.cs` — the module, its queries, operation and commands; `Fuzz.cs` — its fuzz generator.

## Rules

- **A node** is a machine that makes, draws or stores power (`Makes`, `Draws`, `Store` in `Content.Machines`:
  `MachineSpec.Powered`), named by
  its top-left cell, whichever of its cells a cable is laid at; or an **anchor**: any other cell with a
  wall, a floor, a door or a back wall and no machine. An anchor is nothing but a cable's end: it
  comes with the first cable laid there and goes with the last cut.
- **A cable** joins two different nodes whose nearest cells are within `Content.Power.MaxSpan` cells,
  straight; two nodes take one cable at most. It takes no cell, so it runs through walls and leaks no
  air.
- **A cable's end moves** to another node with `Move`: every cable ending at a node, or one named;
  none may then span farther than a cable does, or it would be pulled taut. Moving costs nothing.
- **A network** is the nodes cables join, named by its lowest node cell. Its supply is what the
  machines on it that make power make, none told to rest: one that burns fuel (with a `FuelSlot`: the
  burner generator, which `furnace` rests while its fire is out; 0045) wherever it stands, any other
  only under the stars: every cell of it in space with no back wall behind it (a solar panel in a
  room, or before a back wall, is in the shade; 0043); its demand, what the machines on it that draw power draw, each its
  `Draws` × (1000 + its modules' `Draw`) / 1000, rounded up, a cost (`Find` asks a draw callback, which reads
  `IItemsQueries.Boost`; [0086](../../docs/decisions/0086-modules-one-socket-for-machines-and-tools.md)), so a station
  with the arc module draws twice and its network's share falls for every user on it: a station (`Content.Stations`: the
  Former, the circuit assembler, the electric smelter, a grow capsule) whenever crafting does not rest it, whatever
  its slots hold, for a queued one may work a paid unit with its inputs empty; an analyzer only while its slot holds
  something, a charging station (the charger, a drone bay) only while something it holds is short of its capacity; and none told to rest by the
  module it works for (`Rest`); a machine taken apart rests no more. Its
  satisfaction is `floor(1000 × served / demand)`, at most 1000, and 1000 with no demand, served being its supply and
  what its stores give out (below): every user on it works at that share, slowed alike. A resting user is still on its
  network and gets its share. A powered machine with no cable is on no network: it makes nothing and gets nothing.
- **A store** (a machine with a `Store`, the capacitor bank; [0101](../../docs/decisions/0101-a-network-stores-its-spare-power.md))
  holds up to its `Capacity` in power-ticks. Each tick its network settles (`PowerNodes.Settle`), in the derivation
  phase, from what each store holds as the tick begins, its stores in the reading order of their cells: with supply at
  or above demand, what is spare goes in, each store up to its `In` and the room it has left, the rest wasted; short,
  they give out, each up to its `Out` and what it holds, until the shortfall is met. A store never takes from another.
  The simulation phase adds each store's flow to what it holds (`Settle` in `Power.cs`). The 12000 a bank holds, at 10 in
  or out a tick, is a minute of its intake.
- **A fire is wanted** (`Needed`) while its network draws, or its stores could take in, more than its panels make
  (`Steady`): `furnace` lights a burner generator's next fuel only then. Other fires are not counted, so two burn
  together and never hand over a tick dark.
- **A charging station** (a machine with `Charges` in `Content.Machines`, 0053) puts what it draws into
  what its slots hold short of full: each tick, in the simulation phase after the networks are found, in machine id
  order, its `Draws` in power-ticks × its satisfaction / 1000, stored in thousandths
  (`Content.Charge.Scale`), rounded down, shared evenly between those slots, the rest of the division one each to the
  first in slot order, through `IItemsOps.Charge(id, slot, amount)`, never past full. The charger draws
  3 and fills a battery of 1800 in 600 ticks at full power, 1200 at half; a drone bay draws 4 and shares it between its
  docked drones (0062). Empty or full it
  draws nothing, so it takes nothing from the network. Power owns this rather than items or a module of
  its own: it already knows each machine's satisfaction and depends on items.
- A map lays its cables from `Layout.Cables`, each end resolved to its node; a cable that ends on no
  node does not load (`FormatException`). They count as paid. The wreck's come from
  `Content.Wreck.Cables` through `Registry.Wreck`.

## API

- **Queries** (`IPowerQueries`): `Cables`; `Networks`; `NodeAt(cell)` — the node a cable laid there
  ends on, or null; `NetworkOf(node)`; `Cabled(node)` — whether a cable ends there, so `building`
  refuses to take a machine apart; `Satisfaction(machineId)` — in thousandths, 0 off every network;
  `Draw(machineId)` — what it draws while it draws, its modules folded in, rounded up: its panel's bolt (0086);
  `LayProblem(peer, a, b)` — why that peer's `Lay` would be refused, for the cable ghost;
  `MoveProblem(from, to, cable?)` — why that `Move` would be refused, for a dragged end;
  `Charges(machineId)` — whether that charging station fills what it holds this tick (short of full, on a
  network giving it power): its light and its panel's bolt; `Store(machineId)` — a store's `StoreLevel(Energy,
  Capacity, Flow)`, what it holds after this tick and what went in (out below 0), null for a machine that stores
  nothing: its gauge; `Needed(machineId)` — whether a fire on that machine's network is wanted (above), false on none.
- **Operations** (`IPowerOps`): `Rest(machineId, resting)` — the machine draws or makes nothing from
  the next derivation on, or works again: `research` rests an analyzer with nothing to learn, and
  `crafting` an idle station that draws (nothing queued, a unit done waiting for room or one waiting for
  its inputs; a fed one with nothing it can work), from their simulation systems, so it shows from the
  next tick; `furnace` a burner generator whose fire is out, from its construction system, so it shows this tick.
  Resting a resting machine does nothing.
- **Commands:** `Lay(A, B)` — a cable between the nodes at those cells, paid from the sender
  (`Content.Power.CableCost`, nothing in creative mode: paid only if `Take` took something), refused
  with `power.noNode`, `power.sameNode`, `power.tooLong`, `power.twice` or `items.notEnough`;
  `Cut(A, B)` — takes the cable between those node cells away, giving back what was paid, refused with
  `power.noCable` or `items.full`; `Move(Node, To, Cable?)` — every cable ending at node `Node`, or
  only `Cable` (an `Ends`), to the node at cell `To`, refused with `power.noCable`, `power.noNode`,
  `power.sameNode`, `power.twice` or `power.taut`. TypeScript's `move { from }` is `Node` here:
  `From` is the sender on every command.

## Invariants

- Cables join two different nodes, the lower first, in order, each pair once.
- No cable spans farther than `Content.Power.MaxSpan`, however its ends are moved.
- A network's satisfaction is between 0 and 1000, and what its users get, `floor(draw × satisfaction /
1000)` summed, never exceeds its supply and what its stores gave out.
- Energy is kept: supply + given out = drawn + taken in + wasted, the waste never below 0; a store never holds below 0
  or past its capacity, moves no more than its `In` or `Out` a tick, and stops short only at one of those limits
  (`StoreTests`, two properties proven to bite).

## Tests waiting for other modules

Every test of `power.test.ts` is ported, with `atmosphere` and `building`.

## Deliberately not handled

- An anchor's wall taken apart or broken leaves the cable ending in the air: it keeps working.
  Refusing it would need `building` to read cables for every wall, and nobody misses a mount.
- A machine built later with its top-left on an anchor becomes the node there; one built over an
  anchor otherwise leaves the anchor inside it. Both are rare and harmless.
- No priorities, no switches, no overload: a short network with its stores spent slows everything on it. A store
  holds no charge back for later and never leaks.
- A store taken apart (its cables cut first) loses what it held: refusing it would ask `building` to read stores, and a
  player empties a bank by using it.
- Networks are found from scratch every tick: tens of cables, so a cache would cost more than it
  saves until the reference world says otherwise. One kept between ticks would also have to follow
  what an analyzer's slot holds and which machines rest, neither of which moves the grid's `Version`.
