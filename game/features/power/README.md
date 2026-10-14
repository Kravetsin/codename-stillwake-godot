# power (client): cables as ropes

Cables hanging between their nodes and the power overlay, where they are laid, carried and cut (0017,
0021, 0022), ported from `apps/game/src/features/power/` and `render/cable.ts` (G-08). The sim's
`sim/Power` decides: the client reads `Cables`, `Networks`, `NodeAt`, `NetworkOf`, `Satisfaction`,
`LayProblem` and `MoveProblem`, and sends `Lay`, `Cut` and `Move`.

## Files

| File            | Holds                                                                                  | Godot |
| --------------- | -------------------------------------------------------------------------------------- | ----- |
| `Wiring.cs`     | what a press takes hold of (`GripAt`), supports on the way (`SupportOnWay`), `LightOf` (`wiring.ts`) | no    |
| `CableRopes.cs` | every cable as ropes, a rope per piece, hanging aboard and floating outside; carried ends, landings and cables just laid (`ropes.ts`) | no    |
| `PowerHands.cs` | the hands in the overlay: lay, lead on through supports, carry with Shift or the plan, cut with the right button (`mode.ts`) | no    |
| `PowerView.cs`  | the cables, pegs, plugs, grommets and bolts; the overlay, its colours and the ghost    | yes   |

`game/render/CableShape.cs` (`render/cable.ts`: ends, hangs, pieces through the hull) and
`game/render/Rope.cs` (the tether's rope, now also started from given points and brushed by bodies)
are shared. Ported tests: `cable.test.ts`, `ropes.test.ts`, `wiring.test.ts`.

## Guarantees

- A cable is the same rope throughout: laid, it is the rope it was dragged as from the first press; a
  carried end is the real cable, highlighted white (red where it may not be let go); dropped on a node
  it waits there until the world moves it and goes on from where it hangs, never snapping back unless the
  move was refused (two seconds).
- A dragged cable runs to where it would end: a machine's socket, an anchor's middle.
- Cables sync with the world at most every 250 ms once it has run a tick; ropes move every frame at 60
  steps a second and sway out of every hero's body: this one and the others where they are drawn
  (`HeroView.Others`).
