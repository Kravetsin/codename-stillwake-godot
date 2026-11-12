# The back walls' focus clears the front round the pointer

- Status: accepted; supersedes in part [back-focus-clear](2026-11-07-back-focus-clear.md) (the front hidden and the back
  walls unlit with the back focused)
- Date: 2026-11-12

## Context

With the back walls focused the front was hidden and the back walls drawn out of the light (back-focus-clear). Played,
only the back walls were left: no doors, machines or front walls, and the player lost where the rooms were. Drawn faint
over them again, a front wall over a back wall and one over nothing looked alike.

## Decision

- **With the back walls focused the world is drawn as 0110 had it:** the front at 35% (`Tools.FrontInBack`), the back
  walls whole and lit.
- **Round the pointer the front walls and doors are gone and the back walls drawn as painted, out of the light**
  (`Tools.ClearedRound`, `PlanHand.Clears`, `Lit.SetBare`): the dig's see-through (0107), three cells, down to nothing
  instead of a third, no button held. There a back wall shows whole in its material, and where there is none, the stars.

## Accepted limitations

- Machines, cables and pipes stay faint round the pointer: the see-through is the walls' and doors' shader only.
- Away from the pointer a front wall over a back wall and one over nothing still look alike; the player looks by pointing.
- The ground's soft shadow on natural back walls stays round the pointer.
