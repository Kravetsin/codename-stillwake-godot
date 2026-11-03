# The power driver works a square

- Status: accepted.
- Builds on [0107](0107-left-is-front-right-is-back.md) (the screwdriver builds), [0055](0055-an-electric-tool-draws-while-it-runs.md)
  (an electric tool draws while it runs) and [0104](0104-the-cell-is-8-px.md) (the 2×2 stamp).

Walls go up a cell at a time, and in the middle of the space stage a hull is minutes of holding the button. The user asked for a
better builder: the power driver (шуруповёрт), bought on the black market where the second belt slot is.

**Four ticks a tick, two at most on one plan.** `Screwdriver(Item, Work, Area)`: the power driver is `Work: 2, Area: 2`. While it
runs on the belt's charge it works the 2×2 square round the cell corner nearest the pointer, every layer's plan in each cell (the
user: all layers). `Area²` ticks of work a tick are shared over the square's plans in its order, `Work` at most on one
(`Aiming.SharesOf`): four walls go a tick each, one or two plans twice as fast as the screwdriver, three as 2, 1, 1. A machine, door
or ladder is one plan and goes twice as fast too, and so does one-cell mode: the user would not have a builder switch modes to
build a station quickly. The square is never slower than one cell.

**It draws as the drill.** `Draw: 1` from the belt whenever the left button builds with it, at plans or not, in either mode; a
battery is 90 s. Flat, it works one cell, as the screwdriver, so nobody carries both. A run that followed the targets would
flicker the square a round trip behind the pointer.

**A mode, not a key held.** A right click with it in hand switches the square and one cell (the user: Ctrl held while moving is
awkward); the plan put away, the right button did nothing. The mode is the client's, not saved, and the outline shows it.

**Short of items, a fixed order.** Bottom row first, left to right, front before back; what cannot be paid waits. Random would
leave holes in different places each time; the order matches how walls rise. The square's tip sums what its blueprints still need.

**Bought at `order2`**, a craft block at 4 × its worth, made at the workbench of plates, gears, a rotor, a coil, wire and a
circuit. Batteries come about `order4`: bought earlier, it is a screwdriver till then (the user's pick). `Prices.WorthOf` prices
a craft block by its item.

**The look and sound were picked** on Artifacts: «Пистолет», a pistol-grip driver in worn green enamel with three charge lights,
drawn to the fist as the screwdriver now is (both were too big for the hand); and «Винт в металл», freesound 455339, cut to a bite,
a looped steady stretch and a run-down (`game/audio/CREDITS.md`).

## Deliberately not here

- Modules fitted into it, and more than two ticks a tick on one plan.
- Any sim check of the tool, the square or the run on `Work` (principle 6): the square is the client's.
- A friend's mode: not sent, so others see the chuck turn whenever it runs at work.
- Four refunds dropped at once when marks come down four at a time; walls round the hero wait (`building.heroInside`).
