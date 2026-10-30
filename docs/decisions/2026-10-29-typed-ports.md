# Ports take or give one fluid

- Status: accepted
- Date: 2026-10-29
- Builds on [0109](0109-pipes-are-spans.md): spans join ports and joints as before, now by the ports' way and fluid

## Context

Water is coming: ice melted into water, a cistern that holds it, an electrolyser that turns it into oxygen. A port was
only a cell and a span joined any two, so a pipe could not tell a vent that takes air from a tank that gives it, nor a
water port from an air one. More fluids will follow, so the rule has to grow by data.

## Decision

- **A port has a way and a fluid:** `Port(X, Y, Way, Fluid)` in `MachineSpec.Ports`, `Way` `In` (takes) or `Out` (gives),
  the fluid an id of `Content.Fluids` (`air`, `water`). The tank and the generator give air, the vent takes it; their
  cells did not move, so every pipe stayed connected. A new fluid is an id and ports on the machines that use it.
- **A line** is a network counted with every valve open. Its fluid is its first port's in cell order, none while it has
  no port. Lines are derived, never saved or hashed, and found again on the first read after a span changed or the grid
  moved, so the commands of one tick see each other; a player joining gets them from `Rebuild`.
- **A span is refused**, after `pipes.twice`, when both its ends are ports that give (`pipes.bothGive`) or take
  (`pipes.bothTake`), or its ends' fluids differ (`pipes.mixedFluid`). An end's fluid is its port's, else its line's; a
  line with no port matches anything. Lines ignore valves, so opening one never mixes fluids and `SetValve` refuses nothing.
- **Direction is about one span's two ends.** Output → joint → output is legal: a tank and generators on one manifold
  still work, and air still fills a whole network. Rejected: a rule over the whole network (it would refuse by the order
  things were built and by valve state), a direction saved on each span (nothing models directed flow), and one output
  per network (it breaks a tank beside a generator).
- **A machine is placed only where its ports fit:** a blueprint is refused when placed, and at the `Work` that would finish
  it, if a port would land on a node spanned straight to a port of its way, on a line of another fluid, or two of its
  ports of different fluids on one line (`IPipesQueries.SeatProblem`). Building reads the pipes only as its commands
  apply, so it keeps its place in the run order and the saves: no format bump.
- **The piping shows it:** every port a socket in its fluid's colour with an arrow out of its machine or into it, and while
  a span is laid, the ports it may end at ringed (`FluidProblem` from its start, the start's machine's own left out).

## Deliberately not handled

- **Laid spans are not checked again:** drawn maps and saves keep what they hold, a drawn tank–generator span included.
  Rebuilding in place a machine whose port held such a span is refused until the span is cut.
- **Flow direction:** way is a rule for laying and placing; air fills a network as before.
- **A player's own span not yet applied** is not seen by their lit ports or ghost: pipes are not predicted. The sim decides.
