# Plan: Typed pipe ports, then the cistern

## Why

Water is coming: ice melted into water, a cistern that holds it, an electrolyser that turns it into oxygen. Today a
port is only a cell and a span joins any two, so a pipe cannot tell a vent that takes air from a tank that gives it,
or a water port from an air one. This plan gives every port a way (in or out) and a fluid, makes the sim refuse pipes
and machines that would join two outputs, two inputs or two fluids, and lights the ports a pipe may go to while it is
being laid. Then it adds the cistern, the first machine with water ports, so the rules have a second fluid to bite on.
The cistern holds nothing yet: water itself is the next plan's.

**Order and PRs.** P-1 and P-2 are each their own branch and PR, P-1 merged first. P-1 changes how pipes are laid and
machines placed for everyone (vent to vent now needs a joint); P-2 adds a machine to the
build menu. P-1 writes the ADR and the READMEs; P-2 adds the cistern's lines to them.

**Words.** A *port* is a machine's pipe connection (`MachineSpec.Ports`); its *way* is `In` (takes) or `Out` (gives);
its *fluid* an id of `Content.Fluids`. A *node* is a cell a span ends at, a port or a *joint* (0109). A *network* is
what carries now, split by closed valves. A *line* is the same spans counted with every valve open; it has no fluid of
its own, only its ports': the fluid of its first port in node order, none if it has no port.

## Tasks

### P-1 — Every port has a way and a fluid

**Rules, as the player meets them:**
- In piping mode (key C), every port shows a socket in its fluid's colour, an arrow pointing out of the machine (gives)
  or into it (takes).
- Pressing and dragging lights the ports the pipe may end at. From an output: the inputs of its fluid. From an input:
  the outputs of its fluid. From a joint: the ports its line's fluid and the way rule allow, or every port if its line
  has no port. From a bare cell: every port. Never the start's own machine's ports. Length (`MaxSpan`), `pipes.twice`
  and cost do not change what lights: only way and fluid do. A drag may still end on an unlit cell; the ghost shows why
  it is refused, as today.
- A span is refused, after `pipes.twice` and before the items, in this order: both ends outputs (`pipes.bothGive`,
  "Both ends give"), both ends inputs (`pipes.bothTake`, "Both ends take"), the ends' fluids differ (`pipes.mixedFluid`,
  "Different fluids"). An end's fluid is its port's, else its line's; an end on a line with no port matches anything.
- Direction is about the two ends of one span only: output → joint → output is legal, so a tank and generators on one
  manifold (the testbed) still work, and air still flows across the whole network.
- Lines ignore valves, so opening a valve never mixes fluids; `SetValve` gains no refusal.
- **A machine is placed only where its ports fit.** A blueprint is refused when placed, and the `Work` that would finish
  it, the way a wall does not finish on a hero (0077), if any of its ports would land on a node that a span joins
  directly to a port of the same way (`pipes.bothGive` / `pipes.bothTake`), or on a node of a line holding another
  fluid, or two of its own ports of different fluids would land on one line (`pipes.mixedFluid`). With instant
  building, it is checked when placing. With air the only fluid in P-1, only the way can fail; P-2 makes the fluid
  reachable. The player clears a plan stuck on it by cutting the pipe or cancelling the plan (`CancelPlan`).
- Laid spans are never checked again: drawn maps and saves keep what they hold (a drawn tank–generator span stays).

**What does not change:** spans, joints, `MaxSpan`, costs, valves (0109); Atmosphere and how air flows.

- **Content:** `Fluids` (`air`, `water`) and `Way` (`In`, `Out`); `MachineSpec.Ports` becomes
  `IReadOnlyList<Port>`, `Port(X, Y, Way, Fluid)`. Tank `(0, 2) Out air`, generator `(0, 2) Out air`, vent
  `(0, 0) In air`: the same cells, so every pipe stays connected. Content tests: every port's fluid is in `Fluids`;
  no line of a drawn map (the wreck, the testbed) holds ports of two fluids or a direct span between ports of one way
  other than the drawn tank–generator spans there today.
- **Grid:** `PortsOf` keeps returning cells; a `PortAt(cell)` gives a port's way and fluid. `PipeNetworks.FromLayout`
  and the tests that read `p.X`, `p.Y` follow the record.
- **Pipes:** lines are found as networks are, with no valves (`PipeNetworks.Find(spans, [], [])`): nodes sorted,
  a line's fluid its first port's (`PortAt`) in that order, never from a dictionary walk. They are a module field,
  derived and never saved or hashed: dropped in `ApplyLay`, `ApplyCut` and `Rebuild`, and found again on the first read
  after that or after `_grid.Version` moved (checked on every read), never only once a tick, so commands of one tick see
  each other. Finding them never moves `State.Version`. `FluidProblem(a, b)` holds the three refusals and `LayProblem`
  calls it; `FluidAt(cell)` (null: no fluid) and `SeatProblem(kind, cell)` (cell: the machine's top-left as its plan
  stands; every port in its list order; the first problem) are queries.
- **Run order:** unchanged. Building reads the pipes only as its commands apply, as it reads where heroes are, so it
  keeps its place before Pipes and Crew in the run order and the saves: no format bump.
- **Building:** `SeatProblem` after its own checks of the blueprint, before the items; at `Work` beside
  `building.heroInside`. The reason reaches the hero's prompt (`Prompt`, `WorkAim`) and the building ghost through
  `T(key)`: checked in a frame, and that it reads differently from missing materials.
- **Client:** `PipesOverlay` draws each port's socket by fluid and way, the same size and place as today's sockets;
  air in the pipes' air colour, water teal `#2fa3b8`. `PipeHands` works out the lit set when the drag starts, every port
  where `FluidProblem(start, port)` is null but the start's machine's, and again whenever `Pipes.Version` or the grid's
  version moves during the drag. A press on a machine stands for its nearest port, as today. `Pipes.En.cs` gets
  the three keys.
- **Docs:** an ADR `2026-10-29-typed-ports`: the rules above; why direction per span beat a per-network rule (refuses
  by build order and valve state), a saved direction per span (nothing models directed flow) and one output per
  network (breaks tank plus generator); that lines are derived, so a joining player gets them from `Rebuild`. Pipes
  README (rules, API, refusals, deliberately not handled), Building README (`SeatProblem`), Registry's order, the
  client pipes README; glossary lines for порт/port, вход/input, выход/output, среда/fluid.
- **Done when** scenario tests cover: out → in laid; out → out refused `pipes.bothGive`, in → in `pipes.bothTake`;
  out → joint → out laid; vent → vent refused, vent → joint → vent laid; the refusals' order (`tooLong` before the ports, the ports
  before the items, `twice` before the ports after a load); a drawn tank–generator span kept after load;
  a vent placed on a joint spanned from a tank built, and a generator on one spanned from a vent; a generator placed
  with its port on a joint spanned to a tank refused, and a vent finished there refused at its
  `Work`; in one tick, a `Work` finishing a vent then a `LayPipe` from a generator to it laid, a `Work` finishing a generator then a
  `LayPipe` from the tank to it refused, and a `LayPipe` from a
  tank to a joint then a generator finished on it refused; a press on a machine's cell picks its nearest port. An
  FsCheck invariant over a ship of tanks, generators and vents under random lays, cuts, instant builds and machines taken
  apart: no network holds ports of two fluids, no span joins two ports of one way (shown to bite by breaking
  `FluidProblem` and `SeatProblem` each once). The wreck joins no two ports of one way; the testbed only its drawn
  tank–generator span. The generic fuzz is not relied on: the wreck has one port of each way. Test files that change are named in the PR (`PipesTests`, `AirTests`, `DoubledTests`,
  `ContentTests.Machines`, `MachineTraitsTests`, `BuildMenuDataTests`). Frames: piping with sockets; a drag from the
  generator lighting only vents; a red ghost on generator → tank with its reason.

### P-2 — The cistern, water's first ports

**Rules:**
- The cistern, `cistern`, 4×4, built from the Air tab after the tank: an input of water on its left `(0, 2) In water`
  and an output on its right `(3, 2) Out water`. It stores nothing yet, needs no power and is no air source: air
  ignores it.
- Its ports follow P-1: a span from a cistern to a vent is refused `pipes.mixedFluid`, and so is a vent placed on a
  joint of a cistern's line. A press on its left half takes its input, on its right half its output.
- A water line is drawn teal, unfaded and still, with no flow: it is not dry, it carries nothing yet.

- **Content:** its row in `Machines.All`: cost `plate 4, tube 2` (a designer's number), `BuildTicks 160`,
  `Menu: new(MenuTabs.Air, 25)`, map letter `I` (free: `H` is the ladder), no traits.
- **Art:** `game/art/machine_cistern.png`, 32×32 in the tank's palette: a steel cylinder lying on two saddles over a
  base plate, a hatch on top, a sight glass in its middle showing teal water, pipe stubs at its ports. Confirmed by the
  user before the PR opens (`MachineArtTests` is red without it). Its name and handbook line in the building feature's
  `En`. In the testbed's map (`TestbedTests` wants every machine drawn), piped to a second cistern.
- **Docs:** the ADR and Pipes README gain the cistern as the first water machine; a glossary line for
  цистерна/cistern.
- **Done when** scenario tests cover: cistern → vent refused `pipes.mixedFluid`; cistern out → cistern in laid; a
  cistern placed with its input on an air line's joint refused, and a vent finished on a cistern line's joint refused
  at its `Work`; a cistern piped and built beside a vent and a tank leaves air as it was. Within one tick: a `LayPipe`
  cistern → joint then a vent finished on that joint refused, a cistern finished on a bare joint then a `LayPipe` vent →
  it refused, a `CutPipe` then a vent placed on the freed joint built (the lines found again after each). A drag from
  a cistern's output never lights its own input; a press on its left half takes its input, on its right half its output.
  A two-world test where one world asks `FluidAt` and `SeatProblem` between ticks and is reloaded now and then keeps
  the hashes alike. The FsCheck invariant's maps
  hold cisterns beside air machines, make and shut valves, and check lines (all valves open), not networks; the content
  and testbed checks too; a fuzz run sends `pipes.mixedFluid` from both `LayPipe` and building. A frame of
  the cistern in the testbed, piped, its sockets and line teal.

## Not in this plan

Water itself, in the next plan; the water design decides which module owns it, and Atmosphere keeps air:
- the `water` item, a glass bottle with a cap and a drop on it;
- ice melting into it at the furnace and smelter;
- the cistern holding water and passing it on;
- loading a bottle into the cistern;
- the electrolyser: three glass columns over a base, oxygen in the left, hydrogen vented from the right, a reservoir
  between, bubbles rising; it takes water in and gives air out, the first machine to test `SeatProblem`'s rule on two
  of its own ports of different fluids on one line;
- Atmosphere reading only the air ports once a machine has ports of two fluids.

## Deliberately not handled

- **Flow direction along spans.** Way is a rule for laying and placing, not a direction of flow: air still fills a
  whole network.
- **Checking laid spans again.** Drawn maps and old saves may hold spans the rules would now refuse; they stay and work.
- **Rebuilding in place** a machine taken apart, whose port held a drawn span to a port of the same way (a drawn
  tank–generator span), is refused until that span is cut. Rare, and the reason says what to do.
- **A machine's output piped to its own input** is allowed and never lit; the water plan decides if it means anything.
- **A blueprint planned on a bare joint** later piped so it no longer fits waits at its last `Work` (P-1's seat rule).
- **A player's own span not yet applied** is not seen by their lit set or ghost: `LayPipe` is not predicted (only valves
  and doors are). The lit set and the ghost are hints; the sim decides.
- **The cistern does nothing** between P-2 and the water plan: built, piped and drawn, holding no water.
