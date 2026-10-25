# landing (client): the drop pod's seats

What the launch platform and the drop pod show beyond the grid's own drawing (ADR `2026-10-25-the-drop-pod`). The sim's
`sim/Landing` decides: the client reads `Seats` and `SeatOf`, and the crew feature sends `Board` and `Alight` from E.
The platform's lamps and socket are the power feature's (`PlatformLamps`).

## Files

| File | Purpose | Godot |
| --- | --- | --- |
| `PodSeats.cs` | each drop pod with a hero in it: the hero behind its porthole and its skirt's lamps lit, over the pod | yes |
| `logic/Landing.En.cs` | Its player text (`En`): the landing refusals | no |

## Rules

- A seated hero is drawn behind its pod's porthole from the chest up, the same picture for every peer; its own body is
  hidden (`HeroView`), held at the pod's floor (`Seating`), so the pod stands in front of nothing. The seats are compared every
  frame, so the porthole empties the frame the body shows.
- E on a pod boards it and opens its one slot under the inventory, as a crate's; seated, E leaves it and shuts the slot
  (`crew`). A pod the sim would refuse is neither boarded nor opened; the prompt says why.

## Deliberately not handled

- The suit's colour behind the glass: one picture for every hero.
