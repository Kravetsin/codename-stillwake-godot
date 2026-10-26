# landing (client): the drop pod's seats

What the launch platform and the drop pod show beyond the grid's own drawing (ADR `2026-10-25-the-drop-pod`). The sim's
`sim/Landing` decides: the client reads `Seats` and `SeatOf`, and the crew feature sends `Board` and `Alight` from E.
The platform's lamps and socket are the power feature's (`PlatformLamps`).

## Files

| File | Purpose | Godot |
| --- | --- | --- |
| `PodSeats.cs` | each drop pod with a hero in it: its skirt's lamps lit and the glint on its glass, over the pod and the hero | yes |
| `logic/Landing.En.cs` | Its player text (`En`): the landing refusals | no |

## Rules

- A seated hero is the crew's to draw (`SeatedHeroes`): its own figure in its suit, behind the porthole from the chest up;
  its walking body is hidden (`HeroView`), held at the pod's floor (`Seating`). This feature lays the lit lamps and the
  glass's glint over it (`dropPod_lit.png`), drawn after it (`SurfaceViews`). The seats are compared every frame, so the
  lamps go out the frame the body shows.
- E on a pod boards it and opens its one slot under the inventory, as a crate's; seated, E leaves it and shuts the slot
  (`crew`). A pod the sim would refuse is neither boarded nor opened; the prompt says why.

## Deliberately not handled

- What the seated hero wears or holds: the figure behind the glass is its suit alone.
