# Power

Electricity: cables laid point to point between nodes, the networks they join, and how much of what
its users would draw each network can give (ADR 0017); cables move by hand (0021). Ported from
`packages/sim/src/modules/power`; its rules hold here unchanged.

- **Depends on:** `grid` (machines and what is built, to find nodes), `rooms` (whether a panel stands
  in space), `items` (what a cable costs, what a charging station holds: `IItemsQueries`, `IItemsOps`).
- **State** (`PowerState`, members internal): `Cables` — every cable as the cells of its two nodes,
  the lower first, and whether it was paid for, ordered by its ends; `Resting` — the machines told to
  rest, by id, sorted: they draw and make nothing. Saved; a battery's charge is the item's, saved by
  `items`. Derived: `Networks` — what the cables join, in id order, each with its
  nodes, supply, demand and satisfaction; `Satisfaction` — each cabled user's, by machine id, only
  looked up. Both found again every tick, in the derivation phase, from the cables: the machines on
  them are those at their ends (`MachineAt`), so a tick costs what is cabled, not the ship's machines.
- **Files:** `PowerNodes.cs` — nodes, spans and networks, and the `Cable`, `Ends` and `PowerNetwork`
  records; `Power.cs` — the module, its queries, operation and commands; `Fuzz.cs` — its fuzz generator.

## Rules

- **A node** is a machine that makes or draws power (`Makes`, `Draws` in `Content.Machines`), named by
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
  room, or before a back wall, is in the shade; 0043); its demand, what the machines on it that draw power draw, one working on
  what is put in it (an analyzer, with `Slots`) only while its first slot holds something, and none
  told to rest by the module it works for (`Rest`), and a charging station only while what it holds
  is short of its capacity; a machine taken apart rests no more. Its
  satisfaction is `floor(1000 × supply / demand)`, at most 1000, and 1000 with no demand: every user
  on it works at that share, slowed alike. A resting user is still on its network and gets its share.
  A powered machine with no cable is on no network: it makes nothing and gets nothing.
- **A charging station** (a machine with `Charges` in `Content.Machines`, 0053) puts what it draws into
  what its slot holds: each tick, in the simulation phase after the networks are found, in machine id
  order, its `Draws` in power-ticks × its satisfaction / 1000, stored in thousandths
  (`Content.Charge.Scale`), rounded down, through `IItemsOps.Charge`, never past full. The charger draws
  3 and fills a battery of 1800 in 600 ticks at full power, 1200 at half. Empty or full it
  draws nothing, so it takes nothing from the network. Power owns this rather than items or a module of
  its own: it already knows each machine's satisfaction and depends on items.
- A map lays its cables from `Layout.Cables`, each end resolved to its node; a cable that ends on no
  node does not load (`FormatException`). They count as paid. The wreck's come from
  `Content.Wreck.Cables` through `Registry.Wreck`.

## API

- **Queries** (`IPowerQueries`): `Cables`; `Networks`; `NodeAt(cell)` — the node a cable laid there
  ends on, or null; `NetworkOf(node)`; `Cabled(node)` — whether a cable ends there, so `building`
  refuses to take a machine apart; `Satisfaction(machineId)` — in thousandths, 0 off every network;
  `LayProblem(peer, a, b)` — why that peer's `Lay` would be refused, for the cable ghost;
  `MoveProblem(from, to, cable?)` — why that `Move` would be refused, for a dragged end;
  `Charges(machineId)` — whether that charging station fills what it holds this tick (short of full, on a
  network giving it power): its light and its panel's bolt.
- **Operations** (`IPowerOps`): `Rest(machineId, resting)` — the machine draws or makes nothing from
  the next derivation on, or works again: `research` rests an analyzer with nothing to learn, and
  `crafting` an idle station that draws (nothing queued, or a unit done waiting for room), from their
  simulation systems, so it shows from the next tick; `furnace` a burner generator whose fire is out,
  from its construction system, so it shows this tick. Resting a resting machine does nothing.
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
1000)` summed, never exceeds its supply.

## Tests waiting for other modules

Every test of `power.test.ts` is ported, with `atmosphere` and `building`.

## Deliberately not handled

- An anchor's wall taken apart or broken leaves the cable ending in the air: it keeps working.
  Refusing it would need `building` to read cables for every wall, and nobody misses a mount.
- A machine built later with its top-left on an anchor becomes the node there; one built over an
  anchor otherwise leaves the anchor inside it. Both are rare and harmless.
- No storage, no priorities, no switches, no overload: a short network slows everything on it.
- Networks are found from scratch every tick: tens of cables, so a cache would cost more than it
  saves until the reference world says otherwise. One kept between ticks would also have to follow
  what an analyzer's slot holds and which machines rest, neither of which moves the grid's `Version`.
