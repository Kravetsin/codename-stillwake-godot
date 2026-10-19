# 0062 — Drones keep machines going

- Status: accepted; supersedes [0002](0002-coop-model.md)'s line on drones as host-only physics
- Date: 2026-10-18
- Design: "drone logistics" (approved with its cuts, 2026-10-18), replaced by "drones v2: drones keep it going" (the
  user's decisions, 2026-10-18); mockup "Drones Keep It Going"

## Context

Every item moved between machines by hand. While the crew was out salvaging, production stopped as soon as an
input ran dry or an output filled. 0057 gave the Former and the assembler input slots and entries fed from them;
nothing filled those inputs but a hero. 0002 had drones as bodies in the host's physics, outside the hull; the drones
wanted now fly inside it, between machines, and what they carry is the economy itself.

A first version of this decision followed Factorio: requests set per machine with a count, crates set as storage,
providers or requesters, request slots with a wheel counter. Built and played, the user found it unintuitive: too much
to set before anything moved. This version replaces it.

## Decision

- **Drones are sim entities,** in a module of their own, `Logistics`, not host physics: what they carry changes
  inventories, so every peer must fly them alike. Positions are integers, thousandths of a cell; order is by id and
  slot. This supersedes 0002's "Rapier simulates … drones".
- **Nothing is set; demand comes from what slots hold.**
  - **An input holding an item** is kept full of it: when what it holds and what drones bring is under half a
    stack, drones bring the rest of a stack. A fuel slot is an input like any other: planks in a furnace's fuel bring
    planks. This holds for fed stations (the furnace, the smelter, the grow capsule, whose seeds are so kept topped
    up) and for machines with a fire (the burner generator).
  - **A queued station** (the Former, the assembler) wants what its **head entry** needs when that entry is fed from
    its inputs: the cost of its units not begun, at most a stack of each item, short when under half that or under
    one unit's. A paid head entry wants nothing; so does what its inputs hold but the entry does not use.
  - **An input remembers its item** (`Items`' `Kept`, by machine and slot, sorted, saved): the last item put in
    it, by a hand or a drone, and kept through the slot being burnt, smelted or grown down to nothing. Drones refill a
    remembered slot as one holding its item, empty included, so a furnace whose chunks ran out is fed again as soon as a
    crate has some. Only a hand taking all of it out (a `Move` or a `Drop` that empties the slot) makes it forget.
    Items owns it: it sees every write of a slot and tells a hand from a fire; logistics only reads it (`KeptAt`).
  - **An input that remembers nothing is left alone.** So silicon never ends up in a furnace by itself, and a machine
    the player has not started stays as it is.
  - **Outputs** (every machine's slots past its inputs, the pod's and queued stations' too) are carried to a crate:
    one already holding the item first, then the nearest with room, then the lowest id.
  - **Sources** are crates and machines' outputs. Crates are plain storage: no roles, no lamps.
  - Analyzers, charging stations and bays are never served.
- **A per-machine switch:** `SetServed(Machine, On)`; a switched-off machine is neither a source nor a target.
  Saved as the sorted ids of machines switched off. Refused `logistics.noMachine` and `logistics.bay`. The badge at the
  right of a machine's frame header shows it and toggles it.
- **Matching,** from where a drone is (a bay, or a drone free in the air): every refill and every output a crate has
  room for, the one whose source is nearest by Manhattan distance, then the lowest source id and slot, a refill before
  an output, then the lowest target id. A drone carries a whole stack: the least of what is wanted, the source's
  stack and the room at the target; an output, all there is when it arrives up to the crate's room. What does not
  fit at a target goes on to a crate with room, by the same choice as an output; with none it lies there.
- **A drone bay** is a machine of four slots and a charging station; docked drones are items in its slots (a drone, a
  stack of one, made at the assembler). A bay looks every `LookEvery` ticks, spread by its id, and launches one drone at
  most a look: its first holding `LaunchAt`. Its **zone** is the square of cells within `Reach` of its footprint; one
  flood from the bay (or the drone) through passable cells inside it says what is reached. Two zones may overlap; what
  drones bring counts for every bay. Reach stays fixed; overlapping zones and the switch cover the rest, and a bay
  module may extend it later.
- **Job after job:** after a delivery, or a source found empty, or a goal taken apart, a drone looks for its next job
  from where it is. It flies home only when its zone has no work it can do, or its charge is low.
- **Charge** (0053): a drone is a battery, `Charge.Drone`. It spends `Logistics.Draw` power-ticks of it each tick it
  flies (not an item's `Draw`, which a hero's tool draws from the belt). A bay draws 4 and **Power charges every slot
  of a charging station** short of full, sharing what it draws between them, the rest of the division one each to the
  first in slot order: the charger's one slot is charged as before. A drone launches holding at least `LaunchAt`, half
  of full. Before a job it checks that its charge covers `Draw` × the Manhattan length from where it is to the source,
  on to the target and home, each leg from a machine counted from its farthest cell, and two cells; if not, it flies
  home marked low. In flight, at each cell centre, one
  breadth-first search finds both its next step and the way home; on a job, holding no more than the way home and two
  cells, it turns home marked low. Moving a cell costs at most what its way home shrinks by plus one cell, so it gets
  home: **no drone goes flat in the air** while nothing lengthens its way. One that cannot pay a tick (a wall built
  across its way home) drops.
- **Flight:** `Speed` thousandths of a cell a tick, cell centre to cell centre, four neighbours. Passable is a cell
  holding air or a door's, closed doors included; doors are never opened. Nothing about a path is kept. A cell ahead
  that stops being passable turns it back to the centre it left. **Its goal cut off** (a wall, a room vented beyond
  it), it flies home, its cargo with it. **With no way home** — its cell airless, no path home, its bay gone — it
  drops where it is, itself and its cargo as floor stacks.
- **At the source** it takes what is there up to its job, less if a hero took some. **At the target** it puts in what
  fits — a fuel into the fuel slot, anything else into the inputs but the fuel slot, a crate across its slots — and lays
  the rest on to a crate with room, or on the floor there with none. **Home,** it docks in a free slot of its bay and lays any cargo it brought back beside it,
  or lies down where it is with no slot free.
- **Items gains machine-to-machine operations** for this: `TakeFrom(id, slot, count)`, `PutIn(id, stack)`,
  `LayDown(stack, cell)`, and the queries `HeldFor` and `RoomFor`; and `Charge(id, slot, amount)` names its slot. Moves
  by hand keep their rules.
- **Reservations are derived** from the drones in flight, never saved: a drone on its way to a source holds that slot;
  what drones bring counts against what a target wants. A drone free in the air leaves its old job before it looks.
- **A bay's numbers are read in one place** (`BayOf`), so machine modules fold in there.
- **The assembler gets a fourth input,** as the content rule asks of a drone's four parts. A crafted drone holds no
  charge, as a crafted battery does not; the battery in its cost loses its charge there.
- **Format 17** (unreleased, so its contents changed with this version): the logistics slice (`off`, `drones`,
  `nextId`, `ticks`), empty, its clock the world's tick; the assembler's fourth input, empty, after its three.
- **The client:** an input that remembers its item shows a small sky dot in its corner while drones serve the
  machine, and, empty, a faint ghost of the item; its tooltip says "Drones keep this full: X", and "none in reach" when
  no crate or output a bay reaches holds it (`Supplied`). A drone badge at the right of every frame header where drones have work (crates, fed stations,
  fire machines, queued stations): a drone on sky while served and reached, dim and crossed while switched off, grey
  where no bay reaches; the one tooltip says which and what a click does. A bay's frame shows four drone slots, each
  slot's bar its drone's charge (green, amber under a quarter), and a bolt with what it draws, lit while it charges;
  its zone is outlined, dashed sky, while its frame is open or a bay blueprint is in hand. No seconds anywhere.
- **The look and the sound** (the user's picks, "Drone Look Concepts" and "Drone Sound Audition", 2026-10-18): the drone
  is a steel quad, the bay a perch with four pads (`game/Content/DroneLook.cs`). A flying drone is its picture, lit
  with relief, on a spring after the sim ("lively": it leans into its speed and acceleration, bobs, faces its way, and
  its cargo swings from a clamp), 8 pixels over its cell's centre or 2 under a solid cell (a door's own cells are
  no ceiling inside it, so it flies through a closed door at its height); it takes off from the pad it
  left and lands on the one it docks in. Its lamp is sky while it works and blinks amber flying home low; a docked drone
  shows its charge in four lamps by quarters, its pad's strip pulses green while it charges, and the perch's
  beacon shines while the bay has power. Heard, all quieter than a station at work: one loop a drone, a fan's air whose
  pitch and volume follow its motor and speed and waver, each drone tuned apart (−36 LUFS standing); the same soft thud
  and latch as it lifts off and touches down (−33); a small switch's click as its clamp takes a stack or puts it in
  (−35); no beep for a low drone. All client-side: the sim is unchanged, and each peer draws and plays what it sees.

## Amendment, 2026-10-19: a trip takes every slot of its item

Played: a burner generator was refilled late and only to thirty-odd planks while its crate still held planks. A drone
took from one crate slot, so with planks split over slots it brought that slot's count, and the generator, then at
half a stack, was short no longer. The user's decision:

- **A drone is sent for an item, not a slot.** What a source offers of an item is summed over its slots (a crate's
  all, a machine's outputs); at the source the drone takes up to its job from every slot holding it, the last first,
  each stack alike the first, as one stack. One trip tops a machine up to a stack when the crate holds that much.
- **Reservations are by count:** what drones on their way to a crate will take of an item is held back from what it
  offers, so two drones share one crate's planks and never go for the same ones. A machine's outputs of an item are
  held whole while a drone is on its way to them, as a carry-off takes all there is when it arrives: one at a time, as
  before. A crate sorted or emptied by hand mid-flight gives the drone what of its item it holds then.
- Every other rule stands: the half-stack trigger, the nearest source, a refill before an output, what is in flight
  counting against what is wanted, a crate holding the item first for an output. A drone's saved `slot` is dropped;
  older saves load, the field ignored.
- **Salvage stacks are bigger,** so a furnace holds more than a few trips' worth and crates split it less: a hull plate
  5 → 20, a scrap chunk 10 → 50, asteroid ore (`rock`) 10 → 50. Saves need nothing: no saved stack is over the old limit.

## Rejected

- **Forgetting an input's item when it runs dry,** this version's first build: a furnace burnt down while its supply
  crate was empty was never fed again, even once the crate was filled.
- **A right-click on a remembered empty slot to forget it:** a command, a refusal and a fuzz case for what taking the
  stack out, or switching the machine off, already does.
- **Requests and crate roles (Factorio's supply and demand),** this decision's first version: too much to set
  before anything moved. What a slot holds says what to bring; the switch says what to leave alone.
- **Drones home after every job:** a round trip per delivery, and the lowest ids first when drones were scarce.
- **A grace period in vacuum, or a drone dropping when only its goal is cut off:** a drone falling in a breathable
  room for a goal it can no longer reach reads as a bug; it flies home instead. One with no way home drops at once.
- **Charging one drone at a time,** left to right as a belt drains: every docked drone charges, as the user asked.
- **A per-bay zone size:** overlapping zones and the switch do it; a bay module may later.
- **The bay's connected aired region as its zone:** the whole ship, shifting with every door and breach.
- **Saved paths, replanning by the grid's log, a cap on paths a tick:** a drone finds a short path at every cell
  for little; nothing kept means nothing to keep right across a load.

## Consequences

- With refills at half a stack and whole stacks carried, trips are few: in the testbed two drones keep a furnace, the
  Former and a grow capsule going for ten minutes and carry what five machines make, most jobs chained in the air.
- A drone home low waits docked until it holds `LaunchAt`: with no power at its bay it never flies again.
- An output is carried off as soon as there is some and a drone is free; while drones are busy it gathers, so
  carries grow by themselves.
- A furnace whose supply runs out waits, its slot remembering its chunks, and burns again once a crate has some.
- With two drones and one bay drawing 4, charge is what limits them: both home together charge a while, and a furnace
  may stand empty for some seconds between deliveries.
- Cargo a drone turned home with lies at its bay; leftovers with no crate to take them lie at the target; a full set
  of crates leaves outputs where they are.
- The testbed's two rooms leak more than one oxygen generator makes: on main room two runs dry in about three
  minutes. The testbed now has three generators.
- A bay with docked drones cannot be taken apart, as no machine holding items can.
- Drones carry through closed doors and never let air through: an airless airlock between rooms stops them.
