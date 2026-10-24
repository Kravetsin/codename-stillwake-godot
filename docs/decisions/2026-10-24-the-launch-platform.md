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

- **A piece is a structure** (`Structure.Platform`, `_` in a map; build `launchPlatform`, 2 × 1 cells as a hatch is, one
  scrap, seven ticks), stood on, sealed and lit as a shut hatch is (`Supports`, `BlocksFromBelow`, `Seals`, `BlocksLight`,
  a door's leak). It is no doorway (`Group.None`): it never toggles, so no door rule touches it.
- **Pieces side by side are one run** (`StructureRuns.RunOf`): the cells of its code along its row. The client draws each
  piece by whether its row runs on past it, left and right (`StructureDraw.Joined`, `platform_0/2/8/10.png`), so a run is
  one deck: the socket at its left end, the lamp at its right.
- **Power gates it, nothing more.** A cable may end on any of its cells (`PowerNodes` anchors any structure). Its lamp is
  green while a cell of it is fed (`IPowerQueries.Feeds`: its network makes or stores power and covers all its demand),
  red otherwise; there is no amber. A network drawing nothing has full satisfaction, so `Feeds` asks for supply as well.
- **Creative mode only, for now** (`Build.CreativeOnly`): its card shows on the Ship tab in creative mode alone, and a plan
  outside it is refused `building.creativeOnly`. It costs a scrap all the same, for when it leaves creative mode.

## Deliberately not handled

- Opening, the pod, landers, launching and returning: the next slices, by the design.
- A sim module for the platform: none yet, as nothing it holds is saved; `Landing` comes with the pod.
- A plan made in creative mode and worked after it is switched off: it is worked and paid as any plan.
