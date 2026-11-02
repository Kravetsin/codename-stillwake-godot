# A magnetic flare breaks machines until they are repaired

- Status: accepted; its repair cost superseded by [free-repair](2026-10-30-free-repair.md)
- Date: 2026-10-30

## Context

The starting space had one danger, meteors, and they only break the hull. Once a machine stood, nobody had to look at it
again. The user wanted a random event: something flashes in the sky, and afterwards some of the electric machines stop
working, not taken apart, but repairable, their cables left where they are.

## Decision

- **Where and when.** A surface kind's `Flares` record (`Content/MagneticFlares.cs`) brings them: the ship's
  `StartingSpace`, one every 8 to 15 minutes, 30 s of warning, breaking 10–30% of the electric machines. Planets have none,
  a `Calm` world draws none. They are armed once the oxygen generator is known (`Research.Knows("generator")`): from then
  on the player can make whatever repairing it takes. Creative mode knows everything, so flares come from the start there.
- **The warning** is the aurora, lamps flickering and cables glowing: no text.
- **What breaks** is drawn at random from the powered machines a cable ends on and not broken, as the cables lie on the
  strike's tick: a machine with no cable works neither way, and its breaking would spend the share. `ceil(n × percent / 100)` of them, so every flare breaks one at
  least. Nothing protects a machine yet, not standing deep in the hull either.
- **Broken** is Power's: the machine stays a node, its cables, slots, queue and store with it, and makes, draws and stores
  nothing. Everything that asks Power whether a machine works (`Satisfaction`, `Needed`) stops with no rule of its own.
- **Repair** is Building's, a tick of screwdriver work as a plan's is. Its cost is each item of the machine's build cost
  but a circuit, a quarter (`RepairRules.Share`, 250‰) rounded up, so a one-item cost takes that item; its time a quarter
  of the build's. The first tick pays it all at once, from whoever works it, or is refused and takes nothing.
- **Taken apart broken**, a machine refunds its cost less the repair cost, paid or not: otherwise taking apart and
  rebuilding would beat repairing. A paid repair is not given back.
- **The dev panel** starts a flare by hand (`FlareNow(ticks)`), with its warning, in any world.

## Consequences

- Spares and walking the ship become a steady cost, as hull patches are.
- The next users are named: fuses and shielded walls will only shrink the set a flare picks from; harder places bring a
  stronger record.
- A rare-part repair costs nearly a new machine; an arc lamp with no spare stays broken.
- A broken drone bay still sends drones (logistics reads no power); a fire already lit in a broken burner burns out.
- Machines broken off screen are found only by their look or by what stops.
