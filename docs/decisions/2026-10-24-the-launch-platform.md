# The launch platform is a deck of joined pieces with a power lamp

- Status: accepted
- Date: 2026-10-24

## Context

Heroes reach a planet only by dev commands (0076: "dev commands until the drop pod"). The drop pod and its landers (an
oxygen tank, a cargo hold) are to stand on a launch platform aboard and drop through it. Designed with the user (the
"Drop capsule concepts" canvas and the session's design): a platform is a floor of pieces that join into one run, opens
as one run, and only with power; a hero sits in their own pod and picks which landers go, from any powered run. This is
the first slice: the platform alone, to build and to cable. Nothing opens yet.

## Decision

- **A piece is a structure** (`Structure.Platform`, `_` in a map; build `launchPlatform`, 2 × 1 cells as a hatch is, forty
  ticks), stood on, sealed and lit as a shut hatch is (`Supports`, `BlocksFromBelow`, `Seals`, `BlocksLight`,
  a door's leak). It is no doorway (`Group.None`): it never toggles, so no door rule touches it.
- **Pieces side by side are one run** (`StructureRuns.RunOf`): the cells of its code along its row. The client draws each
  piece by whether its row runs on past it, left and right (`StructureDraw.Joined`, `platform_0/2/8/10.png`), so a run is
  one deck: the socket at its left end, the lamp at its right.
- **Power gates it, nothing more.** A cable may end on any of its cells (`PowerNodes` anchors any structure). Its lamp is
  green while a cell of it is fed (`IPowerQueries.Feeds`: its network makes or stores power and covers all its demand),
  red otherwise; there is no amber. A network drawing nothing has full satisfaction, so `Feeds` asks for supply as well.
- **It is the last thing space builds.** A piece costs a deck drive, two plates and a glass. The deck drive (`deckDrive`, a
  part) is made at the circuit assembler of two circuits, a rotor and a coil: what slides the deck open. So its blueprint
  opens once the drive, plates and glass are analysed (0027), after the electric smelter and the circuit assembler, both
  built with circuits; glass is its lamp's lens. Its card is on the Ship tab. (First built for creative mode only, a scrap a
  piece; the user then asked for this cost and for it in every game.)

## Deliberately not handled

- Opening, the pod, landers, launching and returning: the next slices, by the design.
- A sim module for the platform: none yet, as nothing it holds is saved; `Landing` comes with the pod.
- A run of many pieces costs a drive each: the pod needs two pieces, a lander one, and a run is built only as long as the
  landers on it need.
