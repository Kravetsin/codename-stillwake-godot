# The charger holds two batteries, drawing for each

- Status: accepted; amends [0053](0053-charge-belongs-to-the-item.md)'s charging station
- Date: 2026-11-03

## Context

The first playtest up to the landing found batteries wanted all the time and the charger slow: 600 ticks, 30 s, for one
battery at its draw of 3. Charging one faster would need it to draw more than an early panel makes (a battery in 15 s is
a draw of 6; a solar panel makes 5), or energy from nothing.

## Decision

- **Two slots, the same footprint** (the user's idea): 2 by 4 cells as before, its bay split into two, one over the
  other. Worlds keep every charger where it stands.
- **It draws its 3 for each battery short of full** (`MachineSpec.ChargesEach`): 3 with one, 6 with two, none when
  empty or full. Each fills as fast as one alone, so the charger fills twice the batteries in the same 30 s.
- **The drone bay keeps sharing** its 4 between its drones: changing it would rebalance the drones, which nobody asked.
- **Format 47:** a saved charger's crate of one slot gets its second, empty; what it held stays in the first.

## Consequences

- Two batteries on one solar panel ask 6 of its 5: the whole network works at 833 a thousand, both batteries filling a
  little slower and every other machine on it too. A player's choice, left as it is.
- The panel shows a line a battery; the station's picture shows each battery's charge in its own bay.
