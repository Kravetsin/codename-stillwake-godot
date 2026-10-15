# power (client): cables as ropes

Cables hanging between their nodes and the power tab's overlay, where they are laid, carried and cut
(0017, 0021, 0022, 0030), ported from `apps/game/src/features/power/` and `render/cable.ts` (G-08). The sim's
`sim/Power` decides: the client reads `Cables`, `Networks`, `NodeAt`, `NetworkOf`, `Satisfaction`,
`LayProblem` and `MoveProblem`, and sends `Lay`, `Cut` and `Move`.

## Files

| File            | Holds                                                                                  | Godot |
| --------------- | -------------------------------------------------------------------------------------- | ----- |
| `Wiring.cs`     | what a press takes hold of (`GripAt`), supports on the way (`SupportOnWay`), `LightOf` (`wiring.ts`) | no    |
| `CableRopes.cs` | every cable as ropes, a rope per piece, hanging aboard and floating outside; carried ends, landings and cables just laid (`ropes.ts`) | no    |
| `PowerHands.cs` | the hands on the power tab: the Cable card lays and leads on through supports, no card carries a peg or plug, a middle press near a cable cuts (`mode.ts`) | no    |
| `PowerView.cs`  | the cables, pegs, plugs, grommets and bolts; the overlay, its colours and the ghost    | yes   |

`game/render/CableShape.cs` (`render/cable.ts`: ends, hangs, pieces through the hull) and
`game/render/Rope.cs` (the tether's rope, now also started from given points and brushed by bodies)
are shared. Ported tests: `cable.test.ts`, `ropes.test.ts`, `wiring.test.ts`; beyond them (`CableRopesTests`), a cable
moving on every frame at 240 Hz, let go between two steps, and moved off a hull crossing.

## Guarantees

- A cable is the same rope throughout: laid, it is the rope it was dragged as from the first press; a
  carried end is the real cable, highlighted white (red where it may not be let go); dropped on a node
  it waits there until the world moves it and goes on from where it hangs, never snapping back unless the
  move was refused (two seconds). It goes on from the line it was drawn as even where its pieces change:
  a cable through the hull moved to a node on it, or one laid from outside to a lamp inside, starts each
  new piece along its stretch of that line (`CableRopes.StartsOf`); the TypeScript snapped those to a
  fresh hang.
- A dragged cable runs to where it would end: a machine's socket, an anchor's middle.
- While a cable is carried, every powered machine's socket glows and is outlined along its own picture,
  a screen pixel wide (`render/Outline.cs`): green where the cable would plug in, grey out of reach,
  red where it may not.
- Cables sync with the world at most every 250 ms once it has run a tick; bolts, pegs, plugs and loose
  plugs are made anew only when what they show changes, and the ropes' lines and traces are refilled
  in kept buffers, so a frame allocates next to nothing.
- Ropes step 60 times a second, a step ahead of the clock, and are drawn between their last two steps
  (`Rope.DrawnAt`), so they move on every frame of a faster screen rather than hold still three frames
  in four at 240 Hz; they sway out of every hero's body: this one and the others where they are drawn
  (`HeroView.Others`).
