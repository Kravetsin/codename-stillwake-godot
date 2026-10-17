# 0053 — Charge belongs to the item; batteries and the charging station

- Status: accepted
- Date: 2026-10-17

## Context

The drill comes next, and runs on batteries worn on the belt (0047). A battery is the first item that
holds something of its own: two batteries are not alike when one is full. Until now a stack was an item
and a count, and stacks of one item merged. Tools will wear down later, the next value of the same kind,
so the shape chosen here is the one durability will take (0049).

## Decision

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
- **The battery:** `battery`, a stack of one, `Capacity` 600 drilling ticks (`Charge.Battery`, in
  thousandths of a tick of work, `Charge.PerTick` 1000), worn on the belt. Made at the lifepod of a
  plate, a coil and two wire in 60 ticks, opened by analysis. Its picture is the one picked; empty, it
  shows `battery_empty`.
- **The charging station:** `charger`, 1×2 cells, one slot taking only items with a capacity (refused
  `items.notBattery`, put or swapped in), draws 3, costs a frame, a coil and four wire, opened by
  analysis. **Power charges it:** it already knows each machine's satisfaction and depends on items.
  Each tick, after the networks are found, a station adds `Charges` (1000) × its satisfaction / 1000,
  rounded down, through the items op `Charge(machine, amount)`, never past full: empty to full in 600
  ticks at full power, twice that at half. Empty or full it draws nothing. Its picture is a placeholder:
  a cabinet with a light, pulsing green while it charges and steady once full and powered, and a bay
  showing the battery's charge rising; the light gives light (a `Glow`).
- **Drain order.** `Drain(peer, amount)` takes the whole amount from the first battery on the belt, in
  slot order, that holds at least that much, and says whether one did: the belt drains left to right,
  one battery at a time. It has no caller until the drill; it is tested now. Creative mode takes
  nothing and answers yes.
- **On screen, without numbers where it is always seen:** a slot holding a battery draws a thin bar
  along its icon's foot, green, amber under a quarter, an empty track at none. The card adds a CHARGE
  meter: percent of full and seconds of drilling left, rounded down. The station's panel (the mockup):
  its name, a bolt with what it draws lit while it charges, its slot, a bar and "empty / full /
  charging / no power".
- **The energy bar** sits right of the suit's gauge and shows the charge of the belt's batteries
  (`BeltCharge`) of what they hold full, only while the free hand holds an electric item (content's
  `Electric`), in the colour of that item's energy: charge's green. Another kind of energy recolours it
  by what is held. No item is electric until the drill; `-- --electric=<item>` counts one as electric in
  a dev run.

## Rejected

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
