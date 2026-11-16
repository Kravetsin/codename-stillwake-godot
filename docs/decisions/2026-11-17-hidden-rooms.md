# A derelict's rooms are dark till a hero has seen them

- Status: accepted; built
- Date: 2026-11-17

## Context

A vessel world showed every room of the vessel from the moment a hero arrived. The user wanted a derelict closed at first, seen
from the side as a whole hull, a room appearing only once a player opens the door to it, with all the space up to the next doors
and hatches, and staying open after. Five looks were drawn on a pick page (https://claude.ai/artifact/YPZpDZrd1XVdsvhygJa2Qe):
the hull's own plating over the rooms, plating wiped away from the door, plates lifted off into space, dark plating with the
layout showing through, and ink-dark rooms. The user picked the ink-dark rooms (E «Тьма внутри»), without the light that
poured in from the door on the page: the dark simply goes.

## Decision

- **Client only** (`game/features/unseen`): what is seen is worked out from the grid and the heroes' bodies, never in the sim. A
  vessel world never outlives a load, so nothing needs saving, and no command or module is added.
- **Seen** is every open cell with a back wall joined to a hero's body through open cells, stopped by walls and shut doors and
  hatches; every hero on the surface sees, so co-op shares it with no message. Seen stays seen.
- **Dark** is unseen space and what seals between unseen spaces (shut doors, deck floors); the hull and the walls round a seen
  room stay drawn. Plain ink, unshaded, over all inside; a room seen fades from it in 300 ms.
- The pointer names no machine in the dark.

## Consequences

- A friend joining a visit later sees only what is open from then on.
- A lamp in an unseen room still lights the hull's inner face a little, through the game's light.
- Cutting into a room from outside shows it once a hero stands in the hole, with no rule of its own.
