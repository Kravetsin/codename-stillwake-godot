# The launch platform is a deck of joined pieces that opens with power

- Status: accepted
- Date: 2026-10-24

## Context

Heroes reach a planet only by dev commands (0076: "dev commands until the drop pod"). The drop pod and its landers (an
oxygen tank, a cargo hold) are to stand on a launch platform aboard and drop through it. Designed with the user (the
"Drop capsule concepts" canvas and the session's design): a platform is a floor of pieces that join into one run, opens
as one run, and only with power; a hero sits in their own pod and picks which landers go, from any powered run. This is
the first slice: the platform alone, to build, to cable and to open by hand.

## Decision

- **A piece is a structure** (`Structure.PlatformClosed`, `_` in a map; open `PlatformOpen`, `~`; build `launchPlatform`,
  2 × 1 cells as a hatch is, forty ticks), stood on, sealed and lit as a shut hatch is (`Supports`, `BlocksFromBelow`,
  `Seals`, `BlocksLight`, a door's leak).
- **Pieces side by side are one run, and one doorway** (`StructureGroup.Platform`, `Join.Run`): the cells of the group along
  its row (`StructureRuns.RunOf`, `LandingModule.RunOf`), so a run opens and shuts whole from any of its cells, by the
  doorway rules a door's segments follow (0111). The client draws each piece by whether its row runs on past it, left and
  right (`StructureDraw.Joined`, `platform_0/2/8/10.png`, open `platform_open_*`), so a run is one deck: the socket at its
  left end, the lamp at its right. It is drawn open or shut, with no slide; a door's sounds play.
- **Power gates it.** A cable may end on any of its cells (`PowerNodes` anchors any structure). Its lamp is green while a
  cell of it is fed (`IPowerQueries.Feeds`: its network makes or stores power and covers all its demand), red otherwise;
  there is no amber. A network drawing nothing has full satisfaction, so `Feeds` asks for supply as well.
- **A sim module opens it** (`Landing`, save format 30): `SetPlatform(cell, open)` sets its run as its sender saw it should
  be, refused `landing.unpowered` when opening a dark run; shutting needs no power. `SetDoor` refuses a platform
  (`grid.noDoor`), as Grid cannot ask Power. E on a run sends it (`Aim`), and says why a dark one stays shut.
- **Open, it vents but holds.** An open piece is an `Opening` that still `Supports`: its room is open to what lies under the
  run (space, laid in a hull's floor) and loses its air, while whatever stands on the deck stays. Dropping is the pod's.
- **It is the last thing space builds.** A piece costs a deck drive, two plates and a glass. The deck drive (`deckDrive`, a
  part) is made at the circuit assembler of two circuits, a rotor and a coil: what slides the deck open. So its blueprint
  opens once the drive, plates and glass are analysed (0027), after the electric smelter and the circuit assembler, both
  built with circuits; glass is its lamp's lens. Its card is on the Ship tab.

## Deliberately not handled

- The pod, landers, launching and returning: the next slices, by the design.
- A run left open when its power fails stays open: shutting it is the player's, as a door's is.
- No prediction of a `SetPlatform` in flight: a host applies it at once, a guest sees it a round trip late.
- A run of many pieces costs a drive each: the pod needs two pieces, a lander one, and a run is built only as long as the
  landers on it need.
