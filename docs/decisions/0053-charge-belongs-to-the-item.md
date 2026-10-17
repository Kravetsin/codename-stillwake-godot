# 0053 — Charge belongs to the item; batteries and the charging station

- Status: accepted
- Date: 2026-10-17

## Context

Electric tools come next, a drill first, then cutters and laser guns, all running on batteries worn on
the belt (0047). A battery is a general store of energy, not any one tool's, and the first item that
holds something of its own: two batteries are not alike when one is full. Until now a stack was an item
and a count, and stacks of one item merged. Tools will wear down later, the next value of the same kind,
so the shape chosen here is the one durability will take (0049).

## Decision

- **Energy is measured as machine power is.** A power-tick is a unit of power drawn for a tick: a machine
  drawing 3 for 600 ticks uses 1800. **Stores declare a `Capacity`, consumers declare a `Draw`, both in
  these units** (`Content.Charge`). Charge is stored in thousandths of a power-tick (`Charge.Scale`), so a
  station's share of power, in thousandths, adds whole numbers. **No per-consumer time is ever shown**:
  how long a battery lasts depends on what draws from it, and the player feels it rather than reads it.
- **Per-item state lives on the stack.** `Stack(Item, Count, Charge = 0)`: `Charge` only on items whose
  stack size is 1 and which declare a `Capacity` in content, from 0 to it. Every copy is made with
  `with`, so it goes with the stack through a move, a swap, a crate, a drop and its pick-up;
  `FloorStack.Charge` carries it on the floor, converted in one place (`AsStack`, `LayDown`). Stacks
  merge, in a slot or on the floor, only when alike (`Stacks.Alike`: one item, one charge); otherwise a
  whole stack moved onto another swaps, so two batteries swap. A later value (wear) is one more field
  with a default, compared in `Alike` and carried by the floor the same way; nothing else changes.
- **Made empty.** Stacks built from a cost, given, crafted or refunded, hold nothing: a crafted battery
  is empty. No cost names an item with a capacity (a content test), so paying never destroys charge.
- **Saves** write `charge` only when it is not 0, so a save without batteries is as it was. Format 12,
  its step an identity: it keeps an older build from loading a battery and dropping its charge.
- **The battery:** `battery`, a stack of one, `Capacity` 1800 power-ticks (`Charge.Battery`, stored
  1,800,000), worn on the belt. Made at the lifepod of a
  plate, a coil and two wire in 60 ticks, opened by analysis. Its picture is the one picked; empty, it
  shows `battery_empty`.
- **The charging station:** `charger`, 1×2 cells, one slot taking only items with a capacity (refused
  `items.notBattery`, put or swapped in), draws 3, costs a frame, a coil and four wire, opened by
  analysis. **Power charges it:** it already knows each machine's satisfaction and depends on items.
  A station (`Charges` in content) puts in what it draws: each tick, after the networks are found, its
  `Draws` × its satisfaction / 1000 power-ticks, rounded down in stored units, through the items op
  `Charge(machine, amount)`, never past full. Drawing 3, it fills 1800 in 600 ticks at full power, twice
  that at half. Empty or full it draws nothing. Its picture is a placeholder:
  a cabinet with a light, pulsing green while it charges and steady once full and powered, and a bay
  showing the battery's charge rising; the light gives light (a `Glow`).
- **Consumers.** An electric item declares its `Draw`, in power units, as a machine does; the drill will
  draw 3 while drilling, so a full battery lasts it 30 s, a fact no screen states. Its card shows the
  draw as a machine shows its own: a bolt and the number.
- **Drain order.** `Drain(peer, amount)` takes `amount` power-ticks, a tool's draw for a tick, whole from
  the first battery on the belt, in slot order, that holds at least that much, and says whether one did:
  the belt drains left to right, one battery at a time. It has no caller until the first electric tool;
  it is tested now. Creative mode takes
  nothing and answers yes.
- **On screen, without numbers where it is always seen:** a slot holding a battery draws a thin bar
  along its icon's foot, green, amber under a quarter, an empty track at none. The card adds a CHARGE
  meter: percent of full, a bar, and what it holds of full in power-ticks with a bolt (`⚡ 1170 / 1800`),
  rounded down; no seconds. The station's panel (the mockup):
  its name, a bolt with what it draws lit while it charges, its slot, a bar and "empty / full /
  charging / no power".
- **The energy bar** sits right of the suit's gauge and shows the charge of the belt's batteries
  (`BeltCharge`) of what they hold full, only while the free hand holds an electric item (one with a
  `Draw`), in the colour of that item's energy: charge's green. Another kind of energy recolours it by
  what is held. No item is electric until the drill; `-- --electric=<item>` counts one as electric in
  a dev run.

## Rejected

- **Charge in a tool's own units** (ticks of drilling) and seconds left on the card: the battery serves
  many tools, so a unit of one would mislead for the rest. Power-ticks are what machines already speak.
- **A dictionary of named values per stack** ("charge", "wear"): order and hashing would need care, and
  every reader would look values up by name. Two typed fields are clearer, and the third can wait for
  its user.
- **Charge kept by machine or by hero, beside the items:** it would stay behind when a battery is moved,
  dropped or put in a crate, and every move would have to carry it across by hand.
- **Charging in items or in a module of its own:** items knows no power; a new module would read power's
  satisfaction to do what power can do where it finds it.
- **Drain from all batteries at once, or the fullest first:** the belt reads left to right, so the player
  sees which battery is spent and which to swap.

## Consequences

- Two batteries holding different charge swap where two full stacks of one item would be refused.
- A battery holds its charge anywhere: in a crate, on the floor, on a belt. Only a tool spends it.
- A battery put in a station without power keeps what it has; the panel says "no power".
- The desync fuzz draws batteries into stations and onto belts, and a desync test runs stations charging
  while the fuzz moves batteries between belts, crates, the floor and the station.
