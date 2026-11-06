# Lighter electric tools

- Status: accepted; amends [0053](0053-charge-belongs-to-the-item.md)'s drill draw and [salvage-tools](2026-11-04-salvage-tools.md)'s grinder and cutter draws
- Date: 2026-11-06

## Context

The playtest found the drill's battery painful: 30 s of drilling, two or three asteroids, then the trip back and 30 s at
the charger. The walk and the wait outweighed the work. Two ways were weighed: a battery four times larger, or tools that
draw less. A larger battery makes charging four times longer too, and grows every other consumer of the belt with it (the
railgun's shots, the regen suit's air). A lower draw keeps the charger's 30 s and leaves the railgun and the suit alone.

## Decision

- **The drill draws 1** (was 3): a battery lasts it 1800 ticks, 90 s; fitted with the arc, 2, 45 s.
- **The grinder draws 2** (was 4): twice the drill, its price for three times the drill on wrecks; 45 s a battery.
- **The plasma cutter draws 1** (was 2): 90 s. **The power driver keeps its 1.**
- Draws stay whole power units, as a machine's: the drill, the cutter and the driver now draw alike, and only the grinder
  costs more. If 90 s proves short, or the tools want telling apart by their draw again, the next step is a draw in
  thousandths of a power-tick, which the stored charge already counts in.
- The battery, the accumulator, the charger, the railgun and the regen suit are unchanged.

## Consequences

- Drilling costs the ship a third of the power it did: one charger drawing 3 keeps three drills running.
- A module that lowers a draw can no longer lower the drill's: rounded up, 1 stays 1.
